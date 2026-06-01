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

    private Sensor_HeroKnight groundSensor;
    private Sensor_HeroKnight wallSensorR1;
    private Sensor_HeroKnight wallSensorR2;
    private Sensor_HeroKnight wallSensorL1;
    private Sensor_HeroKnight wallSensorL2;

    private bool  facingRight   = true;
    private bool  grounded      = false;
    private bool  isWallSliding = false;
    private bool  isDashing     = false;
    private bool  isDead        = false;
    private bool  isInvincible  = false;

    private int   jumpCount       = 0;
    private int   currentAttack   = 0;
    private float timeSinceAttack = 0f;
    private float delayToIdle     = 0f;
    private float lastDashTime    = -999f;

    // 외부 읽기용 (DeathZone 등에서 사용)
    public bool IsDead => isDead;

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
        anim.SetFloat("AirSpeedY", rb.linearVelocity.y);
    }

    void UpdateGrounded()
    {
        bool state = groundSensor.State();
        if (!grounded && state)
        { grounded = true; jumpCount = 0; anim.SetBool("Grounded", true); }
        else if (grounded && !state)
        { grounded = false; anim.SetBool("Grounded", false); }
    }

    void UpdateWallSlide()
    {
        isWallSliding = (wallSensorR1.State() && wallSensorR2.State())
                     || (wallSensorL1.State() && wallSensorL2.State());
        anim.SetBool("WallSlide", isWallSliding);
    }

    void UpdateMovement()
    {
        if (isDashing) return;
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
        float moveX     = Input.GetAxisRaw("Horizontal");
        int   facingDir = facingRight ? 1 : -1;

        if (Input.GetMouseButtonDown(0) && timeSinceAttack > attackCooldown && !isDashing)
        {
            currentAttack++;
            if (currentAttack > 3)                  currentAttack = 1;
            if (timeSinceAttack > attackComboWindow) currentAttack = 1;
            anim.SetTrigger("Attack" + currentAttack);
            timeSinceAttack = 0f;
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

        if (Mathf.Abs(moveX) > Mathf.Epsilon)
        { delayToIdle = 0.05f; anim.SetInteger("AnimState", 1); }
        else
        {
            delayToIdle -= Time.deltaTime;
            if (delayToIdle < 0f) anim.SetInteger("AnimState", 0);
        }
    }

    public void TakeDamage(float amount)
    {
        if (isDead || isInvincible) return;
        if (healthBar != null) healthBar.TakeDamage(amount);
        if (healthBar != null && healthBar.IsDead) { Die(); return; }
        anim.SetTrigger("Hurt");
        StartCoroutine(InvincibleRoutine());
    }

    public void Heal(float amount)
    {
        if (isDead) return;
        if (healthBar != null) healthBar.Heal(amount);
    }

    // public으로 외부(DeathZone 등)에서도 호출 가능
    public void Die()
    {
        if (isDead) return;
        isDead = true;
        rb.linearVelocity = Vector2.zero;
        if (healthBar != null) healthBar.TakeDamage(healthBar.MaxHp);
        anim.SetBool("noBlood", noBlood);
        anim.SetTrigger("Death");
        Debug.Log("[Player] 사망!");
        if (GameOverController.Instance != null)
            GameOverController.Instance.ShowGameOverDelayed(1.2f);
    }

    IEnumerator InvincibleRoutine()
    {
        isInvincible = true;
        yield return new WaitForSeconds(invincibleTime);
        isInvincible = false;
    }

    IEnumerator DashRoutine(float moveX, int facingDir)
    {
        isDashing    = true;
        lastDashTime = Time.time;
        anim.SetTrigger("Roll");
        float dir             = Mathf.Abs(moveX) > 0.1f ? Mathf.Sign(moveX) : facingDir;
        float originalGravity = rb.gravityScale;
        rb.gravityScale   = 0f;
        rb.linearVelocity = new Vector2(dir * dashSpeed, 0f);
        yield return new WaitForSeconds(dashDuration);
        rb.gravityScale = originalGravity;
        isDashing       = false;
    }

    IEnumerator AttackHitRoutine(int attackNum)
    {
        // 콤보별 칼이 닿는 타이밍
        float delay = attackNum == 1 ? 0.15f : attackNum == 2 ? 0.20f : 0.25f;
        yield return new WaitForSeconds(delay);
        if (isDead) yield break;

        float   dir    = facingRight ? 1f : -1f;
        Vector2 center = (Vector2)transform.position
                       + new Vector2(dir * attackHitOffset, 1.2f);
        Vector2 size   = new Vector2(attackHitWidth, attackHitHeight);

        Collider2D[]        cols    = Physics2D.OverlapBoxAll(center, size, 0f);
        HashSet<MonsterHit> damaged = new HashSet<MonsterHit>();
        foreach (var col in cols)
        {
            // 루트 오브젝트에서 MonsterHit 탐색 (HitCollider 비활성 포함)
            MonsterHit mh = col.transform.root.GetComponentInChildren<MonsterHit>(true);
            if (mh != null && damaged.Add(mh))
                mh.TakeDamage(attackDamage);
        }
    }

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

    void Flip(bool toRight)
    {
        facingRight = toRight;
        Vector3 s   = transform.localScale;
        s.x         = Mathf.Abs(s.x) * (toRight ? 1f : -1f);
        transform.localScale = s;
    }

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
