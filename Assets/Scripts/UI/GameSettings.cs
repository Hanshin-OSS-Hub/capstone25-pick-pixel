using System;
using UnityEngine;
using UnityEngine.Audio;

/// <summary>
/// 게임 전역 설정(볼륨/VSync/그래픽)의 단일 진실 공급원.
/// 씬 전환과 무관하게 살아 있고(DontDestroyOnLoad), PlayerPrefs 로 영구 저장된다.
/// 모든 UI(메인 메뉴의 Panel_Settings, ESC 메뉴의 Panel_Settings 클론 등)는
/// SettingsPanelBinder 를 통해 이 싱글톤과 양방향으로 동기화된다.
/// </summary>
[DisallowMultipleComponent]
public class GameSettings : MonoBehaviour
{
    public static GameSettings Instance { get; private set; }

    const string KeyVolume   = "GameSettings.Volume";
    const string KeyVSync    = "GameSettings.VSync";
    const string KeyGraphics = "GameSettings.Graphics";

    [Header("Audio")]
    [Tooltip("할당되면 AudioMixer 의 exposed 파라미터(volumeParameter)에 dB 값을 쓴다. " +
             "비어 있으면 AudioListener.volume 으로 폴백한다.")]
    [SerializeField] AudioMixer audioMixer;
    [SerializeField] string volumeParameter = "MasterVolume";
    [SerializeField] float minVolumeDb = -80f;
    [SerializeField] float maxVolumeDb = 0f;

    public float Volume   { get; private set; } = 1f;
    public bool  VSync    { get; private set; } = true;
    public float Graphics { get; private set; } = 1f;

    /// <summary>값이 바뀔 때마다(어디서 바뀌었든) 발생. UI 바인더가 구독해서 슬라이더/토글을 갱신한다.</summary>
    public event Action Changed;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Bootstrap()
    {
        if (Instance != null) return;

        var prefab = Resources.Load<GameObject>("GameSettings");
        if (prefab != null) Instantiate(prefab);
        else new GameObject(nameof(GameSettings)).AddComponent<GameSettings>();
    }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        Load();
        ApplyVolume();
        ApplyVSync();
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public void SetVolume(float value)
    {
        value = Mathf.Clamp01(value);
        if (Mathf.Approximately(value, Volume)) return;
        Volume = value;
        ApplyVolume();
        PlayerPrefs.SetFloat(KeyVolume, Volume);
        Changed?.Invoke();
    }

    public void SetVSync(bool value)
    {
        if (value == VSync) return;
        VSync = value;
        ApplyVSync();
        PlayerPrefs.SetInt(KeyVSync, VSync ? 1 : 0);
        Changed?.Invoke();
    }

    public void SetGraphics(float value)
    {
        value = Mathf.Clamp01(value);
        if (Mathf.Approximately(value, Graphics)) return;
        Graphics = value;
        PlayerPrefs.SetFloat(KeyGraphics, Graphics);
        Changed?.Invoke();
    }

    void Load()
    {
        Volume   = PlayerPrefs.GetFloat(KeyVolume,   1f);
        VSync    = PlayerPrefs.GetInt  (KeyVSync,    1) != 0;
        Graphics = PlayerPrefs.GetFloat(KeyGraphics, 1f);
    }

    void ApplyVolume()
    {
        if (audioMixer != null && !string.IsNullOrEmpty(volumeParameter))
        {
            float db = Volume <= 0.0001f
                ? minVolumeDb
                : Mathf.Lerp(minVolumeDb, maxVolumeDb, Volume);
            audioMixer.SetFloat(volumeParameter, db);
            return;
        }
        AudioListener.volume = Volume;
    }

    void ApplyVSync()
    {
        QualitySettings.vSyncCount = VSync ? 1 : 0;
    }

    void OnApplicationQuit() => PlayerPrefs.Save();
}
