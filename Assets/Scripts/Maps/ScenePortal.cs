using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Collider2D))]
public class ScenePortal : MonoBehaviour
{
    [Header("Scene")]
    public string targetScene = "Stage1";
    public KeyCode interactKey = KeyCode.E;

    [Header("Interaction Feedback")]
    public GameObject promptUI;
    public GameObject highlightObject;

    [Header("Debug")]
    public bool showDebugLog = true;

    private bool playerInRange;

    void Awake()
    {
        GetComponent<Collider2D>().isTrigger = true;
        SetInteractionVisual(false);
    }

    void Update()
    {
        if (!playerInRange || !Input.GetKeyDown(interactKey)) return;

        if (string.IsNullOrEmpty(targetScene))
        {
            Debug.LogError($"[ScenePortal] '{gameObject.name}': targetScene is empty.");
            return;
        }

        if (showDebugLog)
            Debug.Log($"[ScenePortal] Load scene: {targetScene}");

        SceneManager.LoadScene(targetScene);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        playerInRange = true;
        SetInteractionVisual(true);

        if (showDebugLog)
            Debug.Log($"[ScenePortal] Player entered. Press {interactKey} to enter {targetScene}.");
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        playerInRange = false;
        SetInteractionVisual(false);
    }

    void SetInteractionVisual(bool active)
    {
        if (promptUI != null) promptUI.SetActive(active);
        if (highlightObject != null) highlightObject.SetActive(active);
    }

    void OnDrawGizmos()
    {
        Gizmos.color = new Color(0.4f, 0.8f, 1f, 0.9f);
        Gizmos.DrawWireCube(transform.position, new Vector3(1.5f, 3f, 0f));
    }
}
