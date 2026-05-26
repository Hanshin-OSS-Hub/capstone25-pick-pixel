using System.Collections;
using UnityEngine;

public enum MonsterState { Patrol, Chase, Attack }

public class MonsterAI : MonoBehaviour
{
    [Header("State")]
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
    public float groundCheckDistance = 0.4f;
    public LayerMask groundLayer;

    [Header("Attack")]
    public float attackRange    = 1.2f;
    public float attackCooldown = 1.5f;
    public float attackDuration = 0.4f;
    public int   attackDamage   = 10;

    private float lastAttackTime;
    private bool  isAttacking;
    private bool  isDead;

    [Header("Flip Control")]
    public float flipCooldown = 0.3f;
    private float lastFlipTime;

    Rigidbody2D   rb;
    BoxCollider2D bodyCol;
    Animator      anim;

    [Header("Hit Collider")]
    public GameObject hitCollider;

    void Awake()
    {
        rb      = GetComponent<Rigidbody2D>();
        bodyCol = GetComponent<BoxCollider2D>();
        anim    = GetComponent<Animator>();
        if (isFlying) rb.gravityScale = 0;
    }

    void Start()
    {
        if (player == null)
            player = GameObject.FindWithTag("Player")?.transform;
        if (player == null)
            Debug.LogError($"[MonsterAI] {gameObject.name}: Player를 찾을 수 없습니다!");
        AutoPlaceGroundCheck();
        if (hitCollider != null) hitCollider.SetActive(false);
    }

    void OnDestroy()
    {
        CancelInvoke();
        StopAllCoroutines();
    }

    void FixedUpdate()
    {
        if (isDead || player == null) return;
        switch (state)
        {
            case MonsterState.Patrol: Patrol(); break;
            case MonsterState.Chase:  Chase();  break;
            case MonsterState.Attack: Attack(); break;
        }
    }

    void Patrol()
    {
        anim.Play("Walk");
        if (isFlying)
            rb.linearVelocity = new Vector2(moveDir * moveSpeed, 0);
        else
        {
            rb.linearVelocity = new Vector2(moveDir * moveSpeed, rb.linearVelocity.y);
            bool shouldFlip = (!IsGroundAhead() || IsWallAhead())
                              && Time.time > lastFlipTime + flipCooldown;
            if (shouldFlip) Flip();
        }
        if (DistanceToPlayer() <= detectRange) state = MonsterState.Chase;
    }

    void Chase()
    {
        anim.Play("Walk");
        float dist = DistanceToPlayer();
        if (dist <= attackRange && Time.time > lastAttackTime + attackCooldown)
        { state = MonsterState.Attack; return; }

        if (isFlying)
        {
            Vector2 dir = (player.position - bodyCol.bounds.center).normalized;
            rb.linearVelocity = dir * moveSpeed;
        }
        else
        {
            float dir = Mathf.Sign(player.position.x - bodyCol.bounds.center.x);
            rb.linearVelocity = new Vector2(dir * moveSpeed, rb.linearVelocity.y);
            if (dir != moveDir && Time.time > lastFlipTime + flipCooldown) Flip();
        }
        if (dist > detectRange) state = MonsterState.Patrol;
    }

    void Attack()
    {
        if (isAttacking) return;
        isAttacking = true;
        rb.linearVelocity = Vector2.zero;
        if      (isDashMelee) DashAttack();
        else if (isRanged)    RangedAttack();
        else                  MeleeAttack();
        StartCoroutine(EndAttackRoutine());
    }

    IEnumerator EndAttackRoutine()
    {
        yield return new WaitForSeconds(attackDuration);
        if (hitCollider != null) hitCollider.SetActive(false);
        isAttacking    = false;
        lastAttackTime = Time.time;
        state          = MonsterState.Chase;
    }

    void MeleeAttack()
    {
        anim.Play("Attack");
        if (hitCollider != null) hitCollider.SetActive(true);
        DealDamageToPlayer();
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
        StartCoroutine(StopDashRoutine());
        if (hitCollider != null) hitCollider.SetActive(true);
        DealDamageToPlayer();
    }

    IEnumerator StopDashRoutine()
    {
        yield return new WaitForSeconds(0.25f);
        rb.linearVelocity = Vector2.zero;
    }

    void DealDamageToPlayer()
    {
        if (player == null) return;
        var pc = player.GetComponent<PlayerController>();
        if (pc != null) pc.TakeDamage(attackDamage);
    }

    public void Die()
    {
        if (isDead) return;
        isDead = true;
        CancelInvoke();
        StopAllCoroutines();
        rb.linearVelocity = Vector2.zero;
        Destroy(gameObject, 0.5f);
    }

    float DistanceToPlayer()
    {
        if (player == null) return float.MaxValue;
        return Vector2.Distance(bodyCol.bounds.center, player.position);
    }

    bool IsGroundAhead()
    {
        if (groundCheck == null) return true;
        return Physics2D.Raycast(groundCheck.position, Vector2.down, groundCheckDistance, groundLayer);
    }

    bool IsWallAhead()
    {
        if (bodyCol == null) return false;
        Vector2 dir      = new Vector2(moveDir, 0f);
        float   checkDist = bodyCol.bounds.extents.x + 0.15f;
        // 몬스터 중앙에서 진행 방향으로 레이 발사 (Ground 레이어 벽 감지)
        return Physics2D.Raycast(bodyCol.bounds.center, dir, checkDist, groundLayer);
    }

    void Flip()
    {
        moveDir     *= -1;
        lastFlipTime = Time.time;
        Vector3 scale = transform.localScale;
        scale.x = Mathf.Abs(scale.x) * moveDir;
        transform.localScale = scale;
    }

    void AutoPlaceGroundCheck()
    {
        if (isFlying || groundCheck == null || bodyCol == null) return;
        Vector3 pos = groundCheck.position;
        pos.y = bodyCol.bounds.min.y - 0.05f;
        groundCheck.position = pos;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectRange);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
        if (groundCheck != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(groundCheck.position,
                groundCheck.position + Vector3.down * groundCheckDistance);
        }
    }
}
