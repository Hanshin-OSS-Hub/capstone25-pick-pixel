using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class MapManager : MonoBehaviour
{
    [Header("=== 고정 방 ===")]
    [SerializeField] private GameObject startRoom;
    [SerializeField] private GameObject exitRoom;

    [Header("=== 전투방들 ===")]
    [SerializeField] private GameObject[] combatRooms;

    [Header("=== 설정 ===")]
    [SerializeField] private int minCombatRooms = 2;
    [SerializeField] private int maxCombatRooms = 3;

    private List<GameObject> currentRunRooms = new List<GameObject>();
    private int currentRoomIndex = 0;
    private int lastCombatIndex  = -1;

    public GameObject CurrentRoom      => currentRunRooms.Count > 0 ? currentRunRooms[currentRoomIndex] : null;
    public int        CurrentRoomIndex => currentRoomIndex;
    public int        TotalRooms       => currentRunRooms.Count;

    void Start() { GenerateRunOrder(); ActivateCurrentRoom(); }

    [ContextMenu("Generate Run Order")]
    public void GenerateRunOrder()
    {
        currentRunRooms.Clear();
        currentRoomIndex = 0;
        lastCombatIndex  = -1;
        DeactivateAllRooms();

        currentRunRooms.Add(startRoom);
        int combatCount = Random.Range(minCombatRooms, maxCombatRooms + 1);
        for (int i = 0; i < combatCount; i++)
        {
            int index = SelectWithoutRepeat(combatRooms.Length);
            currentRunRooms.Add(combatRooms[index]);
        }
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

        var camFollow = Camera.main?.GetComponent<CameraFollow>();
        if (camFollow == null) return;

        // 1차: TilemapCollider2D가 있는 솔리드 타일맵 기준 (장식 레이어 제외)
        // 2차 폴백: 솔리드 기준 못 찾으면 모든 Tilemap 유니온 사용
        Bounds? unionBounds = CalcTilemapBounds(room, solidOnly: true);
        if (!unionBounds.HasValue)
        {
            Debug.LogWarning($"[MapManager] {room.name}: TilemapCollider2D 없음 → 전체 Tilemap으로 폴백");
            unionBounds = CalcTilemapBounds(room, solidOnly: false);
        }

        if (unionBounds.HasValue) camFollow.SetRoomBounds(unionBounds.Value);
        else                      camFollow.ClearBounds();
    }

    public void GoToNextRoom()
    {
        if (currentRoomIndex < currentRunRooms.Count - 1)
        { currentRoomIndex++; ActivateCurrentRoom(); }
        else Debug.Log("===== 스테이지 클리어! =====");
    }

    static Bounds? CalcTilemapBounds(GameObject room, bool solidOnly)
    {
        Bounds? union = null;
        foreach (var tm in room.GetComponentsInChildren<Tilemap>())
        {
            if (solidOnly && tm.GetComponent<TilemapCollider2D>() == null) continue;
            tm.CompressBounds();
            if (tm.cellBounds.size == Vector3Int.zero) continue;
            Vector3 minW = tm.CellToWorld(tm.cellBounds.min);
            Vector3 maxW = tm.CellToWorld(tm.cellBounds.max) + tm.layoutGrid.cellSize;
            var b = new Bounds(); b.SetMinMax(minW, maxW);
            if (union == null) union = b;
            else { var u = union.Value; u.Encapsulate(b); union = u; }
        }
        return union;
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
