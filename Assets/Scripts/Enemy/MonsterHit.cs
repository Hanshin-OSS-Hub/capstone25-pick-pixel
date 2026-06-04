using UnityEngine;

public class MonsterHit : MonoBehaviour
{
    public int hp = 5;

    private int maxHp;
    private MonsterHealthBar healthBar;

    void Awake()
    {
        maxHp = hp;
        // transform.root 는 씬 최상위를 반환하므로 방 자식에 배치된 몬스터에서 실패함
        // → GetComponentInParent 로 부모 체인을 타고 올라가 MonsterHealthBar 탐색
        healthBar = GetComponentInParent<MonsterHealthBar>(true);
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
            healthBar = GetComponentInParent<MonsterHealthBar>(true);

        hp = Mathf.Max(0, hp - damage);


        if (healthBar != null)
            healthBar.SetFill((float)hp / maxHp);

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
