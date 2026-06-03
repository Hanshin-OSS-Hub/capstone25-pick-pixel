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
        SetInteractionVisual(false);

        if (upgradePanel != null)
            upgradePanel.SetActive(false);
    }

    void Update()
    {
        if (!playerInRange || !Input.GetKeyDown(interactKey)) return;

        if (upgradePanel != null)
            upgradePanel.SetActive(!upgradePanel.activeSelf);
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
}
