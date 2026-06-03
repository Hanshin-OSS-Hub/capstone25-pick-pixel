using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HealthBar : MonoBehaviour
{
    [Header("UI 연결")]
    public Image barFill;
    public TextMeshProUGUI hpText;

    [Header("HP 설정")]
    public float maxHp = 100f;

    [Header("색상 구간")]
    public Color colorFull = Color.white;
    public Color colorMid  = new Color(1f, 0.6f, 0f);
    public Color colorLow  = new Color(1f, 0.1f, 0.2f);

    [Header("스무스 이동")]
    public float smoothSpeed = 5f;

    private float currentHp;
    private float targetRatio = 1f;

    public float CurrentHp => currentHp;
    public float MaxHp     => maxHp;
    public bool  IsDead    => currentHp <= 0f;

    void Start()
    {
        // 문영진: PlayerStats 연동 — 스탯 강화 시 MaxHp 반영
        if (PlayerStats.Instance != null)
            maxHp = PlayerStats.Instance.MaxHp;

        currentHp   = maxHp;
        targetRatio = 1f;
        if (barFill != null) barFill.fillAmount = 1f;
        RefreshText();
    }

    void Update()
    {
        if (barFill == null) return;
        barFill.fillAmount = Mathf.MoveTowards(
            barFill.fillAmount, targetRatio, smoothSpeed * Time.deltaTime);
        UpdateColor(barFill.fillAmount);
    }

    public void TakeDamage(float amount)
    {
        currentHp   = Mathf.Max(0f, currentHp - amount);
        targetRatio = currentHp / maxHp;
        RefreshText();
    }

    public void Heal(float amount)
    {
        currentHp   = Mathf.Min(maxHp, currentHp + amount);
        targetRatio = currentHp / maxHp;
        RefreshText();
    }

    public void SetMaxHp(float newMax, bool refillHp = true)
    {
        maxHp = newMax;
        if (refillHp) currentHp = newMax;
        targetRatio = currentHp / maxHp;
        RefreshText();
    }

    public void SetCurrentHp(float hp)
    {
        currentHp   = Mathf.Clamp(hp, 0f, maxHp);
        targetRatio = currentHp / maxHp;
        RefreshText();
    }

    void UpdateColor(float ratio)
    {
        if (barFill == null) return;
        if      (ratio > 0.5f)  barFill.color = colorFull;
        else if (ratio > 0.25f) barFill.color = colorMid;
        else                    barFill.color = colorLow;
    }

    void RefreshText()
    {
        if (hpText != null)
            hpText.text = $"{(int)currentHp} / {(int)maxHp}";
    }
}
