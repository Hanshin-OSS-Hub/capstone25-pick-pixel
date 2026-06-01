using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 상호작용 키로 다른 씬으로 이동하는 포탈.
/// 플레이어가 트리거 범위 안에 있을 때 interactKey 를 누르면 targetScene 을 로드한다.
///
/// 사용법:
/// 1. 포탈 오브젝트에 이 스크립트 부착 (Collider2D 자동으로 Trigger 설정됨)
/// 2. targetScene 에 이동할 씬 이름 입력 (반드시 Build Settings 에 등록되어 있어야 함)
/// 3. Player 오브젝트 Tag 가 "Player" 여야 함
/// 4. (선택) promptUI 에 "E 키로 입장" 같은 안내 UI 를 연결하면 범위 진입 시 표시됨
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class ScenePortal : MonoBehaviour
{
    [Header("이동 설정")]
    [Tooltip("이동할 씬 이름 (Build Settings 에 등록 필요)")]
    public string targetScene = "Stage1";

    [Tooltip("상호작용 키")]
    public KeyCode interactKey = KeyCode.E;

    [Header("안내 UI (선택)")]
    [Tooltip("범위 진입 시 켜고, 벗어나면 끌 안내 오브젝트")]
    public GameObject promptUI;

    [Header("디버그")]
    public bool showDebugLog = true;

    private bool playerInRange;

    void Awake()
    {
        GetComponent<Collider2D>().isTrigger = true;
        if (promptUI != null) promptUI.SetActive(false);
    }

    void Update()
    {
        if (!playerInRange) return;
        if (!Input.GetKeyDown(interactKey)) return;

        if (string.IsNullOrEmpty(targetScene))
        {
            Debug.LogError($"[ScenePortal] '{gameObject.name}': targetScene 이 비어 있습니다!");
            return;
        }

        if (showDebugLog) Debug.Log($"[ScenePortal] '{targetScene}' 씬으로 이동");
        SceneManager.LoadScene(targetScene);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        playerInRange = true;
        if (promptUI != null) promptUI.SetActive(true);
        if (showDebugLog) Debug.Log($"[ScenePortal] 플레이어 진입 — {interactKey} 키로 '{targetScene}' 입장 가능");
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        playerInRange = false;
        if (promptUI != null) promptUI.SetActive(false);
    }

    void OnDrawGizmos()
    {
        Gizmos.color = new Color(0.4f, 0.8f, 1f, 0.9f);
        Gizmos.DrawWireCube(transform.position, new Vector3(1.5f, 3f, 0f));
    }
}
