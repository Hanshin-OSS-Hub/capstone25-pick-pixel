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
    }

    void Start()
    {
        if (player == null)
            player = GameObject.FindWithTag("Player")?.transform;
        if (player == null)
            Debug.LogError($"[MonsterAI] {gameObject.name}: Player를 찾을 수 없습니다!");

        // 몬스터 몸통과 플레이어 몸통의 "단단한 충돌"을 끈다.
        // 이게 켜져 있으면 몬스터가 플레이어 옆에 붙으려 해도 콜라이더끼리 밀어내서
        // 둘 사이 거리가 attackRange보다 멀게 유지된다 → 플레이어가 몬스터 쪽으로 계속
        // 밀고 들어가거나(겹침) 위에서 밟아야만 공격 사거리에 들어와 공격이 나가던 문제 발생.
        // 충돌을 무시하면 몬스터가 플레이어에게 그대로 겹쳐 붙을 수 있어,
        // "옆에 있으면 인식 → 따라옴 → 근처에서 공격모션 → 닿으면 데미지"가 자연스럽게 동작한다.
        // (플레이어 공격은 OverlapBox 기반이라 충돌 무시와 무관하게 정상 작동)
        if (player != null && bodyCol != null)
        {
            Collider2D pCol = player.GetComponent<Collider2D>();
            if (pCol != null)
                Physics2D.IgnoreCollision(bodyCol, pCol, true);
        }

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

        // 추적 중에는 항상 플레이어를 바라본다. (Patrol의 발판 끝 깜빡임 방지용
        // flipCooldown은 여기선 적용하지 않음 — 적용하면 이동은 플레이어 쪽으로 하면서
        // 스프라이트만 반대로 보는 현상이 생긴다)
        float dx = player.position.x - bodyCol.bounds.center.x;
        // 거의 같은 x(겹쳐 있을 때)에는 좌우 깜빡임 방지를 위해 방향 유지
        if (Mathf.Abs(dx) > 0.1f)
        {
            int want = dx > 0 ? 1 : -1;
            if (want != moveDir) FaceDirection(want);
        }

        if (isFlying)
        {
            Vector2 dir = (player.position - bodyCol.bounds.center).normalized;
            rb.linearVelocity = dir * moveSpeed;
        }
        else
        {
            rb.linearVelocity = new Vector2(moveDir * moveSpeed, rb.linearVelocity.y);
        }
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
        return Physics2D.Raycast(bodyCol.bounds.center, dir, checkDist, groundLayer);
    }

    void Flip()
    {
        FaceDirection(-moveDir);
    }

    // 지정한 방향(1: 오른쪽, -1: 왼쪽)으로 즉시 전환 + 스프라이트 적용
    void FaceDirection(int newDir)
    {
        moveDir      = newDir;
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
