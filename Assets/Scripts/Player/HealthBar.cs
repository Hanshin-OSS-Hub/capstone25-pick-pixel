using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// ü�¹� UI ��Ʈ�ѷ�
/// - Bar_Fill�� Image (Fill ���) fillAmount�� ��ü ���̱�
/// - ������ ���� ���� �ڵ� ��ȯ
/// - PlayerController.TakeDamage() / Heal()���� ȣ��
/// </summary>
public class HealthBar : MonoBehaviour
{
    [Header("UI ����")]
    public Image barFill;            // Bar_Fill�� Image ������Ʈ
    public TextMeshProUGUI hpText;   // HP �ؽ�Ʈ (����)

    [Header("HP ����")]
    public float maxHp = 100f;

    [Header("���� ����")]
    public Color colorFull = Color.white;                        // 50% �̻�
    public Color colorMid = new Color(1f, 0.6f, 0f);           // 25~50%
    public Color colorLow = new Color(1f, 0.1f, 0.2f);         // 25% ����

    [Header("������ �̵�")]
    public float smoothSpeed = 5f;   // ü�¹ٰ� �پ��� �ӵ� (0�̸� ���)

    private float currentHp;
    private float targetRatio = 1f;  // ��ǥ ���� (��������)

    // �ܺ� �б��
    public float CurrentHp => currentHp;
    public float MaxHp => maxHp;
    public bool IsDead => currentHp <= 0f;

    // ����������������������������������������������������������������������������������
    void Start()
    {
        if (PlayerStats.Instance != null)
            maxHp = PlayerStats.Instance.MaxHp;

        currentHp = maxHp;
        targetRatio = 1f;
        if (barFill != null) barFill.fillAmount = 1f;
        RefreshText();
    }

    void Update()
    {
        // �������ϰ� fillAmount �̵�
        if (barFill == null) return;
        barFill.fillAmount = Mathf.MoveTowards(
            barFill.fillAmount, targetRatio, smoothSpeed * Time.deltaTime);
        UpdateColor(barFill.fillAmount);
    }

    // ������ �ܺο��� ȣ�� ����������������������������������������������

    /// <summary>������ ����. 0 ���ϸ� ���.</summary>
    public void TakeDamage(float amount)
    {
        currentHp = Mathf.Max(0f, currentHp - amount);
        targetRatio = currentHp / maxHp;
        RefreshText();
    }

    /// <summary>ȸ�� ����.</summary>
    public void Heal(float amount)
    {
        currentHp = Mathf.Min(maxHp, currentHp + amount);
        targetRatio = currentHp / maxHp;
        RefreshText();
    }

    /// <summary>�ִ� HP �缳�� (�� �������� ���� ��).</summary>
    public void SetMaxHp(float newMax, bool refillHp = true)
    {
        maxHp = newMax;
        if (refillHp) currentHp = newMax;
        targetRatio = currentHp / maxHp;
        RefreshText();
    }

    /// <summary>���� HP�� ���� ���� (���̺� �ε� ��).</summary>
    public void SetCurrentHp(float hp)
    {
        currentHp = Mathf.Clamp(hp, 0f, maxHp);
        targetRatio = currentHp / maxHp;
        RefreshText();
    }

    // ������ ���� ���� ������������������������������������������������������

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