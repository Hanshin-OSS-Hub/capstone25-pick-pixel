using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class StatUpgradeStation : MonoBehaviour
{
    [Header("Interaction")]
    public KeyCode interactKey = KeyCode.E;
    public GameObject upgradePanel;
    public GameObject promptUI;
    public GameObject highlightObject;

    private bool playerInRange;

    void Awake()
    {
        GetComponent<Collider2D>().isTrigger = true;
        ResolveUpgradePanel();
        SetInteractionVisual(false);

        if (upgradePanel != null)
            upgradePanel.SetActive(false);
        else
            Debug.LogWarning("[StatUpgradeStation] upgradePanel reference is missing.", this);
    }

    void Update()
    {
        if (!playerInRange || !Input.GetKeyDown(interactKey)) return;

        if (upgradePanel != null)
            upgradePanel.SetActive(!upgradePanel.activeSelf);
        else
            Debug.LogWarning("[StatUpgradeStation] Cannot open stat upgrade UI because upgradePanel is missing.", this);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        playerInRange = true;
        SetInteractionVisual(true);
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        playerInRange = false;
        SetInteractionVisual(false);

        if (upgradePanel != null)
            upgradePanel.SetActive(false);
    }

    void SetInteractionVisual(bool active)
    {
        if (promptUI != null) promptUI.SetActive(active);
        if (highlightObject != null) highlightObject.SetActive(active);
    }

    void ResolveUpgradePanel()
    {
        if (upgradePanel != null) return;

        var terminals = Resources.FindObjectsOfTypeAll<StatUpgradeTerminalUI>();
        for (int i = 0; i < terminals.Length; i++)
        {
            GameObject terminal = terminals[i].gameObject;
            if (!terminal.scene.IsValid()) continue;

            upgradePanel = terminal;
            return;
        }
    }
}
