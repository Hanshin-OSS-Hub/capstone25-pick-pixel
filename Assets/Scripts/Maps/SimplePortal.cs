using UnityEngine;
using UnityEngine.Tilemaps;

public class SimplePortal : MonoBehaviour
{
    [Header("포탈 설정")]
    public PortalDirection direction    = PortalDirection.Right;
    public ParticleSystem  portalEffect;
    public float           spawnOffset  = 3f;
    public bool            showDebugLog = true;

    private MapManager mapManager;

    public enum PortalDirection { Left, Right }

    void OnEnable()
    {
        if (mapManager == null) mapManager = FindObjectOfType<MapManager>();
        if (portalEffect == null) portalEffect = GetComponentInChildren<ParticleSystem>();
        if (portalEffect != null) portalEffect.Play();
    }

    void OnDisable()
    {
        if (portalEffect != null) portalEffect.Stop();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        if (mapManager == null) { Debug.LogError("[SimplePortal] MapManager 없음!"); return; }

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
