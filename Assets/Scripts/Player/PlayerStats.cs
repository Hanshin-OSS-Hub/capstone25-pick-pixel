using UnityEngine;

/// <summary>
/// 로비에서 강화한 스탯을 Stage1까지 유지하는 싱글톤.
/// DontDestroyOnLoad로 씬 전환 후에도 값 보존.
/// </summary>
public class PlayerStats : MonoBehaviour
{
    public static PlayerStats Instance { get; private set; }

    [Header("강화 포인트")]
    public int upgradePoints = 5;

    public const int MaxLevel = 5;

    // ── 강화 레벨 ────────────────────────────────────────────────────────
    private int _hpLevel;
    private int _moveSpeedLevel;
    private int _jumpForceLevel;
    private int _dashSpeedLevel;

    public int HpLevel        => _hpLevel;
    public int MoveSpeedLevel => _moveSpeedLevel;
    public int JumpForceLevel => _jumpForceLevel;
    public int DashSpeedLevel => _dashSpeedLevel;

    // ── 실제 스탯 값 ─────────────────────────────────────────────────────
    public float MaxHp      => 100f + _hpLevel        * 25f;
    public float MoveSpeed  => 7.5f + _moveSpeedLevel * 0.5f;
    public float JumpForce  => 14f  + _jumpForceLevel * 1f;
    public float DashSpeed  => 18f  + _dashSpeedLevel * 2f;

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
    }

    // ── 강화 메서드 ─────────────────────────────────────────────────────

    public bool UpgradeHp()
    {
        if (upgradePoints <= 0 || _hpLevel >= MaxLevel) return false;
        _hpLevel++; upgradePoints--; return true;
    }

    public bool UpgradeMoveSpeed()
    {
        if (upgradePoints <= 0 || _moveSpeedLevel >= MaxLevel) return false;
        _moveSpeedLevel++; upgradePoints--; return true;
    }

    public bool UpgradeJumpForce()
    {
        if (upgradePoints <= 0 || _jumpForceLevel >= MaxLevel) return false;
        _jumpForceLevel++; upgradePoints--; return true;
    }

    public bool UpgradeDashSpeed()
    {
        if (upgradePoints <= 0 || _dashSpeedLevel >= MaxLevel) return false;
        _dashSpeedLevel++; upgradePoints--; return true;
    }

    public void Reset()
    {
        upgradePoints = 5;
        _hpLevel = _moveSpeedLevel = _jumpForceLevel = _dashSpeedLevel = 0;
    }
}
