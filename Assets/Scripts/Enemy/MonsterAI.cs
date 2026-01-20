using UnityEngine;

public enum MonsterState
{
    Patrol,
    Chase,
    Attack
}

public class MonsterAI : MonoBehaviour
{
    [Header("State")]
    public MonsterState state = MonsterState.Patrol;

    // =========================
    // Monster Type
    // =========================
    [Header("Monster Type")]
    public bool isFlying;        // 공중 몬스터
    public bool isRanged;        // 원거리 공격
    public bool isDashMelee;     // 돌진 근접 공격

    // =========================
    // Movement
    // =========================
    [Header("Movement")]
    public float moveSpeed = 1.5f;
    public float dashSpeed = 6f;
    private int moveDir = 1;

    // =========================
    // Detect
    // =========================
    [Header("Detect Player")]
    public Transform player;
    public float detectRange = 5f;

    // =========================
    // Ground Check
    // =========================
    [Header("Ground Check")]
    public Transform groundCheck;
    public float groundCheckDistance = 0.4f;
    public LayerMask groundLayer;

    // =========================
    // Attack
    // =========================
    [Header("Attack")]
    public float attackRange = 1.2f;
    public float attackCooldown = 1.5f;
    public float attackDuration = 0.4f;

    private float lastAttackTime;
    private bool isAttacking;

    // =========================
    // Flip
    // =========================
    [Header("Flip Control")]
    public float flipCooldown = 0.3f;
    private float lastFlipTime;

    // =========================
    // Components
    // =========================
    Rigidbody2D rb;
    BoxCollider2D bodyCol;
    Animator anim;

    [Header("Hit Collider")]
    public GameObject hitCollider;

    // =========================
    // Awake / Start
    // =========================
    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        bodyCol = GetComponent<BoxCollider2D>();
        anim = GetComponent<Animator>();

        if (isFlying)
            rb.gravityScale = 0;
    }

    void Start()
    {
        AutoPlaceGroundCheck();
        if (hitCollider != null)
            hitCollider.SetActive(false);
    }

    // =========================
    // Update
    // =========================
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
        anim.Play("Walk");

        if (isFlying)
        {
            rb.linearVelocity = new Vector2(moveDir * moveSpeed, 0);
        }
        else
        {
            rb.linearVelocity = new Vector2(moveDir * moveSpeed, rb.linearVelocity.y);

            if (!IsGroundAhead() && Time.time > lastFlipTime + flipCooldown)
                Flip();
        }

        if (DistanceToPlayer() <= detectRange)
            state = MonsterState.Chase;
    }

    // =========================
    // Chase
    // =========================
    void Chase()
    {
        anim.Play("Walk");

        float dist = DistanceToPlayer();

        if (dist <= attackRange && Time.time > lastAttackTime + attackCooldown)
        {
            state = MonsterState.Attack;
            return;
        }

        if (isFlying)
        {
            Vector2 dir = (player.position - bodyCol.bounds.center).normalized;
            rb.linearVelocity = dir * moveSpeed;
        }
        else
        {
            float dir = Mathf.Sign(player.position.x - bodyCol.bounds.center.x);
            rb.linearVelocity = new Vector2(dir * moveSpeed, rb.linearVelocity.y);

            if (dir != moveDir && Time.time > lastFlipTime + flipCooldown)
                Flip();
        }

        if (dist > detectRange)
            state = MonsterState.Patrol;
    }

    // =========================
    // Attack
    // =========================
    void Attack()
    {
        if (isAttacking) return;

        isAttacking = true;
        rb.linearVelocity = Vector2.zero;

        if (isDashMelee)
            DashAttack();
        else if (isRanged)
            RangedAttack();
        else
            MeleeAttack();

        Invoke(nameof(EndAttack), attackDuration);
    }

    void EndAttack()
    {
        if (hitCollider != null)
            hitCollider.SetActive(false);

        isAttacking = false;
        lastAttackTime = Time.time;
        state = MonsterState.Chase;
    }

    // =========================
    // Attack Types
    // =========================
    void MeleeAttack()
    {
        anim.Play("Attack");

        if (hitCollider != null)
            hitCollider.SetActive(true);
    }

    void RangedAttack()
    {
        anim.Play("Attack");
        // TODO: Projectile 생성
    }

    void DashAttack()
    {
        anim.Play("Attack");

        Vector2 dir = (player.position - bodyCol.bounds.center).normalized;
        rb.linearVelocity = dir * dashSpeed;

        Invoke(nameof(StopDash), 0.25f);

        if (hitCollider != null)
            hitCollider.SetActive(true);
    }

    void StopDash()
    {
        rb.linearVelocity = Vector2.zero;
    }

    // =========================
    // Utils
    // =========================
    float DistanceToPlayer()
    {
        return Vector2.Distance(bodyCol.bounds.center, player.position);
    }

    bool IsGroundAhead()
    {
        if (groundCheck == null) return true;

        return Physics2D.Raycast(
            groundCheck.position,
            Vector2.down,
            groundCheckDistance,
            groundLayer
        );
    }

    void Flip()
    {
        moveDir *= -1;
        lastFlipTime = Time.time;

        Vector3 scale = transform.localScale;
        scale.x = Mathf.Abs(scale.x) * moveDir;
        transform.localScale = scale;
    }

    void AutoPlaceGroundCheck()
    {
        if (isFlying || groundCheck == null || bodyCol == null) return;

        float footY = bodyCol.bounds.min.y;
        Vector3 pos = groundCheck.position;
        pos.y = footY - 0.05f;
        groundCheck.position = pos;
    }

    // =========================
    // Gizmos
    // =========================
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        if (groundCheck != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(
                groundCheck.position,
                groundCheck.position + Vector3.down * groundCheckDistance
            );
        }
    }
}
