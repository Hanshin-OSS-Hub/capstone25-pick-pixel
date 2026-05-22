using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Panel_Settings 의 슬라이더/토글을 GameSettings 싱글톤과 양방향으로 묶는다.
/// - 패널이 켜질 때 GameSettings 값을 UI 에 반영
/// - 슬라이더/토글이 바뀌면 GameSettings 에 기록 → 자동으로 게임에 적용 + 저장 + 다른 패널도 동기화
/// 인스펙터에서 슬라이더/토글을 비워두면 Awake 시점에 자식 계층에서 라벨로 자동 매칭한다
/// (TMP 부모 텍스트에 "vol"/"graph" 포함 여부, Toggle 은 단일이므로 첫 번째 매칭).
/// </summary>
[DisallowMultipleComponent]
public class SettingsPanelBinder : MonoBehaviour
{
    [SerializeField] Slider volumeSlider;
    [SerializeField] Slider graphicsSlider;
    [SerializeField] Toggle vsyncToggle;

    bool _suppress;

    void Awake() => AutoFindIfMissing();

    void OnEnable()
    {
        WireListeners(true);
        SyncFromSettings();
        if (GameSettings.Instance != null)
            GameSettings.Instance.Changed += SyncFromSettings;
    }

    void OnDisable()
    {
        WireListeners(false);
        if (GameSettings.Instance != null)
            GameSettings.Instance.Changed -= SyncFromSettings;
    }

    void AutoFindIfMissing()
    {
        if (volumeSlider != null && graphicsSlider != null && vsyncToggle != null) return;

        if (volumeSlider == null || graphicsSlider == null)
        {
            foreach (var s in GetComponentsInChildren<Slider>(true))
            {
                var parent = s.transform.parent;
                var tmp = parent != null ? parent.GetComponent<TextMeshProUGUI>() : null;
                string label = tmp != null ? tmp.text.Trim().ToLowerInvariant() : string.Empty;
                if (volumeSlider == null && label.Contains("vol")) volumeSlider = s;
                else if (graphicsSlider == null && label.Contains("graph")) graphicsSlider = s;
                if (volumeSlider != null && graphicsSlider != null) break;
            }
        }
        if (vsyncToggle == null) vsyncToggle = GetComponentInChildren<Toggle>(true);
    }

    void WireListeners(bool subscribe)
    {
        if (volumeSlider != null)
        {
            volumeSlider.onValueChanged.RemoveListener(OnVolumeChanged);
            if (subscribe) volumeSlider.onValueChanged.AddListener(OnVolumeChanged);
        }
        if (graphicsSlider != null)
        {
            graphicsSlider.onValueChanged.RemoveListener(OnGraphicsChanged);
            if (subscribe) graphicsSlider.onValueChanged.AddListener(OnGraphicsChanged);
        }
        if (vsyncToggle != null)
        {
            vsyncToggle.onValueChanged.RemoveListener(OnVSyncChanged);
            if (subscribe) vsyncToggle.onValueChanged.AddListener(OnVSyncChanged);
        }
    }

    void SyncFromSettings()
    {
        var s = GameSettings.Instance;
        if (s == null) return;

        _suppress = true;
        if (volumeSlider   != null) volumeSlider  .SetValueWithoutNotify(s.Volume);
        if (graphicsSlider != null) graphicsSlider.SetValueWithoutNotify(s.Graphics);
        if (vsyncToggle    != null) vsyncToggle   .SetIsOnWithoutNotify(s.VSync);
        _suppress = false;
    }

    void OnVolumeChanged  (float v) { if (!_suppress) GameSettings.Instance?.SetVolume  (v); }
    void OnGraphicsChanged(float v) { if (!_suppress) GameSettings.Instance?.SetGraphics(v); }
    void OnVSyncChanged   (bool  v) { if (!_suppress) GameSettings.Instance?.SetVSync   (v); }
}
