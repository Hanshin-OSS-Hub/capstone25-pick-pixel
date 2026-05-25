using UnityEngine;
using System.Collections;

/// <summary>
/// PlayerController - HeroKnight 애니메이터 기반 통합 컨트롤러
///
/// 애니메이터 파라미터 (HeroKnight_AnimController 그대로 사용)
///   AnimState  (Int)    : 0 = Idle, 1 = Run
///   AirSpeedY  (Float)  : 수직 속도
///   Grounded   (Bool)   : 착지 여부
///   WallSlide  (Bool)   : 벽 슬라이드 여부
///   IdleBlock  (Bool)   : 블록 유지 여부
///   noBlood    (Bool)   : 피 없는 사망 여부
///   Jump / Attack1 / Attack2 / Attack3 / Block / Roll / Hurt / Death (Trigger)
///
/// 씬 오브젝트 구조
///   Player
///   ├─ GroundSensor      (Sensor_HeroKnight)
///   ├─ WallSensor_R1     (Sensor_HeroKnight)
///   ├─ WallSensor_R2     (Sensor_HeroKnight)
///   ├─ WallSensor_L1     (Sensor_HeroKnight)
///   └─ WallSensor_L2     (Sensor_HeroKnight)
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(Collider2D))]
public class PlayerController : MonoBehaviour
{
    // ── 이동 ────────────────────────────────────────────────────────────
    [Header("Movement")]
    public float moveSpeed = 7.5f;

    // ── 점프 ────────────────────────────────────────────────────────────
    [Header("Jump")]
    public float jumpForce = 14f;
    public int extraJumps = 1;       // 0 = 단일 점프, 1 = 더블 점프
    public float jumpCutMultiplier = 0.5f;    // 점프 버튼 일찍 떼면 상승 단축

    // ── 대시 / 롤 ────────────────────────────────────────────────────────
    [Header("Dash / Roll")]
    public float dashSpeed = 18f;
    public float dashDuration = 0.15f;
    public float dashCooldown = 0.5f;

    // ── 공격 ─────────────────────────────────────────────────────────────
    [Header("Attack")]
    public float attackCooldown = 0.25f;   // 최소 클릭 간격
    public float attackComboWindow = 1.0f;    // 이 시간 초과 시 콤보 리셋

    // ── 기타 ─────────────────────────────────────────────────────────────
    [Header("Misc")]
    public bool noBlood = false;
    public GameObject slideDust = null;       // 벽 슬라이드 파티클 (없어도 동작)

    // ── 컴포넌트 ─────────────────────────────────────────────────────────
    private Rigidbody2D rb;
    private Animator anim;
    private Collider2D col;

    private Sensor_HeroKnight groundSensor;
    private Sensor_HeroKnight wallSensorR1;
    private Sensor_HeroKnight wallSensorR2;
    private Sensor_HeroKnight wallSensorL1;
    private Sensor_HeroKnight wallSensorL2;

    // ── 내부 상태 ─────────────────────────────────────────────────────────
    private bool facingRight = true;
    private bool grounded = false;
    private bool isWallSliding = false;
    private bool isDashing = false;
    private bool isJumpHeld = false;
    private bool isDead = false;

    private int jumpCount = 0;
    private int currentAttack = 0;
    private float timeSinceAttack = 0f;
    private float delayToIdle = 0f;
    private float lastDashTime = -999f;

    // 외부 읽기용
    public bool IsDead => isDead;

    // ═════════════════════════════════════════════════════════════════════
    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        col = GetComponent<Collider2D>();

