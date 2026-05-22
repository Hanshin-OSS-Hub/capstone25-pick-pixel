using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 체력바 UI 컨트롤러
/// - Bar_Fill의 Image (Fill 방식) fillAmount로 액체 줄이기
/// - 비율에 따라 색상 자동 변환
/// - PlayerController.TakeDamage() / Heal()에서 호출
/// </summary>
public class HealthBar : MonoBehaviour
{
    [Header("UI 연결")]
    public Image barFill;            // Bar_Fill의 Image 컴포넌트
    public TextMeshProUGUI hpText;   // HP 텍스트 (선택)

    [Header("HP 설정")]
    public float maxHp = 100f;

    [Header("색상 구간")]
    public Color colorFull = Color.white;                        // 50% 이상
    public Color colorMid = new Color(1f, 0.6f, 0f);           // 25~50%
    public Color colorLow = new Color(1f, 0.1f, 0.2f);         // 25% 이하

    [Header("스무스 이동")]
    public float smoothSpeed = 5f;   // 체력바가 줄어드는 속도 (0이면 즉시)

    private float currentHp;
    private float targetRatio = 1f;  // 목표 비율 (스무스용)

    // 외부 읽기용
    public float CurrentHp => currentHp;
    public float MaxHp => maxHp;
    public bool IsDead => currentHp <= 0f;

    // ─────────────────────────────────────────
    void Start()
    {
        currentHp = maxHp;
        targetRatio = 1f;
        if (barFill != null) barFill.fillAmount = 1f;
        RefreshText();
    }

    void Update()
    {
        // 스무스하게 fillAmount 이동
        if (barFill == null) return;
        barFill.fillAmount = Mathf.MoveTowards(
            barFill.fillAmount, targetRatio, smoothSpeed * Time.deltaTime);
        UpdateColor(barFill.fillAmount);
    }

    // ─── 외부에서 호출 ───────────────────────

    /// <summary>데미지 적용. 0 이하면 사망.</summary>
    public void TakeDamage(float amount)
    {
        currentHp = Mathf.Max(0f, currentHp - amount);
        targetRatio = currentHp / maxHp;
        RefreshText();
    }

    /// <summary>회복 적용.</summary>
    public void Heal(float amount)
    {
        currentHp = Mathf.Min(maxHp, currentHp + amount);
        targetRatio = currentHp / maxHp;
        RefreshText();
    }

    /// <summary>최대 HP 재설정 (새 스테이지 시작 등).</summary>
    public void SetMaxHp(float newMax, bool refillHp = true)
    {
        maxHp = newMax;
        if (refillHp) currentHp = newMax;
        targetRatio = currentHp / maxHp;
        RefreshText();
    }

    /// <summary>현재 HP를 직접 설정 (세이브 로드 등).</summary>
    public void SetCurrentHp(float hp)
    {
        currentHp = Mathf.Clamp(hp, 0f, maxHp);
        targetRatio = currentHp / maxHp;
        RefreshText();
    }

    // ─── 내부 갱신 ───────────────────────────

    void UpdateColor(float ratio)
    {
        if (barFill == null) return;
        if (ratio > 0.5f) barFill.color = colorFull;
        else if (ratio > 0.25f) barFill.color = colorMid;
        else barFill.color = colorLow;
    }

    void RefreshText()
    {
        if (hpText != null)
            hpText.text = $"{(int)currentHp} / {(int)maxHp}";
    }
}