using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Scene에 이미 배치된 방들을 활용하는 맵 매니저
///
/// 사용법:
/// 1. 빈 오브젝트에 이 스크립트 붙이기
/// 2. Inspector에서 기존 방 오브젝트들 드래그해서 연결
/// 3. Play하면 랜덤 순서 결정됨
/// </summary>
public class MapManager : MonoBehaviour
{
    [Header("=== 고정 방 (Scene에서 드래그) ===")]
    [SerializeField] private GameObject startRoom;
    [SerializeField] private GameObject bossRoom;   // 보스방 — BGM 없음 (추후 설정)
    [SerializeField] private GameObject exitRoom;

    [Header("=== 전투방들 (Scene에서 드래그) ===")]
    [Tooltip("전투A, 전투B, 전투C 순서로 넣기")]
    [SerializeField] private GameObject[] combatRooms; // 3개

    [Header("=== 설정 ===")]
    [SerializeField] private int minCombatRooms = 2;
    [SerializeField] private int maxCombatRooms = 3;
    
    // 현재 런의 방 순서
    private List<GameObject> currentRunRooms = new List<GameObject>();
    private int currentRoomIndex = 0;
    private int lastCombatIndex = -1;

    // 보스방 인덱스 추적 (BGM 전환용)
    private int bossRoomIndex = -1;
    
    // 외부 접근용
    public GameObject CurrentRoom => currentRunRooms.Count > 0 ? currentRunRooms[currentRoomIndex] : null;
    public int CurrentRoomIndex => currentRoomIndex;
    public int TotalRooms => currentRunRooms.Count;
    
    private void Start()
    {
        GenerateRunOrder();
        ActivateCurrentRoom();

        // 스테이지 시작 시 BGM 재생
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayStageBGM();
    }
    
    /// <summary>
    /// 이번 런의 방 순서 결정
    /// </summary>
    [ContextMenu("Generate Run Order")]
    public void GenerateRunOrder()
    {
        currentRunRooms.Clear();
        currentRoomIndex = 0;
        lastCombatIndex = -1;
        
        // 1. 모든 방 비활성화
        DeactivateAllRooms();
        
        // 2. 시작방 추가
        currentRunRooms.Add(startRoom);
        Debug.Log($"[0] 시작방");
        
        // 3. 전투방 랜덤 선택 (연속 중복 방지)
        int combatCount = Random.Range(minCombatRooms, maxCombatRooms + 1);

        for (int i = 0; i < combatCount; i++)
        {
            int index = SelectWithoutRepeat(combatRooms.Length);
            currentRunRooms.Add(combatRooms[index]);

            string variant = ((char)('A' + index)).ToString();
            Debug.Log($"[{currentRunRooms.Count - 1}] 전투방 {variant}");
        }

        // 4. 보스방 추가 (설정된 경우)
        bossRoomIndex = -1;
        if (bossRoom != null)
        {
            bossRoomIndex = currentRunRooms.Count;
            currentRunRooms.Add(bossRoom);
            Debug.Log($"[{bossRoomIndex}] 보스방");
        }

        // 5. 출구 추가
        currentRunRooms.Add(exitRoom);
        Debug.Log($"[{currentRunRooms.Count - 1}] 출구");
        
        Debug.Log($"===== 런 생성 완료: 총 {currentRunRooms.Count}개 방 =====");
    }
    
    /// <summary>
    /// 연속 중복 방지 선택
    /// </summary>
    private int SelectWithoutRepeat(int max)
    {
        if (max <= 1) return 0;
        
        List<int> available = new List<int>();
        for (int i = 0; i < max; i++)
        {
            if (i != lastCombatIndex)
                available.Add(i);
        }
        
        int selected = available[Random.Range(0, available.Count)];
        lastCombatIndex = selected;
        return selected;
    }
    
    /// <summary>
    /// 모든 방 비활성화
    /// </summary>
    private void DeactivateAllRooms()
    {
        if (startRoom != null) startRoom.SetActive(false);
        if (bossRoom != null) bossRoom.SetActive(false);
        if (exitRoom != null) exitRoom.SetActive(false);

        foreach (var room in combatRooms)
        {
            if (room != null) room.SetActive(false);
        }
    }
    
    /// <summary>
    /// 현재 방만 활성화 + BGM 전환
    /// </summary>
    private void ActivateCurrentRoom()
    {
        DeactivateAllRooms();

        if (currentRoomIndex < currentRunRooms.Count)
        {
            GameObject room = currentRunRooms[currentRoomIndex];
            if (room != null)
            {
                room.SetActive(true);
                Debug.Log($"현재 방: {room.name} ({currentRoomIndex + 1}/{currentRunRooms.Count})");
            }
        }

        // BGM 전환
        if (AudioManager.Instance != null)
        {
            bool isBossRoom = (bossRoomIndex >= 0 && currentRoomIndex == bossRoomIndex);
            if (isBossRoom)
                AudioManager.Instance.PlayBossBGM();
            else
                AudioManager.Instance.PlayStageBGM();
        }
    }
    
    /// <summary>
    /// 다음 방으로 이동 (포탈에서 호출)
    /// </summary>
    public void GoToNextRoom()
    {
        if (currentRoomIndex < currentRunRooms.Count - 1)
        {
            currentRoomIndex++;
            ActivateCurrentRoom();
        }
        else
        {
            Debug.Log("===== 스테이지 클리어! =====");
            // TODO: 클리어 처리
        }
    }
    
    /// <summary>
    /// 이전 방으로 이동 (필요시)
    /// </summary>
    public void GoToPreviousRoom()
    {
        if (currentRoomIndex > 0)
        {
            currentRoomIndex--;
            ActivateCurrentRoom();
        }
    }
    
    /// <summary>
    /// 새 런 시작 (죽었을 때 등)
    /// </summary>
    [ContextMenu("New Run")]
    public void NewRun()
    {
        GenerateRunOrder();
        ActivateCurrentRoom();
    }
    
    /// <summary>
    /// 현재 런 정보 출력
    /// </summary>
    [ContextMenu("Print Current Run")]
    public void PrintCurrentRun()
    {
        Debug.Log("===== 현재 런 순서 =====");
        for (int i = 0; i < currentRunRooms.Count; i++)
        {
            string marker = (i == currentRoomIndex) ? " ◀ 현재" : "";
            Debug.Log($"[{i}] {currentRunRooms[i]?.name}{marker}");
        }
    }
}
