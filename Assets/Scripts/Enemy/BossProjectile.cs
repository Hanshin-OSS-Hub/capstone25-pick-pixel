using UnityEngine;

/// <summary>
/// 보스 파이어볼 투사체 (선택 사용). 직선으로 날아가며 Player 에 닿으면 데미지.
/// BossAI.fireballPrefab 에 연결하면 원거리 공격이 즉발 판정 대신 투사체로 동작한다.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class BossProjectile : MonoBehaviour
{
    public float speed = 9f;
    public float lifeTime = 4f;
    int damage = 10;

    Rigidbody2D rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        GetComponent<Collider2D>().isTrigger = true;
    }

    public void Launch(Vector2 dir, int dmg)
    {
        damage = dmg;
        rb.linearVelocity = dir.normalized * speed;
        if (dir.x < 0f)
        {
            var s = transform.localScale; s.x = -Mathf.Abs(s.x); transform.localScale = s;
        }
        Destroy(gameObject, lifeTime);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            var pc = other.GetComponent<PlayerController>();
            if (pc != null) pc.TakeDamage(damage);
            Destroy(gameObject);
        }
        else if (other.CompareTag("Ground"))
        {
            Destroy(gameObject);
        }
    }
}
