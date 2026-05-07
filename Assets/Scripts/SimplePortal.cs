using UnityEngine;

/// <summary>
/// MapManager와 연동하는 포탈 (포탈 위치로 정확하게 이동)
/// Portal_Right = 다음 방의 Portal_Left로 이동
/// Portal_Left = 이전 방의 Portal_Right로 이동
/// </summary>
public class SimplePortal : MonoBehaviour
{
    [Header("포탈 설정")]
    [Tooltip("Left = 이전 방, Right = 다음 방")]
    public PortalDirection direction = PortalDirection.Right;

    [Header("시각 효과")]
    [Tooltip("포탈 파티클 이펙트 (선택사항)")]
    public ParticleSystem portalEffect;

    [Header("플레이어 스폰 설정")]
    [Tooltip("포탈에서 떨어진 거리 (다음 방 진입 시)")]
    public float spawnOffset = 3f;

    [Header("카메라 설정")]
    [Tooltip("플레이어와 함께 카메라도 이동시킬지 여부")]
    public bool moveCamera = true;

    [Header("디버그")]
    public bool showDebugLog = true;

    // MapManager 참조
    private MapManager mapManager;
    private Camera mainCamera;

    public enum PortalDirection
    {
        Left,   // 이전 방
        Right   // 다음 방
    }

    void Start()
    {
        // MapManager 찾기
        mapManager = FindObjectOfType<MapManager>();

        if (mapManager == null)
        {
            Debug.LogError("[SimplePortal] MapManager를 찾을 수 없습니다!");
        }

        // 메인 카메라 찾기
        mainCamera = Camera.main;

        // 파티클 자동으로 찾기
        if (portalEffect == null)
        {
            portalEffect = GetComponentInChildren<ParticleSystem>();
        }

        // 파티클 재생
        if (portalEffect != null)
        {
            portalEffect.Play();
        }

        if (showDebugLog)
        {
            Debug.Log($"[SimplePortal] {gameObject.name} 초기화 - 방향: {direction}");
        }
    }

    /// <summary>
    /// 플레이어가 포탈에 진입
    /// </summary>
    void OnTriggerEnter2D(Collider2D other)
    {
        // 플레이어 태그 확인
        if (other.CompareTag("Player"))
        {
            if (mapManager == null)
            {
                Debug.LogError("[SimplePortal] MapManager가 없습니다!");
                return;
            }

            if (showDebugLog)
            {
                Debug.Log($"[SimplePortal] 플레이어가 {direction} 포탈 진입!");
            }

            // 방향에 따라 이동
            if (direction == PortalDirection.Right)
            {
                // 다음 방으로
                mapManager.GoToNextRoom();

                // 플레이어를 다음 방의 Portal_Left로 이동
                MovePlayerToPortal(other.transform, "Portal_Left", spawnOffset);
            }
            else
            {
                // 이전 방으로
                mapManager.GoToPreviousRoom();

                // 플레이어를 이전 방의 Portal_Right로 이동
                MovePlayerToPortal(other.transform, "Portal_Right", -spawnOffset);
            }
        }
    }

    /// <summary>
    /// 플레이어를 특정 포탈 위치로 이동
    /// </summary>
    void MovePlayerToPortal(Transform player, string portalName, float offset)
    {
        GameObject currentRoom = mapManager.CurrentRoom;

        if (currentRoom == null)
        {
            Debug.LogWarning("[SimplePortal] CurrentRoom이 null입니다!");
            return;
        }

        // Portals 폴더 찾기
        Transform portalsFolder = currentRoom.transform.Find("Portals");

        if (portalsFolder == null)
        {
            Debug.LogWarning($"[SimplePortal] {currentRoom.name}에 Portals 폴더를 찾을 수 없습니다!");
            return;
        }

        // 목표 포탈 찾기
        Transform targetPortal = portalsFolder.Find(portalName);

        if (targetPortal == null)
        {
            Debug.LogWarning($"[SimplePortal] {currentRoom.name}에서 {portalName}을 찾을 수 없습니다!");
            return;
        }

        // 포탈 위치로 이동 (offset만큼 떨어진 곳)
        Vector3 newPlayerPosition = new Vector3(
            targetPortal.position.x + offset,  // 포탈에서 offset만큼 떨어진 곳
            targetPortal.position.y,           // 포탈과 같은 높이
            0f
        );

        player.position = newPlayerPosition;

        // 카메라도 이동
        if (moveCamera && mainCamera != null)
        {
            Vector3 newCameraPosition = new Vector3(
                newPlayerPosition.x,
                newPlayerPosition.y,
                mainCamera.transform.position.z  // Z는 유지
            );
            mainCamera.transform.position = newCameraPosition;
        }

        if (showDebugLog)
        {
            Debug.Log($"[SimplePortal] 플레이어 이동: {newPlayerPosition} (목표: {portalName})");
        }
    }

    /// <summary>
    /// 포탈 활성화/비활성화
    /// </summary>
    public void SetActive(bool active)
    {
        if (portalEffect != null)
        {
            if (active)
            {
                portalEffect.Play();
            }
            else
            {
                portalEffect.Stop();
            }
        }

        if (showDebugLog)
        {
            Debug.Log($"[SimplePortal] {gameObject.name} 활성화: {active}");
        }
    }

    /// <summary>
    /// Scene View에서 Gizmo 표시
    /// </summary>
    void OnDrawGizmos()
    {
        // 포탈 박스
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(transform.position, new Vector3(1.5f, 3f, 0f));

        // 방향 화살표
        Vector3 arrowDir = direction == PortalDirection.Right ? Vector3.right : Vector3.left;
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(transform.position, transform.position + arrowDir * 1.5f);
    }

    void OnDrawGizmosSelected()
    {
        // 선택 시 반투명 박스
        Gizmos.color = new Color(0f, 1f, 1f, 0.3f);
        Gizmos.DrawCube(transform.position, new Vector3(1.5f, 3f, 0f));
    }
}
