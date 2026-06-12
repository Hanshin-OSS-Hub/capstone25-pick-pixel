using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(Collider2D))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 7.5f;

    [Header("Jump")]
    public float jumpForce = 14f;
    public int   extraJumps = 1;
    public float jumpCutMultiplier = 0.5f;

    [Header("Dash / Roll")]
    public float dashSpeed    = 18f;
    public float dashDuration = 0.15f;
    public float dashCooldown = 0.5f;

    [Header("Attack")]
    public float attackCooldown    = 0.25f;
    public float attackComboWindow = 1.0f;

    [Header("공격 히트박스")]
    public float attackHitWidth  = 2.2f;
    public float attackHitHeight = 2.0f;
    public float attackHitOffset = 1.4f;
    public int   attackDamage    = 1;

    [Header("HP")]
    public HealthBar healthBar;
    public float     invincibleTime = 0.5f;

    [Header("Misc")]
    public bool       noBlood   = false;
    public GameObject slideDust = null;

    private Rigidbody2D rb;
    private Animator    anim;
    private Collider2D  col;

    private PhysicsMaterial2D slickMat;   // 공중용: 마찰 0 → 벽에 붙어도 멈추지 않고 자연스럽게 슬라이딩
    private PhysicsMaterial2D gripMat;    // 바닥용: 정상 마찰 → 경사/발판 위에서 미끄러지지 않음
    private PhysicsMaterial2D currentMat; // 현재 적용된 머티리얼(중복 적용 방지)

    private Sensor_HeroKnight groundSensor;
    private Sensor_HeroKnight wallSensorR1;
    private Sensor_HeroKnight wallSensorR2;
    private Sensor_HeroKnight wallSensorL1;
    private Sensor_HeroKnight wallSensorL2;

    // ── 내부 상태 ──────────────────────────────────────────────────────────
    private bool  facingRight   = true;
    private bool  grounded      = false;
    private bool  isWallSliding = false;
    private bool  isDashing     = false;
    private bool  isDead        = false;
    private bool  isInvincible  = false;    // 피격 무적 프레임 (정인규)

    private int   jumpCount       = 0;
    private int   currentAttack   = 0;
    private float timeSinceAttack = 0f;
    private float delayToIdle     = 0f;
    private float lastDashTime    = -999f;

    // 효과음용 상태
    [Header("발소리")]
    public  float footstepInterval = 0.3f;  // 달리기 발소리 간격(초)
    private float footstepTimer    = 0f;
    private bool  leftGround       = false;  // 공중에 떴다가 착지할 때만 착지음 재생

    // 외부 읽기용 (DeathZone 등에서 사용)
    public bool IsDead => isDead;

    // ═══════════════════════════════════════════════════════════════════════
    void Awake()
    {
        rb   = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        col  = GetComponent<Collider2D>();

        groundSensor = transform.Find("GroundSensor").GetComponent<Sensor_HeroKnight>();
        wallSensorR1 = transform.Find("WallSensor_R1").GetComponent<Sensor_HeroKnight>();
        wallSensorR2 = transform.Find("WallSensor_R2").GetComponent<Sensor_HeroKnight>();
        wallSensorL1 = transform.Find("WallSensor_L1").GetComponent<Sensor_HeroKnight>();
        wallSensorL2 = transform.Find("WallSensor_L2").GetComponent<Sensor_HeroKnight>();

        // 벽 슬라이딩(공중에서 벽에 붙었을 때)은 마찰로 달라붙어 멈추지 않도록 '공중'에선 마찰 0,
        // 바닥/경사/발판 위에선 정상 마찰을 줘서 미끄러지지 않게 한다. (전환은 UpdateGrounded에서)
        slickMat = new PhysicsMaterial2D("PlayerSlick") { friction = 0f,   bounciness = 0f };
        gripMat  = new PhysicsMaterial2D("PlayerGrip")  { friction = 0.6f, bounciness = 0f };
        ApplyFrictionMaterial(gripMat);

        // 물리는 FixedUpdate(50Hz)로 갱신되지만 카메라는 매 렌더 프레임 추적한다.
        // 보간을 켜지 않으면 빠르게 떨어질 때 위치가 스텝 단위로 끊겨 카메라가 뚝뚝 끊기고
        // 플레이어가 벽에 충돌하는 듯한 모션이 보인다 → Interpolate 로 프레임 사이를 부드럽게.
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
    }

    // 문영진: PlayerStats 연동 — 스탯 강화 시 이동/점프/대시 수치 반영
    void Start()
    {
        var s = PlayerStats.Instance;
        if (s == null) return;
        moveSpeed = s.MoveSpeed;
        jumpForce = s.JumpForce;
        dashSpeed = s.DashSpeed;
    }

    void Update()
    {
        if (isDead) return;
        timeSinceAttack += Time.deltaTime;
        UpdateGrounded();
        UpdateWallSlide();
        UpdateMovement();
        UpdateJumpCut();
        HandleInput();
        UpdateFootsteps();
        anim.SetFloat("AirSpeedY", rb.linearVelocity.y);
    }

    // 달리기 발소리: 바닥에서 이동 중일 때 일정 간격으로 재생
    void UpdateFootsteps()
    {
        float moveX  = Input.GetAxisRaw("Horizontal");
        bool  moving = grounded && !isDashing && !IsAnyPanelOpen && Mathf.Abs(moveX) > 0.05f;
        if (moving)
        {
            footstepTimer -= Time.deltaTime;
            if (footstepTimer <= 0f)
            {
                SfxManager.Instance?.Play(SfxManager.Run, 0.6f);
                footstepTimer = footstepInterval;
            }
        }
        else footstepTimer = 0f; // 멈췄다 다시 걸으면 첫 발소리 즉시
    }

    void UpdateGrounded()
    {
        bool state = groundSensor.State();
        if (!grounded && state)
        {
            grounded = true; jumpCount = 0; anim.SetBool("Grounded", true);
            if (leftGround) { SfxManager.Instance?.Play(SfxManager.Land); leftGround = false; }
        }
        else if (grounded && !state)
        { grounded = false; anim.SetBool("Grounded", false); leftGround = true; }

        // 바닥에선 정상 마찰(경사/발판 미끄러짐 방지), 공중에선 마찰 0(벽 슬라이딩 자연스럽게)
        ApplyFrictionMaterial(grounded ? gripMat : slickMat);
    }

    // 물리 머티리얼 전환 (실제로 바뀔 때만 적용)
    void ApplyFrictionMaterial(PhysicsMaterial2D m)
    {
        if (m == null || currentMat == m) return;
        currentMat = m;
        if (col != null) col.sharedMaterial = m;
        if (rb  != null) rb.sharedMaterial  = m;
    }

    void UpdateWallSlide()
    {
        isWallSliding = (wallSensorR1.State() && wallSensorR2.State())
                     || (wallSensorL1.State() && wallSensorL2.State());
        anim.SetBool("WallSlide", isWallSliding);
    }

    // 문영진: NPC 패널/스탯 강화 패널이 열려있으면 입력 차단
    bool IsAnyPanelOpen =>
        (NPCPanelUI.Instance != null && NPCPanelUI.Instance.IsOpen) ||
        (StatUpgradePanelUI.Instance != null && StatUpgradePanelUI.Instance.IsOpen) ||
        (StatUpgradeTerminalUI.Instance != null && StatUpgradeTerminalUI.Instance.IsOpen);

    void UpdateMovement()
    {
        if (isDashing) return;
        if (IsAnyPanelOpen)
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            return;
        }

        float moveX = Input.GetAxisRaw("Horizontal");
        rb.linearVelocity = new Vector2(moveX * moveSpeed, rb.linearVelocity.y);
        if      (moveX >  0.05f && !facingRight) Flip(true);
        else if (moveX < -0.05f &&  facingRight) Flip(false);
    }

    void UpdateJumpCut()
    {
        if (Input.GetKeyUp(KeyCode.Space) && rb.linearVelocity.y > 0f && !isDashing)
            rb.linearVelocity = new Vector2(
                rb.linearVelocity.x, rb.linearVelocity.y * jumpCutMultiplier);
    }

    void HandleInput()
    {
        if (StatUpgradeTerminalUI.Instance != null &&
            StatUpgradeTerminalUI.Instance.IsOpen &&
            Input.GetKeyDown(KeyCode.E))
        {
            StatUpgradeTerminalUI.Instance.Close();
            return;
        }

        if (IsAnyPanelOpen) return;

        float moveX     = Input.GetAxisRaw("Horizontal");
        int   facingDir = facingRight ? 1 : -1;

        // 문영진: NPC / 던전입구 / 스탯 강화 상호작용 (E키)
        if (Input.GetKeyDown(KeyCode.E))
        {
            if (Interactable.Current != null)
            {
                Interactable.Current.Interact();
                return;
            }
        }

        // Hurt (Q — 테스트용)
        if (Input.GetKeyDown(KeyCode.Q))
        {
            anim.SetTrigger("Hurt");
            return;
        }

        // Attack (좌클릭)
        if (Input.GetMouseButtonDown(0) && timeSinceAttack > attackCooldown && !isDashing)
        {
            currentAttack++;
            if (currentAttack > 3)                  currentAttack = 1;
            if (timeSinceAttack > attackComboWindow) currentAttack = 1;
            anim.SetTrigger("Attack" + currentAttack);
            timeSinceAttack = 0f;
            SfxManager.Instance?.Play(SfxManager.Hit);
            StartCoroutine(AttackHitRoutine(currentAttack));
            return;
        }

        if (Input.GetMouseButtonDown(1) && !isDashing)
        { anim.SetTrigger("Block"); anim.SetBool("IdleBlock", true); }
        if (Input.GetMouseButtonUp(1))
            anim.SetBool("IdleBlock", false);

        if (Input.GetKeyDown(KeyCode.LeftShift) && !isDashing && !isWallSliding
            && Time.time >= lastDashTime + dashCooldown)
        { StartCoroutine(DashRoutine(moveX, facingDir)); return; }

        if (Input.GetKeyDown(KeyCode.Space) && Input.GetKey(KeyCode.S) && grounded)
        { StartCoroutine(DropThrough()); return; }

        if (Input.GetKeyDown(KeyCode.Space) && !isDashing)
        {
            if (grounded)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
                rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
                jumpCount = 1;
                anim.SetTrigger("Jump");
                SfxManager.Instance?.Play(SfxManager.Jump);
                grounded = false;
                anim.SetBool("Grounded", false);
                groundSensor.Disable(0.2f);
            }
            else if (jumpCount <= extraJumps && extraJumps > 0)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
                rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
                jumpCount++;
                anim.SetTrigger("Jump");
                SfxManager.Instance?.Play(SfxManager.Jump);
            }
            return;
        }

        if (Mathf.Abs(moveX) > Mathf.Epsilon)
        { delayToIdle = 0.05f; anim.SetInteger("AnimState", 1); }
        else
        {
            delayToIdle -= Time.deltaTime;
            if (delayToIdle < 0f) anim.SetInteger("AnimState", 0);
        }
    }

    // ── 피격 / 회복 (정인규) ──────────────────────────────────────────────
    public void TakeDamage(float amount)
    {
        if (isDead || isInvincible) return;
        if (healthBar != null) healthBar.TakeDamage(amount);
        if (healthBar != null && healthBar.IsDead) { Die(); return; }
        anim.SetTrigger("Hurt");
        SfxManager.Instance?.Play(SfxManager.Hurt);
        StartCoroutine(InvincibleRoutine());
    }

    public void Heal(float amount)
    {
        if (isDead) return;
        if (healthBar != null) healthBar.Heal(amount);
    }

    // ── 사망 처리 — public으로 외부(DeathZone 등)에서도 호출 가능 ─────────
    public void Die()
    {
        if (isDead) return;
        isDead = true;

        rb.linearVelocity = Vector2.zero;

        // healthBar 가 Inspector에 연결된 경우 HP 를 0 으로
        if (healthBar != null) healthBar.TakeDamage(healthBar.MaxHp);

        anim.SetBool("noBlood", noBlood);
        anim.SetTrigger("Death");
        SfxManager.Instance?.Play(SfxManager.Death);

        Debug.Log("[Player] 사망!");
        if (GameOverController.Instance != null)
            GameOverController.Instance.ShowGameOverDelayed(1.2f);
    }

    // ── 무적 코루틴 (정인규) ──────────────────────────────────────────────
    IEnumerator InvincibleRoutine()
    {
        isInvincible = true;
        yield return new WaitForSeconds(invincibleTime);
        isInvincible = false;
    }

    // ── 대시 코루틴 ───────────────────────────────────────────────────────
    IEnumerator DashRoutine(float moveX, int facingDir)
    {
        isDashing    = true;
        lastDashTime = Time.time;
        anim.SetTrigger("Roll");
        SfxManager.Instance?.Play(SfxManager.Roll);
        float dir             = Mathf.Abs(moveX) > 0.1f ? Mathf.Sign(moveX) : facingDir;
        float originalGravity = rb.gravityScale;
        rb.gravityScale   = 0f;
        rb.linearVelocity = new Vector2(dir * dashSpeed, 0f);
        yield return new WaitForSeconds(dashDuration);
        rb.gravityScale = originalGravity;
        isDashing       = false;
    }

    // ── 공격 히트박스 코루틴 (정인규) ────────────────────────────────────
    IEnumerator AttackHitRoutine(int attackNum)
    {
        float delay = attackNum == 1 ? 0.15f : attackNum == 2 ? 0.20f : 0.25f;
        yield return new WaitForSeconds(delay);
        if (isDead) yield break;

        float   dir    = facingRight ? 1f : -1f;
        Vector2 center = (Vector2)transform.position
                       + new Vector2(dir * attackHitOffset, 1.2f);
        Vector2 size   = new Vector2(attackHitWidth, attackHitHeight);

        // Monster 레이어만 탐색
        int monsterMask = LayerMask.GetMask("Monster");
        Collider2D[]        cols    = Physics2D.OverlapBoxAll(center, size, 0f, monsterMask);
        HashSet<MonsterHit> damaged = new HashSet<MonsterHit>();
        foreach (var c in cols)
        {
            // transform.root는 씬 최상위를 반환해 방 자식 몬스터에서 실패.
            // Monster 태그 조상을 찾은 뒤 그 하위에서 MonsterHit 탐색.
            Transform t = c.transform;
            while (t != null && !t.CompareTag("Monster")) t = t.parent;
            MonsterHit mh = t != null ? t.GetComponentInChildren<MonsterHit>(true) : null;
            if (mh != null && damaged.Add(mh))
                mh.TakeDamage(attackDamage);
        }
    }

    // ── 플랫폼 하강 코루틴 ────────────────────────────────────────────────
    IEnumerator DropThrough()
    {
        RaycastHit2D hit = Physics2D.Raycast(
            transform.position, Vector2.down, 1.0f, LayerMask.GetMask("Ground"));
        if (hit.collider != null && hit.collider.CompareTag("OneWayPlatform"))
        {
            Physics2D.IgnoreCollision(col, hit.collider, true);
            yield return new WaitForSeconds(0.4f);
            Physics2D.IgnoreCollision(col, hit.collider, false);
        }
    }

    // ── 방향 전환 ─────────────────────────────────────────────────────────
    void Flip(bool toRight)
    {
        facingRight = toRight;
        Vector3 s   = transform.localScale;
        s.x         = Mathf.Abs(s.x) * (toRight ? 1f : -1f);
        transform.localScale = s;
    }

    // ── 애니메이션 이벤트 : 벽 슬라이드 파티클 ──────────────────────────
    void AE_SlideDust()
    {
        if (slideDust == null) return;
        Vector3 pos = facingRight
            ? wallSensorR2.transform.position
            : wallSensorL2.transform.position;
        GameObject dust = Instantiate(slideDust, pos, transform.localRotation);
        dust.transform.localScale = new Vector3(facingRight ? 1 : -1, 1, 1);
    }
}
