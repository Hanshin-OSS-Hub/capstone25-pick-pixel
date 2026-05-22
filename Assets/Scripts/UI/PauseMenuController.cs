using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// ESC 키로 Panel_Settings 를 열고 닫는 컨트롤러.
/// 싱글톤 + DontDestroyOnLoad 로 전체 게임에 1개만 존재.
/// 스테이지를 추가할 땐 Inspector 의 enabledScenes 에 씬 이름만 추가하면 된다.
/// </summary>
[DisallowMultipleComponent]
public class PauseMenuController : MonoBehaviour
{
    public static PauseMenuController Instance { get; private set; }

    [Header("UI")]
    [SerializeField] private GameObject settingsPanel;

    [Header("Input")]
    [SerializeField] private KeyCode toggleKey = KeyCode.Escape;

    [Header("Behavior")]
    [SerializeField] private bool pauseTimeWhenOpen = true;
    [SerializeField] private bool pauseAudioWhenOpen = true;

    [Header("ESC 가 활성화되는 씬 (스테이지 추가 시 여기 한 줄 추가)")]
    [SerializeField]
    private List<string> enabledScenes = new List<string> { "Lobby", "Stage1" };

    private HashSet<string> _enabledSceneSet;
    private bool _isActiveInCurrentScene;
    private bool _isOpen;

    public bool IsOpen => _isOpen;

    // ────────────────────────────────────────────────────────────────────
    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        _enabledSceneSet = new HashSet<string>(enabledScenes);

        if (settingsPanel != null) settingsPanel.SetActive(false);

        SceneManager.sceneLoaded += OnSceneLoaded;
        EvaluateScene(SceneManager.GetActiveScene().name);
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        if (Instance == this) Instance = null;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (_isOpen) Close();
        EvaluateScene(scene.name);
    }

    void EvaluateScene(string sceneName)
    {
        _isActiveInCurrentScene = _enabledSceneSet.Contains(sceneName);
    }

    // ────────────────────────────────────────────────────────────────────
    void Update()
    {
        if (!_isActiveInCurrentScene) return;
        if (Input.GetKeyDown(toggleKey)) Toggle();
    }

    // ── Public API ──────────────────────────────────────────────────────
    public void Toggle() { if (_isOpen) Close(); else Open(); }

    public void Open()
    {
        if (settingsPanel == null || _isOpen) return;
        settingsPanel.SetActive(true);
        _isOpen = true;
        if (pauseTimeWhenOpen) Time.timeScale = 0f;
        if (pauseAudioWhenOpen) AudioListener.pause = true;
    }

    public void Close()
    {
        if (settingsPanel == null || !_isOpen) return;
        settingsPanel.SetActive(false);
        _isOpen = false;
        if (pauseTimeWhenOpen) Time.timeScale = 1f;
        if (pauseAudioWhenOpen) AudioListener.pause = false;
    }

    /// <summary>런타임에서 새 스테이지를 등록할 때 사용.</summary>
    public void RegisterScene(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName)) return;
        _enabledSceneSet.Add(sceneName);
    }

    public void UnregisterScene(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName)) return;
        _enabledSceneSet.Remove(sceneName);
        if (sceneName == SceneManager.GetActiveScene().name)
            _isActiveInCurrentScene = false;
    }
}
