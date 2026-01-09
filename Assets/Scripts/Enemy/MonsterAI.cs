using UnityEngine;

public enum MonsterState
{
    Patrol,
    Chase,
    Attack
}

public class MonsterAI : MonoBehaviour
{
    public MonsterState state = MonsterState.Patrol;

    [Header("Monster Type")]
    public bool isFlying;
    public bool isRanged;
    public bool isDashMelee;

    [Header("Movement")]
    public float moveSpeed = 1.5f;
    public float dashSpeed = 6f;
    private int moveDir = 1;

    [Header("Detect Player")]
    public Transform player;
    public float detectRange = 5f;

    [Header("Ground Check")]
    public Transform groundCheck;
    public float groundCheckDistance = 0.5f;
    public LayerMask groundLayer;

    [Header("Attack")]
    public float attackRange = 1.2f;
    public float attackCooldown = 1.5f;
    private float lastAttackTime;

    [Header("Patrol Control")]
    public float flipCooldown = 0.4f;
    private float lastFlipTime;

    private Rigidbody2D rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        if (isFlying) rb.gravityScale = 0;
    }

    void FixedUpdate()
    {
        switch (state)
        {
            case MonsterState.Patrol:
                Patrol();
                break;
            case MonsterState.Chase:
                Chase();
                break;
            case MonsterState.Attack:
                Attack();
                break;
        }
    }

    // =========================
    // Patrol
    // =========================
    void Patrol()
    {
        rb.linearVelocity = isFlying
            ? new Vector2(moveDir * moveSpeed, 0)
            : new Vector2(moveDir * moveSpeed, rb.linearVelocity.y);

        if (!isFlying)
        {
            RaycastHit2D hit = Physics2D.Raycast(
                groundCheck.position,
                Vector2.down,
                groundCheckDistance,
                groundLayer
            );

            if (!hit && Time.time > lastFlipTime + flipCooldown)
            {
                Flip();
                lastFlipTime = Time.time;
            }
        }

        if (DistanceToPlayer() < detectRange)
            state = MonsterState.Chase;
    }

    // =========================
    // Chase
    // =========================
    void Chase()
    {
        float dist = DistanceToPlayer();

        if (dist <= attackRange && Time.time > lastAttackTime + attackCooldown)
        {
            state = MonsterState.Attack;
            return;
        }

        if (isFlying)
        {
            Vector2 dir = (player.position - transform.position).normalized;
            rb.linearVelocity = dir * moveSpeed;
        }
        else
        {
            float dir = Mathf.Sign(player.position.x - transform.position.x);
            rb.linearVelocity = new Vector2(dir * moveSpeed, rb.linearVelocity.y);

            if (dir != moveDir && Time.time > lastFlipTime + flipCooldown)
                Flip();
        }

        if (dist > detectRange)
            state = MonsterState.Patrol;
    }

    // =========================
    // Attack (타입별 분기)
    // =========================
    void Attack()
    {
        rb.linearVelocity = Vector2.zero;

        if (isDashMelee)
        {
            DashAttack();
        }
        else if (isRanged)
        {
            RangedAttack();
        }
        else
        {
            MeleeAttack();
        }

        lastAttackTime = Time.time;
        state = MonsterState.Chase;
    }

    // =========================
    // 공격 타입들
    // =========================
    void MeleeAttack()
    {
        Debug.Log("지상 근접 공격");
        // HitCollider 활성화 / 애니메이션 트리거
    }

    void RangedAttack()
    {
        Debug.Log("원거리 발사");
        // 투사체 생성
    }

    void DashAttack()
    {
        Debug.Log("공중 돌진 공격");

        Vector2 dir = (player.position - transform.position).normalized;
        rb.linearVelocity = dir * dashSpeed;
    }

    // =========================
    // Utils
    // =========================
    float DistanceToPlayer()
    {
        return Vector2.Distance(transform.position, player.position);
    }

    void Flip()
    {
        moveDir *= -1;
        Vector3 scale = transform.localScale;
        scale.x = Mathf.Abs(scale.x) * moveDir;
        transform.localScale = scale;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
