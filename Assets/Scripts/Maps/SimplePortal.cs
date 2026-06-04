using UnityEngine;

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

    void Update()
    {
        // Right 포탈만 잠금 상태 표시 갱신
        if (direction == PortalDirection.Right && lockedIndicator != null)
            lockedIndicator.SetActive(!AreAllEnemiesDead());
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        if (mapManager == null) { Debug.LogError("[SimplePortal] MapManager 없음!"); return; }

        // 앞으로 이동 포탈 — 모든 적이 처치돼야만 통과 가능
        if (direction == PortalDirection.Right)
        {
            if (!AreAllEnemiesDead())
            {
                if (showDebugLog)
                    Debug.Log("[SimplePortal] 🔒 모든 적을 처치해야 다음 방으로 이동할 수 있습니다!");
                return;
            }
            mapManager.GoToNextRoom();
            MovePlayerToPortal(other.transform, "Portal_Left", spawnOffset);
        }
        // 뒤로 이동 포탈 — 항상 통과 가능
        else
        {
            mapManager.GoToPreviousRoom();
            MovePlayerToPortal(other.transform, "Portal_Right", -spawnOffset);
        }
    }

    /// <summary>
    /// 현재 방의 MonsterAI 중 살아있는 적이 하나라도 있으면 false 반환.
    /// 적이 없는 방(시작방·출구방)에서는 항상 true 반환.
    /// </summary>
    bool AreAllEnemiesDead()
    {
        GameObject currentRoom = mapManager?.CurrentRoom;
        if (currentRoom == null) return true;

        foreach (var monster in currentRoom.GetComponentsInChildren<MonsterAI>(true))
        {
            if (!monster.IsDead) return false;
        }
        return true;
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
        // Right = 파란색(잠금 가능), Left = 초록색(항상 열림)
        Gizmos.color = direction == PortalDirection.Right
            ? new Color(0.2f, 0.6f, 1f, 0.9f)
            : new Color(0.2f, 1f, 0.4f, 0.9f);
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
