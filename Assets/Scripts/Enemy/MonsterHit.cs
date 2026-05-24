using UnityEngine;

public class MonsterHit : MonoBehaviour
{
    public int hp = 5;

    private int maxHp;
    private MonsterHealthBar healthBar;

    void Awake()
    {
        maxHp      = hp;
        healthBar  = GetComponentInParent<MonsterHealthBar>();
    }

    // 기존 트리거 방식 (PlayerAttack 태그 히트박스) 유지
    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("PlayerAttack")) return;
        TakeDamage(1);
    }

    /// <summary>플레이어 OverlapBox 등 외부에서 직접 호출 가능</summary>
    public void TakeDamage(int damage)
    {
        if (hp <= 0) return;

        hp = Mathf.Max(0, hp - damage);

        if (healthBar != null)
            healthBar.SetFill((float)hp / maxHp);

        Debug.Log($"[MonsterHit] {transform.root.name} 피격! HP: {hp}/{maxHp}");

        if (hp <= 0)
        {
            var ai = GetComponentInParent<MonsterAI>();
            if (ai != null)
                ai.Die();
            else
                Destroy(transform.parent != null ? transform.parent.gameObject : gameObject);
        }
    }
}