        groundSensor = transform.Find("GroundSensor").GetComponent<Sensor_HeroKnight>();
        wallSensorR1 = transform.Find("WallSensor_R1").GetComponent<Sensor_HeroKnight>();
        wallSensorR2 = transform.Find("WallSensor_R2").GetComponent<Sensor_HeroKnight>();
        wallSensorL1 = transform.Find("WallSensor_L1").GetComponent<Sensor_HeroKnight>();
        wallSensorL2 = transform.Find("WallSensor_L2").GetComponent<Sensor_HeroKnight>();
    }

    // ═════════════════════════════════════════════════════════════════════
    void Update()
    {
        timeSinceAttack += Time.deltaTime;

        UpdateGrounded();
        UpdateWallSlide();
        UpdateMovement();
        UpdateJumpCut();
        HandleInput();

        anim.SetFloat("AirSpeedY", rb.linearVelocity.y);
    }

    // ── 지면 감지 ─────────────────────────────────────────────────────────
    void UpdateGrounded()
    {
        bool state = groundSensor.State();

        if (!grounded && state)
        {
            grounded = true;
            jumpCount = 0;
            anim.SetBool("Grounded", true);
        }
        else if (grounded && !state)
        {
            grounded = false;
            anim.SetBool("Grounded", false);
        }
    }

    // ── 벽 슬라이드 감지 ─────────────────────────────────────────────────
    void UpdateWallSlide()
    {
        isWallSliding = (wallSensorR1.State() && wallSensorR2.State())
                     || (wallSensorL1.State() && wallSensorL2.State());
        anim.SetBool("WallSlide", isWallSliding);
    }

    // ── 이동 ─────────────────────────────────────────────────────────────
    void UpdateMovement()
    {
        if (isDashing) return;

        float moveX = Input.GetAxisRaw("Horizontal");
        rb.linearVelocity = new Vector2(moveX * moveSpeed, rb.linearVelocity.y);

        if (moveX > 0.05f && !facingRight) Flip(true);
        else if (moveX < -0.05f && facingRight) Flip(false);
    }

    // ── 점프 컷 ───────────────────────────────────────────────────────────
    void UpdateJumpCut()
    {
        isJumpHeld = Input.GetKey(KeyCode.Space);
        if (!isJumpHeld && rb.linearVelocity.y > 0f && !isDashing)
            rb.linearVelocity = new Vector2(
                rb.linearVelocity.x,
                rb.linearVelocity.y * jumpCutMultiplier);
    }

    // ── 입력 처리 ─────────────────────────────────────────────────────────
    void HandleInput()
    {
        float moveX = Input.GetAxisRaw("Horizontal");
        int facingDir = facingRight ? 1 : -1;

        // Death (E)
        if (Input.GetKeyDown(KeyCode.E))
        {
            anim.SetBool("noBlood", noBlood);
            anim.SetTrigger("Death");
            return;
        }

        // Hurt (Q)
        if (Input.GetKeyDown(KeyCode.Q))
        {
            anim.SetTrigger("Hurt");
            return;
        }

        // Attack (좌클릭)
        if (Input.GetMouseButtonDown(0)
            && timeSinceAttack > attackCooldown
            && !isDashing)
        {
            currentAttack++;
            if (currentAttack > 3) currentAttack = 1;
            if (timeSinceAttack > attackComboWindow) currentAttack = 1;

            anim.SetTrigger("Attack" + currentAttack);
            timeSinceAttack = 0f;
            return;
        }

        // Block (우클릭)
        if (Input.GetMouseButtonDown(1) && !isDashing)
        {
            anim.SetTrigger("Block");
            anim.SetBool("IdleBlock", true);
        }
        if (Input.GetMouseButtonUp(1))
            anim.SetBool("IdleBlock", false);

        // Dash / Roll (Left Shift)
        if (Input.GetKeyDown(KeyCode.LeftShift)
            && !isDashing
            && !isWallSliding
            && Time.time >= lastDashTime + dashCooldown)
        {
            StartCoroutine(DashRoutine(moveX, facingDir));
            return;
        }

        // 플랫폼 하강 (S + Space)
        if (Input.GetKeyDown(KeyCode.Space)
            && Input.GetKey(KeyCode.S)
            && grounded)
        {
            StartCoroutine(DropThrough());
            return;
        }

        // Jump / Double Jump (Space)
        if (Input.GetKeyDown(KeyCode.Space) && !isDashing)
        {
            if (grounded)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
                rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
                jumpCount = 1;

                anim.SetTrigger("Jump");
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
            }
            return;
        }

        // Run / Idle (AnimState)
        if (Mathf.Abs(moveX) > Mathf.Epsilon)
        {
            delayToIdle = 0.05f;
            anim.SetInteger("AnimState", 1);
        }
        else
        {
            delayToIdle -= Time.deltaTime;
            if (delayToIdle < 0f)
                anim.SetInteger("AnimState", 0);
        }
    }

    // ── Dash 코루틴 ───────────────────────────────────────────────────────
    IEnumerator DashRoutine(float moveX, int facingDir)
    {
        isDashing = true;
        lastDashTime = Time.time;

        anim.SetTrigger("Roll");

        float dir = Mathf.Abs(moveX) > 0.1f ? Mathf.Sign(moveX) : facingDir;
        float originalGravity = rb.gravityScale;

        rb.gravityScale = 0f;
        rb.linearVelocity = new Vector2(dir * dashSpeed, 0f);

        yield return new WaitForSeconds(dashDuration);

        rb.gravityScale = originalGravity;
        isDashing = false;
    }

    // ── 플랫폼 하강 코루틴 ────────────────────────────────────────────────
    IEnumerator DropThrough()
    {
        RaycastHit2D hit = Physics2D.Raycast(
            transform.position, Vector2.down, 1.0f,
            LayerMask.GetMask("Ground"));

        if (hit.collider != null && hit.collider.CompareTag("OneWayPlatform"))
        {
            Physics2D.IgnoreCollision(col, hit.collider, true);
            yield return new WaitForSeconds(0.4f);
            Physics2D.IgnoreCollision(col, hit.collider, false);
        }
    }

    // ── 사망 처리 (DeathZone 등 외부에서 호출) ───────────────────────────
    /// <summary>
    /// 플레이어를 즉사시킨다.
    /// DeathZone / 몬스터 즉사 공격 등에서 호출.
    /// 중복 호출은 무시된다.
    /// </summary>
    public void Die()
    {
        if (isDead) return;
        isDead = true;

        // 이동 즉시 정지
        rb.linearVelocity = Vector2.zero;

        // HealthBar HP → 0
        HealthBar hb = GetComponentInChildren<HealthBar>();
        if (hb == null)
            hb = FindFirstObjectByType<HealthBar>();
        if (hb != null)
            hb.TakeDamage(hb.MaxHp);

        // Death 애니메이션 재생
        anim.SetBool("noBlood", noBlood);
        anim.SetTrigger("Death");

        // 입력 차단 (Update 중단)
        enabled = false;

        Debug.Log("[PlayerController] 플레이어 사망");
    }

    // ── 방향 전환 ─────────────────────────────────────────────────────────
    void Flip(bool toRight)
    {
        facingRight = toRight;
        Vector3 s = transform.localScale;
        s.x = Mathf.Abs(s.x) * (toRight ? 1f : -1f);
        transform.localScale = s;
    }

    // ── 애니메이션 이벤트 : 벽 슬라이드 파티클 ───────────────────────────
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