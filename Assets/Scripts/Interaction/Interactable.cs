using UnityEngine;

/// <summary>
/// E키 상호작용 공통 베이스.
/// DungeonEntrance, StatUpgradeStation, NPCInteraction 모두 이 클래스를 상속.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public abstract class Interactable : MonoBehaviour
{
    public static Interactable Current { get; private set; }

    [Header("힌트 UI")]
    [SerializeField] protected GameObject interactHint;

    protected virtual void Awake()
    {
        GetComponent<Collider2D>().isTrigger = true;
        if (interactHint != null) interactHint.SetActive(false);
    }

    protected virtual void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        Current = this;
        if (interactHint != null) interactHint.SetActive(true);
    }

    protected virtual void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        if (Current == this)
        {
            Current = null;
            if (interactHint != null) interactHint.SetActive(false);
        }
    }

    public abstract void Interact();
}
