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

    /// <summary>죽었거나 곧 파괴될 몬스터인지 여부 (포탈 잠금 판정 등에서 사용)</summary>
    public bool IsDead => isDead;

    [Header("방 이탈 제한")]
    [Tooltip("이 거리 이상 플레이어가 멀어지면(포탈 이동 등) 추적 중단하고 Patrol로 복귀")]
    public float maxChaseDistance = 30f;

    [Header("Flip Control")]
    public float flipCooldown = 0.3f;
    [Tooltip("스프라이트 아트가 기본적으로 왼쪽을 보고 있으면 true")]
    public bool  spriteFacesLeft = true;
    private float lastFlipTime;

    Rigidbody2D   rb;
    BoxCollider2D bodyCol;
    Animator      anim;

    [Header("Hit Collider")]
    public GameObject hitCollider;
    private Collider2D hitCol;       // 공격 히트박스 콜라이더 (공격 모션 중에만 활성)
    private Collider2D playerCol;    // 플레이어 콜라이더 (겹침 판정용)
    private bool       dealtThisSwing;

    [Header("Ranged Attack (isRanged=true 일 때)")]
    [Tooltip("BossProjectile 컴포넌트가 부착된 투사체 프리팹")]
    public GameObject projectilePrefab;
    [Tooltip("발사 위치 (null이면 몸통 중심 + firePointOffset 사용)")]
    public Transform firePoint;
    [Tooltip("firePoint가 null일 때 몸통 중심으로부터의 발사 오프셋 (x는 진행방향으로 자동 반전)")]
    public Vector2 firePointOffset = new Vector2(0.5f, 0.1f);

    void Awake()
    {
        rb      = GetComponent<Rigidbody2D>();
        bodyCol = GetComponent<BoxCollider2D>();
        anim    = GetComponent<Animator>();
        if (isFlying) rb.gravityScale = 0;
        if (hitCollider != null) hitCol = hitCollider.GetComponent<Collider2D>();
    }

    void Start()
    {
        if (player == null)
            player = GameObject.FindWithTag("Player")?.transform;
        if (player == null)
            Debug.LogError($"[MonsterAI] {gameObject.name}: Player를 찾을 수 없습니다!");
        if (player != null) playerCol = player.GetComponent<Collider2D>();
        AutoPlaceGroundCheck();
        if (hitCollider != null) hitCollider.SetActive(false);
        ApplyFacing();
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

        // 포탈 이동 등으로 플레이어가 너무 멀어지면 추적 포기
        if (dist > maxChaseDistance)
        {
            ResetToPatrol();
            return;
        }

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

        // 히스테리시스: detectRange × 1.5 이상 멀어져야 Patrol 복귀
        // → 경계선에서 Chase/Patrol 오락가락 방지
        if (dist > detectRange * 1.5f) state = MonsterState.Patrol;
    }

    /// <summary>MapManager 또는 외부에서 호출 — 즉시 Patrol 상태로 리셋</summary>
    public void ResetToPatrol()
    {
        state = MonsterState.Patrol;
        if (rb != null) rb.linearVelocity = Vector2.zero;
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
        // 공격 모션이 지속되는 동안 히트박스가 실제로 플레이어와 겹칠 때만 1회 데미지.
        dealtThisSwing = false;
        float t = 0f;
        while (t < attackDuration)
        {
            if (!dealtThisSwing && PlayerInHitbox())
            {
                DealDamageToPlayer();
                dealtThisSwing = true;
            }
            t += Time.deltaTime;
            yield return null;
        }
        if (hitCollider != null) hitCollider.SetActive(false);
        isAttacking    = false;
        lastAttackTime = Time.time;
        state          = MonsterState.Chase;
    }

    /// <summary>활성화된 공격 히트박스가 플레이어 콜라이더와 겹치는지 검사</summary>
    bool PlayerInHitbox()
    {
        if (hitCol == null && hitCollider != null) hitCol = hitCollider.GetComponent<Collider2D>();
        if (hitCol == null || !hitCol.isActiveAndEnabled) return false;
        if (playerCol == null && player != null) playerCol = player.GetComponent<Collider2D>();
        if (playerCol == null) return false;
        return hitCol.bounds.Intersects(playerCol.bounds);
    }

    void MeleeAttack()
    {
        anim.Play("Attack");
        if (hitCollider != null) hitCollider.SetActive(true);
    }

    void RangedAttack()
    {
        anim.Play("Attack");
        if (projectilePrefab == null || player == null || bodyCol == null) return;
        float dirX = Mathf.Sign(player.position.x - bodyCol.bounds.center.x);
        if (dirX == 0f) dirX = moveDir;
        Vector3 spawn = firePoint != null
            ? firePoint.position
            : bodyCol.bounds.center + new Vector3(dirX * firePointOffset.x, firePointOffset.y, 0f);
        var go = Instantiate(projectilePrefab, spawn, Quaternion.identity);
        var pj = go.GetComponent<BossProjectile>();
        if (pj != null) pj.Launch(new Vector2(dirX, 0f), attackDamage);
    }

    void DashAttack()
    {
        anim.Play("Attack");
        Vector2 dir = (player.position - bodyCol.bounds.center).normalized;
        rb.linearVelocity = dir * dashSpeed;
        StartCoroutine(StopDashRoutine());
        if (hitCollider != null) hitCollider.SetActive(true);
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
        float aheadX = (bodyCol != null ? bodyCol.bounds.extents.x + 0.1f : 0.3f) * moveDir;
        Vector2 origin = (Vector2)groundCheck.position + new Vector2(aheadX, 0f);
        return Physics2D.Raycast(origin, Vector2.down, groundCheckDistance, groundLayer);
    }

    bool IsWallAhead()
    {
        if (bodyCol == null) return false;
        Vector2 dir      = new Vector2(moveDir, 0f);
        float   checkDist = bodyCol.bounds.extents.x + 0.15f;
        return Physics2D.Raycast(bodyCol.bounds.center, dir, checkDist, groundLayer);
    }

    void Flip()
    {
        moveDir     *= -1;
        lastFlipTime = Time.time;
        ApplyFacing();
    }

    void ApplyFacing()
    {
        Vector3 scale = transform.localScale;
        float sign = (spriteFacesLeft ? -1f : 1f) * Mathf.Sign(moveDir);
        scale.x = Mathf.Abs(scale.x) * sign;
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
