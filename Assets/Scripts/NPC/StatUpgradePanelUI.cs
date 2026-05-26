using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 스탯 강화 패널 UI 싱글톤.
///
/// Canvas 구조 예시:
///   Canvas
///   └─ Panel_Upgrade  ← 이 스크립트 부착
///      ├─ Txt_Points          (TMP_Text)  강화 포인트 표시
///      ├─ Row_HP
///      │  ├─ Txt_HP_Level     (TMP_Text)
///      │  └─ Btn_HP_Up        (Button)
///      ├─ Row_MoveSpeed
///      │  ├─ Txt_Speed_Level  (TMP_Text)
///      │  └─ Btn_Speed_Up     (Button)
///      ├─ Row_JumpForce
///      │  ├─ Txt_Jump_Level   (TMP_Text)
///      │  └─ Btn_Jump_Up      (Button)
///      ├─ Row_DashSpeed
///      │  ├─ Txt_Dash_Level   (TMP_Text)
///      │  └─ Btn_Dash_Up      (Button)
///      └─ Btn_Close           (Button)
/// </summary>
public class StatUpgradePanelUI : MonoBehaviour
{
    public static StatUpgradePanelUI Instance { get; private set; }

    [Header("Panel")]
    [SerializeField] private GameObject panel;

    [Header("포인트 표시")]
    [SerializeField] private TMP_Text pointsText;

    [Header("각 스탯 레벨 텍스트")]
    [SerializeField] private TMP_Text hpLevelText;
    [SerializeField] private TMP_Text speedLevelText;
    [SerializeField] private TMP_Text jumpLevelText;
    [SerializeField] private TMP_Text dashLevelText;

    [Header("강화 버튼")]
    [SerializeField] private Button btnHp;
    [SerializeField] private Button btnSpeed;
    [SerializeField] private Button btnJump;
    [SerializeField] private Button btnDash;

    [Header("닫기 버튼")]
    [SerializeField] private Button closeButton;

    [Header("Behavior")]
    [SerializeField] private KeyCode closeKey = KeyCode.Escape;

    private bool _isOpen;
    public bool IsOpen => _isOpen;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        if (panel != null) panel.SetActive(false);

        btnHp?.onClick.AddListener(()    => { PlayerStats.Instance?.UpgradeHp();        Refresh(); });
        btnSpeed?.onClick.AddListener(() => { PlayerStats.Instance?.UpgradeMoveSpeed(); Refresh(); });
        btnJump?.onClick.AddListener(()  => { PlayerStats.Instance?.UpgradeJumpForce(); Refresh(); });
        btnDash?.onClick.AddListener(()  => { PlayerStats.Instance?.UpgradeDashSpeed(); Refresh(); });
        closeButton?.onClick.AddListener(Close);
    }

    void Update()
    {
        if (_isOpen && Input.GetKeyDown(closeKey)) Close();
    }

    public void Open()
    {
        if (_isOpen) return;
        if (panel != null) panel.SetActive(true);
        _isOpen = true;
        Time.timeScale = 0f;
        Refresh();
    }

    public void Close()
    {
        if (!_isOpen) return;
        if (panel != null) panel.SetActive(false);
        _isOpen = false;
        Time.timeScale = 1f;
    }

    void Refresh()
    {
        var s = PlayerStats.Instance;
        if (s == null) return;

        if (pointsText != null)
            pointsText.text = $"강화 포인트: {s.upgradePoints}";

        if (hpLevelText != null)
            hpLevelText.text  = $"최대 HP  {s.HpLevel}/{PlayerStats.MaxLevel}  (+{s.MaxHp - 100f})\n현재: {s.MaxHp}";
        if (speedLevelText != null)
            speedLevelText.text = $"이동 속도  {s.MoveSpeedLevel}/{PlayerStats.MaxLevel}\n현재: {s.MoveSpeed:F1}";
        if (jumpLevelText != null)
            jumpLevelText.text = $"점프력  {s.JumpForceLevel}/{PlayerStats.MaxLevel}\n현재: {s.JumpForce:F1}";
        if (dashLevelText != null)
            dashLevelText.text = $"대시 속도  {s.DashSpeedLevel}/{PlayerStats.MaxLevel}\n현재: {s.DashSpeed:F1}";

        bool canUpgrade = s.upgradePoints > 0;
        if (btnHp != null)    btnHp.interactable    = canUpgrade && s.HpLevel < PlayerStats.MaxLevel;
        if (btnSpeed != null) btnSpeed.interactable  = canUpgrade && s.MoveSpeedLevel < PlayerStats.MaxLevel;
        if (btnJump != null)  btnJump.interactable   = canUpgrade && s.JumpForceLevel < PlayerStats.MaxLevel;
        if (btnDash != null)  btnDash.interactable   = canUpgrade && s.DashSpeedLevel < PlayerStats.MaxLevel;
    }
}
