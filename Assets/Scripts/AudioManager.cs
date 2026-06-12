using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 모든 BGM을 단일 AudioSource로 관리하는 영속 싱글톤.
/// 게임 시작 시 [RuntimeInitializeOnLoadMethod]로 자동 생성되어 씬에 배치할 필요가 없고,
/// 씬이 바뀌어도 파괴되지 않아 BGM이 겹치거나 끊기지 않는다.
///
/// 씬별 기본 BGM:
///   MainMenu  → MainMenuBGM
///   Lobby_V2  → LobbyBGM
///   Stage*    → StageBGM (보스방은 MapManager가 PlayBossBGM 으로 전환)
/// 클립은 모두 Resources/Audio 에서 로드한다.
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    private const string MainMenuScene = "MainMenu";
    private const string LobbyScene    = "Lobby_V2";

    private AudioSource bgmSource;
    private float       bgmVolume = 0.7f;

    private AudioClip stageBGM, bossBGM, mainMenuBGM, lobbyBGM;
    private readonly Dictionary<string, AudioClip> cache = new Dictionary<string, AudioClip>();

    // ── 게임 시작 시 1회 자동 생성 (씬 배치 불필요) ───────────────
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        if (Instance != null) return;
        var go = new GameObject("AudioManager (auto)");
        DontDestroyOnLoad(go);
        go.AddComponent<AudioManager>();
    }

    private void Awake()
    {
        // 씬에 수동 배치된 중복 인스턴스가 있으면 컴포넌트만 제거 (영속 인스턴스만 유지)
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        bgmSource = gameObject.AddComponent<AudioSource>();
        bgmSource.loop         = true;
        bgmSource.playOnAwake  = false;
        bgmSource.spatialBlend = 0f; // 2D
        bgmSource.volume       = bgmVolume;

        stageBGM    = Load("Audio/StageBGM");
        bossBGM     = Load("Audio/BossBGM");
        mainMenuBGM = Load("Audio/MainMenuBGM");
        lobbyBGM    = Load("Audio/LobbyBGM");

        SceneManager.sceneLoaded += OnSceneLoaded;
        ApplySceneBGM(SceneManager.GetActiveScene().name);
    }

    private void OnDestroy()
    {
        if (Instance == this) SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private AudioClip Load(string path)
    {
        if (!cache.TryGetValue(path, out AudioClip c))
        {
            c = Resources.Load<AudioClip>(path);
            if (c == null) Debug.LogWarning($"[AudioManager] Resources/{path} 클립을 찾지 못했습니다.");
            cache[path] = c;
        }
        return c;
    }

    // ── 씬에 맞는 기본 BGM 적용 ───────────────────────────────────
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode) => ApplySceneBGM(scene.name);

    private void ApplySceneBGM(string sceneName)
    {
        if      (sceneName == MainMenuScene)        PlayClip(mainMenuBGM);
        else if (sceneName == LobbyScene)           PlayClip(lobbyBGM);
        else if (sceneName.StartsWith("Stage"))     PlayClip(stageBGM);
        else                                        bgmSource.Stop();
        // 보스방 진입 시에는 MapManager 가 PlayBossBGM() 을 호출해 전환
    }

    // 같은 곡이 이미 재생 중이면 다시 시작하지 않음 → 끊김/겹침 방지
    private void PlayClip(AudioClip clip)
    {
        if (clip == null) { bgmSource.Stop(); return; }
        if (bgmSource.clip == clip && bgmSource.isPlaying) return;
        bgmSource.clip = clip;
        bgmSource.Play();
    }

    // ── 공개 API ──────────────────────────────────────────────────
    /// <summary>일반 스테이지 BGM 재생</summary>
    public void PlayStageBGM() => PlayClip(stageBGM);

    /// <summary>보스방 BGM 재생 (클립이 없으면 무음)</summary>
    public void PlayBossBGM() => PlayClip(bossBGM);

    /// <summary>BGM 정지</summary>
    public void StopBGM() => bgmSource.Stop();

    /// <summary>볼륨 실시간 변경</summary>
    public void SetVolume(float volume)
    {
        bgmVolume = Mathf.Clamp01(volume);
        if (bgmSource != null) bgmSource.volume = bgmVolume;
    }
}
