using UnityEngine;

public class MonsterHit : MonoBehaviour
{
    public int hp = 5;

    private int maxHp;
    private MonsterHealthBar healthBar;

    void Awake()
    {
        maxHp = hp;
        // GetComponentInParent 대신 root에서 직접 검색 (더 안전)
        healthBar = transform.root.GetComponent<MonsterHealthBar>();
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

        // 지연 초기화: Awake에서 못 찾은 경우 재시도
        if (healthBar == null)
            healthBar = transform.root.GetComponent<MonsterHealthBar>();

        hp = Mathf.Max(0, hp - damage);


        if (healthBar != null)
            healthBar.SetFill((float)hp / maxHp);

        // 보스 분기 (BossAI 가 있으면 보스 처리, 일반 몬스터엔 영향 없음)
        var boss = GetComponentInParent<BossAI>();
        if (boss != null)
        {
            if (hp <= 0) boss.Die();
            else boss.PlayHit();
            return;
        }

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
