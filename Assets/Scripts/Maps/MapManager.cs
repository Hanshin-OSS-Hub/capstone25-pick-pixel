using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class MapManager : MonoBehaviour
{
    [Header("=== 고정 방 (Scene에서 드래그) ===")]
    [SerializeField] private GameObject startRoom;
    [SerializeField] private GameObject bossRoom;   // 보스방 — BGM 없음 (추후 설정)
    [SerializeField] private GameObject exitRoom;

    [Header("=== 전투방들 (Scene에서 드래그) ===")]
    [SerializeField] private GameObject[] combatRooms;

    [Header("=== 설정 ===")]
    [SerializeField] private int minCombatRooms = 2;
    [SerializeField] private int maxCombatRooms = 3;

    private List<GameObject> currentRunRooms = new List<GameObject>();
    private int currentRoomIndex = 0;
    private int lastCombatIndex  = -1;
    private int bossRoomIndex    = -1;  // BGM 전환용

    public GameObject CurrentRoom      => currentRunRooms.Count > 0 ? currentRunRooms[currentRoomIndex] : null;
    public int        CurrentRoomIndex => currentRoomIndex;
    public int        TotalRooms       => currentRunRooms.Count;

    void Start()
    {
        GenerateRunOrder();
        ActivateCurrentRoom();
        SpawnPlayerInStartRoom();

        // 스테이지 시작 BGM
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayStageBGM();
    }

    /// <summary>
    /// 게임 시작 시 플레이어를 startRoom의 Portal_Left 위치로 워프
    /// Portal_Left가 없으면 startRoom 중심에 배치
    /// </summary>
    private void SpawnPlayerInStartRoom()
    {
        if (startRoom == null)
        {
            Debug.LogWarning("[MapManager] startRoom이 연결되지 않았습니다.");
            return;
        }

        // Player 태그 → PlayerController 컴포넌트 순으로 탐색
        GameObject playerObj = GameObject.FindWithTag("Player");
        if (playerObj == null)
        {
            PlayerController pc = FindFirstObjectByType<PlayerController>();
            if (pc != null) playerObj = pc.gameObject;
        }
        if (playerObj == null)
        {
            Debug.LogWarning("[MapManager] Player 오브젝트를 찾지 못했습니다.");
            return;
        }

        // Portals/Portal_Left 탐색
        Transform portals     = startRoom.transform.Find("Portals");
        Transform portalLeft  = portals?.Find("Portal_Left");

        Vector3 spawnPos;
        if (portalLeft != null)
        {
            // Portal_Left 오른쪽으로 약간 offset (포탈 안으로 안 빨려들어가도록)
            spawnPos = new Vector3(portalLeft.position.x + 3f,
                                   portalLeft.position.y,
                                   0f);
        }
        else
        {
            // Portal_Left 없으면 startRoom 중심에 배치
            spawnPos = new Vector3(startRoom.transform.position.x,
                                   startRoom.transform.position.y,
                                   0f);
            Debug.LogWarning("[MapManager] startRoom에 Portals/Portal_Left가 없어 중심에 배치합니다.");
        }

        playerObj.transform.position = spawnPos;
        Debug.Log($"[MapManager] 플레이어 초기 배치 → {spawnPos}");
    }

    [ContextMenu("Generate Run Order")]
    public void GenerateRunOrder()
    {
        currentRunRooms.Clear();
        currentRoomIndex = 0;
        lastCombatIndex  = -1;
        bossRoomIndex    = -1;
        DeactivateAllRooms();

        // 1. 시작방
        currentRunRooms.Add(startRoom);

        // 2. 전투방 (연속 중복 방지 랜덤)
        int combatCount = Random.Range(minCombatRooms, maxCombatRooms + 1);
        for (int i = 0; i < combatCount; i++)
        {
            int index = SelectWithoutRepeat(combatRooms.Length);
            currentRunRooms.Add(combatRooms[index]);
        }

        // 3. 보스방 (연결된 경우)
        if (bossRoom != null)
        {
            bossRoomIndex = currentRunRooms.Count;
            currentRunRooms.Add(bossRoom);
        }

        // 4. 출구
        currentRunRooms.Add(exitRoom);

        Debug.Log($"===== 런 생성 완료: 총 {currentRunRooms.Count}개 방 =====");
    }

    private int SelectWithoutRepeat(int max)
    {
        if (max <= 1) return 0;
        List<int> available = new List<int>();
        for (int i = 0; i < max; i++)
            if (i != lastCombatIndex) available.Add(i);
        int selected = available[Random.Range(0, available.Count)];
        lastCombatIndex = selected;
        return selected;
    }

    private void DeactivateAllRooms()
    {
        if (startRoom != null) startRoom.SetActive(false);
        if (bossRoom  != null) bossRoom.SetActive(false);
        if (exitRoom  != null) exitRoom.SetActive(false);
        foreach (var room in combatRooms)
            if (room != null) room.SetActive(false);
    }

    private void ActivateCurrentRoom()
    {
        DeactivateAllRooms();
        if (currentRoomIndex >= currentRunRooms.Count) return;

        GameObject room = currentRunRooms[currentRoomIndex];
        if (room == null) return;

        room.SetActive(true);
        Debug.Log($"현재 방: {room.name} ({currentRoomIndex + 1}/{currentRunRooms.Count})");

        // Tilemap 유니온 bounds → 카메라 범위 설정
        var camFollow = Camera.main?.GetComponent<CameraFollow>();
        if (camFollow != null)
        {
            Bounds? unionBounds = null;
            foreach (var tm in room.GetComponentsInChildren<Tilemap>())
            {
                tm.CompressBounds();
                if (tm.cellBounds.size == Vector3Int.zero) continue;
                Vector3 minW = tm.CellToWorld(tm.cellBounds.min);
                Vector3 maxW = tm.CellToWorld(tm.cellBounds.max) + tm.layoutGrid.cellSize;
                Bounds tmBounds = new Bounds();
                tmBounds.SetMinMax(minW, maxW);
                if (unionBounds == null) unionBounds = tmBounds;
                else { Bounds b = unionBounds.Value; b.Encapsulate(tmBounds); unionBounds = b; }
            }
            if (unionBounds.HasValue) camFollow.SetRoomBounds(unionBounds.Value);
            else                      camFollow.ClearBounds();
        }

        // BGM 전환
        if (AudioManager.Instance != null)
        {
            bool isBoss = (bossRoomIndex >= 0 && currentRoomIndex == bossRoomIndex);
            if (isBoss) AudioManager.Instance.PlayBossBGM();
            else        AudioManager.Instance.PlayStageBGM();
        }

        // 현재 방 밖에 있는 몬스터(씬 루트 등) → Patrol 리셋
        ResetOutOfRoomMonsters();
    }

    // 현재 방의 자식이 아닌 MonsterAI를 모두 Patrol로 되돌림
    private void ResetOutOfRoomMonsters()
    {
        GameObject activeRoom = CurrentRoom;
        foreach (var monster in FindObjectsByType<MonsterAI>(FindObjectsSortMode.None))
        {
            // 현재 활성 방의 자식이면 건드리지 않음
            if (activeRoom != null && monster.transform.IsChildOf(activeRoom.transform))
                continue;
            monster.ResetToPatrol();
        }
    }

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
            Debug.Log($"[{i}] {currentRunRooms[i]?.name}{(i == currentRoomIndex ? " ◀ 현재" : "")}");
    }
}
