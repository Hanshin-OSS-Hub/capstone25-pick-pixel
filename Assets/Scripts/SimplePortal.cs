using UnityEngine;

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
        if (currentRoom == null) return;

        Transform portalsFolder = currentRoom.transform.Find("Portals");
        if (portalsFolder == null) { Debug.LogWarning("[SimplePortal] Portals 폴더 없음!"); return; }

        Transform targetPortal = portalsFolder.Find(portalName);
        if (targetPortal == null) { Debug.LogWarning($"[SimplePortal] {portalName} 없음!"); return; }

        player.position = new Vector3(targetPortal.position.x + offset, targetPortal.position.y, 0f);
        if (showDebugLog) Debug.Log($"[SimplePortal] 플레이어 이동 → {player.position}");
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
