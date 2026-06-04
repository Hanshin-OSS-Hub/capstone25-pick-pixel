using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class MapManager : MonoBehaviour
{
    [Header("=== 고정 방 (Scene에서 드래그) ===")]
    [SerializeField] private GameObject startRoom;
    [SerializeField] private GameObject bossRoom;
    [SerializeField] private GameObject exitRoom;

    [Header("=== 전투방 (Combat) ===")]
    [SerializeField] private GameObject[] combatRooms;

    [Header("=== 수직맵 (Vertical) ===")]
    [SerializeField] private GameObject[] verticalRooms;

    [Header("=== 생성 설정 ===")]
    [SerializeField] private int minCombatRooms  = 3;   // 전투방 최소
    [SerializeField] private int maxCombatRooms  = 5;   // 전투방 최대
    [SerializeField] private int minVerticalRooms = 1;  // 수직맵 최소
    [SerializeField] private int maxVerticalRooms = 2;  // 수직맵 최대

    private List<GameObject> currentRunRooms = new List<GameObject>();
    private int currentRoomIndex = 0;
    private int bossRoomIndex    = -1;

    public GameObject CurrentRoom      => currentRunRooms.Count > 0 ? currentRunRooms[currentRoomIndex] : null;
    public int        CurrentRoomIndex => currentRoomIndex;
    public int        TotalRooms       => currentRunRooms.Count;

    // ═══════════════════════════════════════════════════════════
    void Start()
    {
        GenerateRunOrder();
        ActivateCurrentRoom();
        SpawnPlayerInStartRoom();

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayStageBGM();
    }

    // ── 플레이어 초기 배치 ──────────────────────────────────────
    private void SpawnPlayerInStartRoom()
    {
        if (startRoom == null)
        {
            Debug.LogWarning("[MapManager] startRoom이 연결되지 않았습니다.");
            return;
        }

        GameObject playerObj = GameObject.FindWithTag("Player");
        if (playerObj == null)
        {
            PlayerController pc = FindAnyObjectByType<PlayerController>();
            if (pc != null) playerObj = pc.gameObject;
        }
        if (playerObj == null)
        {
            Debug.LogWarning("[MapManager] Player 오브젝트를 찾지 못했습니다.");
            return;
        }

        // Portals/Portal_Left 탐색 → 없으면 재귀 탐색 → 없으면 방 중심
        Transform portals    = startRoom.transform.Find("Portals");
        Transform portalLeft = portals?.Find("Portal_Left");

        if (portalLeft == null)
        {
            foreach (Transform t in startRoom.GetComponentsInChildren<Transform>(true))
            {
                if (t.name == "Portal_Left") { portalLeft = t; break; }
            }
        }

        Vector3 spawnPos;
        if (portalLeft != null)
        {
            spawnPos = new Vector3(portalLeft.position.x + 3f,
                                   portalLeft.position.y, 0f);
        }
        else
        {
            spawnPos = new Vector3(startRoom.transform.position.x,
                                   startRoom.transform.position.y, 0f);
            Debug.LogWarning("[MapManager] Portal_Left를 찾지 못해 방 중심에 배치합니다.");
        }

        playerObj.transform.position = spawnPos;
        Debug.Log($"[MapManager] 플레이어 초기 배치 → {spawnPos}");

        // 배치 직후 카메라를 즉시 스냅 (CameraFollow.SnapToTarget)
        var camFollow = Camera.main?.GetComponent<CameraFollow>();
        camFollow?.SnapToTarget();
    }

    // ── 런 순서 생성 ─────────────────────────────────────────────
    [ContextMenu("Generate Run Order")]
    public void GenerateRunOrder()
    {
        currentRunRooms.Clear();
        currentRoomIndex = 0;
        bossRoomIndex    = -1;
        DeactivateAllRooms();

        // 1. 시작방
        currentRunRooms.Add(startRoom);

        // 2. 중간 방 리스트 구성 (전투방 + 수직맵 보장)
        var middleRooms = new List<GameObject>();

        //  ▸ 전투방: minCombatRooms ~ maxCombatRooms 개 (연속 중복 방지)
        int combatCount = Random.Range(minCombatRooms,
                          Mathf.Max(minCombatRooms, maxCombatRooms) + 1);
        int lastCombat  = -1;
        for (int i = 0; i < combatCount && combatRooms != null && combatRooms.Length > 0; i++)
        {
            int idx = PickWithoutRepeat(combatRooms.Length, ref lastCombat);
            middleRooms.Add(combatRooms[idx]);
        }

        //  ▸ 수직맵: minVerticalRooms ~ maxVerticalRooms 개 (연속 중복 방지)
        int vertCount  = Random.Range(minVerticalRooms,
                         Mathf.Max(minVerticalRooms, maxVerticalRooms) + 1);
        int lastVert   = -1;
        for (int i = 0; i < vertCount && verticalRooms != null && verticalRooms.Length > 0; i++)
        {
            int idx = PickWithoutRepeat(verticalRooms.Length, ref lastVert);
            middleRooms.Add(verticalRooms[idx]);
        }

        // 3. 중간 방 셔플 (피셔-예이츠)
        for (int i = middleRooms.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (middleRooms[i], middleRooms[j]) = (middleRooms[j], middleRooms[i]);
        }
        foreach (var r in middleRooms)
            currentRunRooms.Add(r);

        // 4. 보스방
        if (bossRoom != null)
        {
            bossRoomIndex = currentRunRooms.Count;
            currentRunRooms.Add(bossRoom);
        }

        // 5. 출구
        currentRunRooms.Add(exitRoom);

        Debug.Log($"===== 런 생성: 총 {currentRunRooms.Count}개 방 " +
                  $"(전투 {combatCount}개, 수직 {vertCount}개) =====");
    }

    // 연속 중복 방지 선택 (ref 로 마지막 인덱스 추적)
    private static int PickWithoutRepeat(int max, ref int lastIndex)
    {
        if (max <= 1) return 0;
        var available = new List<int>();
        for (int i = 0; i < max; i++)
            if (i != lastIndex) available.Add(i);
        if (available.Count == 0) available.Add(0);   // 방이 1개뿐일 때 fallback
        int selected = available[Random.Range(0, available.Count)];
        lastIndex = selected;
        return selected;
    }

    // ── 방 활성화/비활성화 ────────────────────────────────────────
    private void DeactivateAllRooms()
    {
        // 방들의 공통 부모(Sections) 아래 "모든" 방을 끈다.
        // → combatRooms/verticalRooms 에 등록되지 않은 방(여관/상점/보물 등)도 확실히 비활성화하여
        //   항상 켜진 채 화면에 남는 문제를 방지한다.
        Transform parent = startRoom != null ? startRoom.transform.parent : null;
        if (parent != null)
        {
            foreach (Transform child in parent)
                child.gameObject.SetActive(false);
            return;
        }

        // 폴백: 부모를 찾지 못하면 등록된 방만 비활성화
        if (startRoom != null) startRoom.SetActive(false);
        if (bossRoom  != null) bossRoom.SetActive(false);
        if (exitRoom  != null) exitRoom.SetActive(false);
        if (combatRooms  != null) foreach (var r in combatRooms)  if (r) r.SetActive(false);
        if (verticalRooms != null) foreach (var r in verticalRooms) if (r) r.SetActive(false);
    }

    private void ActivateCurrentRoom()
    {
        DeactivateAllRooms();
        if (currentRoomIndex >= currentRunRooms.Count) return;

        GameObject room = currentRunRooms[currentRoomIndex];
        if (room == null) return;

        room.SetActive(true);
        Debug.Log($"현재 방: {room.name} ({currentRoomIndex + 1}/{currentRunRooms.Count})");

        // 카메라 범위: "밟는 지형"(Ground/Platform 등) 타일맵 우선,
        // 없으면 모든 타일맵 유니온으로 폴백
        var camFollow = Camera.main?.GetComponent<CameraFollow>();
        if (camFollow != null)
        {
            Bounds? unionBounds = CalcGroundTilemapBounds(room);
            if (!unionBounds.HasValue)
                unionBounds = CalcTilemapBounds(room, solidOnly: false);

            if (unionBounds.HasValue) camFollow.SetRoomBounds(unionBounds.Value);
            else                      camFollow.ClearBounds();
        }

        // 보스방 BGM 전환
        if (AudioManager.Instance != null)
        {
            bool isBoss = (bossRoomIndex >= 0 && currentRoomIndex == bossRoomIndex);
            if (isBoss) AudioManager.Instance.PlayBossBGM();
            else        AudioManager.Instance.PlayStageBGM();
        }

        ResetOutOfRoomMonsters();
    }

    private void ResetOutOfRoomMonsters()
    {
        GameObject activeRoom = CurrentRoom;
        foreach (var monster in FindObjectsByType<MonsterAI>())
        {
            if (activeRoom != null && monster.transform.IsChildOf(activeRoom.transform))
                continue;
            monster.ResetToPatrol();
        }
    }

    // ── Tilemap bounds 계산 ───────────────────────────────────────
    static readonly string[] GroundTilemapNames  = { "GroundTilemap", "PlatformTilemap", "Platforms", "Ground" };
    static readonly string[] ExcludeTilemapNames = { "Background", "Decoration", "DecorationTilemap" };

    static Bounds? CalcGroundTilemapBounds(GameObject room)
    {
        Bounds? union = null;
        foreach (var tm in room.GetComponentsInChildren<Tilemap>())
        {
            string n = tm.gameObject.name;
            if (System.Array.Exists(ExcludeTilemapNames, e => n.Contains(e))) continue;
            if (!System.Array.Exists(GroundTilemapNames,  g => n.Contains(g))) continue;
            tm.CompressBounds();
            if (tm.cellBounds.size == Vector3Int.zero) continue;
            var b = new Bounds();
            b.SetMinMax(tm.CellToWorld(tm.cellBounds.min),
                        tm.CellToWorld(tm.cellBounds.max) + tm.layoutGrid.cellSize);
            if (union == null) union = b;
            else { var u = union.Value; u.Encapsulate(b); union = u; }
        }
        return union;
    }

    static Bounds? CalcTilemapBounds(GameObject room, bool solidOnly)
    {
        Bounds? union = null;
        foreach (var tm in room.GetComponentsInChildren<Tilemap>())
        {
            if (solidOnly && tm.GetComponent<TilemapCollider2D>() == null) continue;
            tm.CompressBounds();
            if (tm.cellBounds.size == Vector3Int.zero) continue;
            var b = new Bounds();
            b.SetMinMax(tm.CellToWorld(tm.cellBounds.min),
                        tm.CellToWorld(tm.cellBounds.max) + tm.layoutGrid.cellSize);
            if (union == null) union = b;
            else { var u = union.Value; u.Encapsulate(b); union = u; }
        }
        return union;
    }

    // ── 공개 API ─────────────────────────────────────────────────
    public void GoToNextRoom()
    {
        if (currentRoomIndex < currentRunRooms.Count - 1)
        { currentRoomIndex++; ActivateCurrentRoom(); }
        else Debug.Log("===== 스테이지 클리어! =====");
    }

    public void GoToPreviousRoom()
    {
        if (currentRoomIndex > 0)
        { currentRoomIndex--; ActivateCurrentRoom(); }
    }

    [ContextMenu("New Run")]
    public void NewRun() { GenerateRunOrder(); ActivateCurrentRoom(); }

    [ContextMenu("Print Current Run")]
    public void PrintCurrentRun()
    {
        for (int i = 0; i < currentRunRooms.Count; i++)
            Debug.Log($"[{i}] {currentRunRooms[i]?.name}" +
                      $"{(i == currentRoomIndex ? " ◀ 현재" : "")}");
    }
}
