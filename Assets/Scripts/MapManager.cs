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

    [Header("=== 카메라 Bounds용 Tilemap 이름 ===")]
    [SerializeField] private string boundsTilemapName = "Platforms";

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

        Tilemap boundsTilemap = null;
        foreach (var tm in room.GetComponentsInChildren<Tilemap>())
        {
            if (tm.gameObject.name == boundsTilemapName)
            { boundsTilemap = tm; break; }
        }

        if (boundsTilemap == null)
            Debug.LogWarning($"[MapManager] '{boundsTilemapName}' Tilemap을 {room.name}에서 찾지 못했습니다!");

        var camFollow = Camera.main?.GetComponent<CameraFollow>();
        if (camFollow != null) camFollow.SetRoomBounds(boundsTilemap);
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
