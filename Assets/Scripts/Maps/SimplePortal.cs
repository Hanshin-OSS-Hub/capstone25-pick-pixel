using UnityEngine;
using UnityEngine.Tilemaps;

public class SimplePortal : MonoBehaviour
{
    [Header("포탈 설정")]
    public PortalDirection direction    = PortalDirection.Right;
    public ParticleSystem  portalEffect;
    public float           spawnOffset  = 3f;
    public bool            showDebugLog = true;

    [Header("잠금 UI (선택)")]
    [Tooltip("적이 살아있을 때 표시할 오브젝트 (자물쇠 아이콘 등). 없어도 동작함.")]
    public GameObject lockedIndicator;

    private MapManager mapManager;

    // 몬스터 처치 여부 폴링 (매 프레임 대신 일정 간격으로 검사)
    private float nextClearCheckTime;
    private bool  lastClearedState = true;

    public enum PortalDirection { Left, Right }

    void OnEnable()
    {
        if (mapManager == null) mapManager = FindAnyObjectByType<MapManager>();
        if (portalEffect == null) portalEffect = GetComponentInChildren<ParticleSystem>();
        if (portalEffect != null) portalEffect.Play();
    }

    void OnDisable()
    {
        if (portalEffect != null) portalEffect.Stop();
    }

    void Update()
    {
        // 일정 간격으로 현재 방의 몬스터 처치 여부를 검사해 포탈 이펙트 on/off
        if (Time.time < nextClearCheckTime) return;
        nextClearCheckTime = Time.time + 0.25f;

        bool cleared = IsRoomCleared();
        if (cleared == lastClearedState) return;
        lastClearedState = cleared;

        // 잠겨 있으면(미처치) 이펙트 정지, 처치 완료되면 이펙트 재생 → 시각적 피드백
        if (portalEffect != null)
        {
            if (cleared) portalEffect.Play();
            else         portalEffect.Stop();
        }

        // 자물쇠 UI 갱신
        if (lockedIndicator != null)
            lockedIndicator.SetActive(!cleared);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        if (mapManager == null) { Debug.LogError("[SimplePortal] MapManager 없음!"); return; }

        // 현재 방의 몬스터가 모두 처치되기 전에는 이동 불가
        if (!IsRoomCleared())
        {
            if (showDebugLog)
                Debug.Log("[SimplePortal] 아직 몬스터가 남아 있어 포탈이 잠겨 있습니다.");
            return;
        }

        if (direction == PortalDirection.Right)
        { mapManager.GoToNextRoom(); MovePlayerToPortal(other.transform, "Portal_Left",  spawnOffset); }
        else
        { mapManager.GoToPreviousRoom(); MovePlayerToPortal(other.transform, "Portal_Right", -spawnOffset); }
    }

    void MovePlayerToPortal(Transform player, string portalName, float offset)
    {
        GameObject currentRoom = mapManager.CurrentRoom;
        if (currentRoom == null)
        {
            Debug.LogError("[SimplePortal] CurrentRoom이 null입니다!");
            return;
        }

        // Portals 폴더 → Portal_Left/Right 탐색
        Transform portalsFolder = currentRoom.transform.Find("Portals");
        Transform targetPortal  = portalsFolder?.Find(portalName);

        // 못 찾으면 전체 하위에서 재탐색
        if (targetPortal == null)
        {
            foreach (Transform t in currentRoom.GetComponentsInChildren<Transform>(true))
            {
                if (t.name == portalName) { targetPortal = t; break; }
            }
        }

        Vector3 spawnPos;
        if (targetPortal != null)
        {
            spawnPos = new Vector3(targetPortal.position.x + offset,
                                   targetPortal.position.y, 0f);
        }
        else
        {
            // 최종 폴백: Tilemap 유니온 중심 → 없으면 방 Transform 위치
            Bounds? bounds = GetRoomBounds(currentRoom);
            if (bounds.HasValue)
                spawnPos = new Vector3(bounds.Value.center.x + offset,
                                       bounds.Value.min.y + 1.5f, 0f);
            else
                spawnPos = new Vector3(currentRoom.transform.position.x + offset,
                                       currentRoom.transform.position.y, 0f);

            Debug.LogWarning($"[SimplePortal] '{currentRoom.name}'에서 {portalName}을 찾지 못해 중심에 배치합니다.");
        }

        player.position = spawnPos;
        if (showDebugLog) Debug.Log($"[SimplePortal] 플레이어 이동 → {spawnPos}");
    }

    // 현재 방에 살아있는 몬스터가 하나도 없으면 true (보스/전투방 클리어 판정)
    // 몬스터가 방의 자식이든, 씬 루트에 있든(방 영역 안에 있으면) 모두 검사한다.
    bool IsRoomCleared()
    {
        if (mapManager == null) return true;
        GameObject room = mapManager.CurrentRoom;
        if (room == null) return true;

        Bounds? roomBounds = GetRoomBounds(room);

        foreach (var ai in FindObjectsByType<MonsterAI>(FindObjectsInactive.Exclude))
        {
            if (ai == null || ai.IsDead) continue;

            // 1) 현재 방의 자식 몬스터
            if (ai.transform.IsChildOf(room.transform)) return false;

            // 2) 루트에 있어도 현재 방의 Tilemap 영역(XY) 안에 있으면 이 방 소속으로 간주
            if (roomBounds.HasValue)
            {
                Vector3 p = ai.transform.position;
                Bounds  b = roomBounds.Value;
                if (p.x >= b.min.x && p.x <= b.max.x &&
                    p.y >= b.min.y && p.y <= b.max.y)
                    return false;
            }
        }
        return true;
    }

    // 방의 Tilemap 유니온 Bounds 계산
    Bounds? GetRoomBounds(GameObject room)
    {
        Bounds? union = null;
        foreach (var tm in room.GetComponentsInChildren<Tilemap>())
        {
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

    void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(transform.position, new Vector3(1.5f, 3f, 0f));
        Gizmos.color = Color.yellow;
        Vector3 dir = direction == PortalDirection.Right ? Vector3.right : Vector3.left;
        Gizmos.DrawLine(transform.position, transform.position + dir * 1.5f);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0f, 1f, 1f, 0.3f);
        Gizmos.DrawCube(transform.position, new Vector3(1.5f, 3f, 0f));
    }
}
