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

        // 스테이지 시작 BGM
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayStageBGM();
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
