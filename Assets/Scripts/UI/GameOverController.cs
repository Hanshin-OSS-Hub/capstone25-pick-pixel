using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 플레이어 사망 시 Panel_gameover 를 화면에 띄우는 컨트롤러.
/// 싱글톤 + DontDestroyOnLoad 로 게임에 1개만 존재.
/// Bootstrap 이 BeforeSceneLoad 에서 자동으로 생성한다.
/// </summary>
[DisallowMultipleComponent]
public class GameOverController : MonoBehaviour
{
    public static GameOverController Instance { get; private set; }

    const string PanelResourceName = "Panel_gameover";
    const string MainMenuSceneName = "MainMenu";
    const string LobbySceneName    = "Lobby_V2";
    const int    CanvasSortingOrder = 200;

    private Canvas     _canvas;
    private GameObject _panelInstance;
    private bool       _isShown;

    public bool IsShown => _isShown;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Bootstrap()
    {
        if (Instance != null) return;
        var go = new GameObject(nameof(GameOverController));
        go.AddComponent<GameOverController>();
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        BuildCanvas();
        InstantiatePanel();
        WireButtons();
        HideImmediate();
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void BuildCanvas()
    {
        var canvasGo = new GameObject("GameOverCanvas",
            typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGo.transform.SetParent(transform, false);

        _canvas = canvasGo.GetComponent<Canvas>();
        _canvas.renderMode    = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder  = CanvasSortingOrder;

        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight  = 0.5f;
    }

    void InstantiatePanel()
    {
        var prefab = Resources.Load<GameObject>(PanelResourceName);
        if (prefab == null)
        {
            Debug.LogError($"[GameOverController] Resources/{PanelResourceName}.prefab 을 찾을 수 없음.");
            return;
        }
        _panelInstance = Instantiate(prefab, _canvas.transform);
        _panelInstance.name = PanelResourceName;

        // 프리팹에 저장된 scale/sizeDelta 값과 무관하게 항상 전체 화면을 채우도록 강제
        var rt = _panelInstance.GetComponent<RectTransform>();
        if (rt != null)
        {
            rt.localScale   = Vector3.one;
            rt.anchorMin    = Vector2.zero;
            rt.anchorMax    = Vector2.one;
            rt.offsetMin    = Vector2.zero;
            rt.offsetMax    = Vector2.zero;
        }
    }

    void WireButtons()
    {
        if (_panelInstance == null) return;

        var mainBtn  = FindButton("Button_main menu");
        var lobbyBtn = FindButton("Button_lobby");

        if (mainBtn != null)
        {
            mainBtn.onClick.RemoveAllListeners();
            mainBtn.onClick.AddListener(OnMainMenu);
        }
        else Debug.LogWarning("[GameOverController] 'Button_main menu' 를 찾지 못함.");

        if (lobbyBtn != null)
        {
            lobbyBtn.onClick.RemoveAllListeners();
            lobbyBtn.onClick.AddListener(OnLobby);
        }
        else Debug.LogWarning("[GameOverController] 'Button_lobby' 를 찾지 못함.");
    }

    Button FindButton(string childName)
    {
        var t = _panelInstance.transform.Find(childName);
        return t != null ? t.GetComponent<Button>() : null;
    }

    void HideImmediate()
    {
        if (_canvas != null) _canvas.gameObject.SetActive(false);
        _isShown = false;
    }

    // ── Public API ──────────────────────────────────────────────────────
    public void ShowGameOver()
    {
        if (_isShown || _canvas == null) return;
        _canvas.gameObject.SetActive(true);
        _isShown = true;
        Time.timeScale     = 0f;
        AudioListener.pause = true;
    }

    /// <summary>플레이어 사망 애니메이션이 끝난 뒤에 패널을 띄우고 싶을 때 사용.</summary>
    public void ShowGameOverDelayed(float seconds)
    {
        if (_isShown) return;
        StartCoroutine(ShowAfter(seconds));
    }

    IEnumerator ShowAfter(float seconds)
    {
        yield return new WaitForSecondsRealtime(seconds);
        ShowGameOver();
    }

    public void OnMainMenu()  => LoadScene(MainMenuSceneName);
    public void OnLobby()     => LoadScene(LobbySceneName);

    void LoadScene(string sceneName)
    {
        Time.timeScale      = 1f;
        AudioListener.pause = false;
        HideImmediate();
        SceneManager.LoadScene(sceneName);
    }
}
