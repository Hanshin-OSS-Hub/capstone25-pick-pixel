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
    [Tooltip("플레이어 인식 반경 (가로·세로 구분 없이 이 거리 안에 들어오면 추적 시작)")]
    public float detectRange = 6f;

    [Header("Ground Check")]
    public Transform groundCheck;
    public float groundCheckDistance = 0.4f;
    public LayerMask groundLayer;

    [Header("Attack")]
    [Tooltip("공격 발동 가로 거리 (이 안에 들어오면 공격)")]
    public float attackRange    = 1.2f;
    [Tooltip("공격이 닿는 세로 허용 범위")]
    public float attackVerticalRange = 2.5f;
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
    private bool       dealtThisSwing;

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
        ApplyFacing();   // 시작 시 진행 방향에 맞춰 초기 방향 적용
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
        // 인식: 같은 층(세로 허용범위) + 가로 detectRange 안에 플레이어가 있으면 추적 시작
        if (PlayerDetected()) state = MonsterState.Chase;
    }

    void Chase()
    {
        anim.Play("Walk");

        // 포탈 이동 등으로 플레이어가 너무 멀어지면 추적 포기 (한 번 인식하면 끈질기게 추적)
        if (DistanceToPlayer() > maxChaseDistance)
        {
            ResetToPatrol();
            return;
        }

        // 공격: 가로 attackRange + 세로 허용범위 안 → 옆에 나란히 서면 확실히 공격
        if (InAttackRange() && Time.time > lastAttackTime + attackCooldown)
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
    }

    // MapManager 또는 외부에서 호출 — 즉시 Patrol 상태로 리셋
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
        // (단순히 공격 상태 진입만으로 데미지를 주던 기존 방식 → 허공 공격/몸통 접촉 데미지 방지)
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

    // 공격 모션 중(EndAttackRoutine 윈도우 동안에만 호출됨), 공격이 닿는 범위 안에
    // 플레이어가 있는지 검사. 작은 히트박스 콜라이더 bounds 대신 "공격 사거리" 기준으로
    // 판정해야, 몬스터가 attackRange 거리에서 멈춰 공격할 때 실제로 데미지가 들어간다.
    bool PlayerInHitbox()
    {
        if (player == null) return false;
        float hx = Mathf.Abs(player.position.x - bodyCol.bounds.center.x);
        float hy = Mathf.Abs(player.position.y - bodyCol.bounds.center.y);
        return hx <= attackRange + 0.3f && hy <= attackVerticalRange;
    }

    // ── 거리/감지 헬퍼 (가로·세로 분리: 2D 횡스크롤에서 같은 층 판정에 유리) ──
    float HorizontalDistanceToPlayer()
    {
        if (player == null) return float.MaxValue;
        return Mathf.Abs(player.position.x - bodyCol.bounds.center.x);
    }

    float VerticalDistanceToPlayer()
    {
        if (player == null) return float.MaxValue;
        return Mathf.Abs(player.position.y - bodyCol.bounds.center.y);
    }

    // 인식은 높이 구분 없이 "반경" 판정 — 플레이어가 가까이(어느 방향이든) 오면 추적 시작.
    // (세로 게이팅을 없애 발판 위·아래 등 높이 차가 있어도 근처면 인식하도록)
    bool PlayerDetected()
    {
        return DistanceToPlayer() <= detectRange;
    }

    bool InAttackRange()
    {
        return HorizontalDistanceToPlayer() <= attackRange
            && VerticalDistanceToPlayer() <= attackVerticalRange;
    }

    void MeleeAttack()
    {
        anim.Play("Attack");
        if (hitCollider != null) hitCollider.SetActive(true);
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
        // 발 바로 아래가 아니라 "진행 방향 앞쪽" 지면을 검사해야 플랫폼 끝에서
        // 허공으로 걸어 나가지 않고 미리 돌아선다. (몸통 절반 너비만큼 앞쪽으로 이동)
        float aheadX = (bodyCol != null ? bodyCol.bounds.extents.x + 0.1f : 0.3f) * moveDir;
        Vector2 origin = (Vector2)groundCheck.position + new Vector2(aheadX, 0f);
        return Physics2D.Raycast(origin, Vector2.down, groundCheckDistance, groundLayer);
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
        ApplyFacing();
    }

    // moveDir과 아트 기본 방향(spriteFacesLeft)에 맞춰 localScale.x 부호 결정
    void ApplyFacing()
    {
        Vector3 scale = transform.localScale;
        // 아트가 왼쪽을 보면: 오른쪽 이동(moveDir>0)일 때 미러(-), 왼쪽 이동일 때 원본(+)
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
