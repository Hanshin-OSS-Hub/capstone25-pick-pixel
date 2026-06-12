using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 효과음(SFX) 전역 재생기. 씬에 배치하지 않아도 게임 시작 시 자동 생성되어 모든 씬에서 동작한다.
/// 클립은 Resources/Audio/SFX 에서 경로로 로드하며, 한 번 로드하면 캐시한다.
/// 추가로 각 씬의 모든 UI Button 클릭에 메뉴 클릭음을 자동 연결한다.
/// </summary>
public class SfxManager : MonoBehaviour
{
    public static SfxManager Instance { get; private set; }

    // ── 클립 경로 (Resources 기준, 확장자 제외) ──────────────────
    public const string Jump      = "Audio/SFX/Jump";
    public const string Land      = "Audio/SFX/Land";
    public const string Run       = "Audio/SFX/Run";
    public const string Roll      = "Audio/SFX/Roll";
    public const string Hit       = "Audio/SFX/Hit";
    public const string Hurt      = "Audio/SFX/Hurt";
    public const string Death     = "Audio/SFX/Death";
    public const string MenuClick = "Audio/SFX/MenuClick";

    private AudioSource src;
    private readonly Dictionary<string, AudioClip> cache = new Dictionary<string, AudioClip>();
    private readonly HashSet<int> hookedButtons = new HashSet<int>();

    // 게임 시작 시 1회 자동 생성 (씬 배치 불필요)
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        if (Instance != null) return;
        var go = new GameObject("SfxManager (auto)");
        DontDestroyOnLoad(go);
        go.AddComponent<SfxManager>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        src = gameObject.AddComponent<AudioSource>();
        src.playOnAwake  = false;
        src.spatialBlend = 0f; // 2D

        SceneManager.sceneLoaded += OnSceneLoaded;
        HookButtons(); // 첫 씬 처리
    }

    private void OnDestroy()
    {
        if (Instance == this) SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    /// <summary>효과음 1회 재생.</summary>
    public void Play(string resourcePath, float volume = 1f)
    {
        AudioClip clip = Get(resourcePath);
        if (clip != null) src.PlayOneShot(clip, volume);
    }

    private AudioClip Get(string path)
    {
        if (!cache.TryGetValue(path, out AudioClip c))
        {
            c = Resources.Load<AudioClip>(path);
            if (c == null) Debug.LogWarning($"[SfxManager] Resources/{path} 클립을 찾지 못했습니다.");
            cache[path] = c;
        }
        return c;
    }

    // ── UI 버튼 클릭음 자동 연결 ──────────────────────────────────
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode) => HookButtons();

    private void HookButtons()
    {
        var buttons = FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var b in buttons)
        {
            if (!hookedButtons.Add(b.GetInstanceID())) continue; // 이미 연결됨
            b.onClick.AddListener(OnAnyButtonClicked);
        }
    }

    private void OnAnyButtonClicked() => Play(MenuClick);
}
