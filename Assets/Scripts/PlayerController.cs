using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 7.5f;

    [Header("Jump")]
    public float jumpForce = 14f;
    public int extraJumps = 1;     // 2단 점프 횟수
    public float jumpCutMultiplier = 0.5f;

    [Header("Dash")]
    public float dashSpeed = 18f;
    public float dashDuration = 0.15f;
    public float dashCooldown = 0.5f;

    [Header("References")]
    public GroundCheck ground;     // GroundCheck 오브젝트 연결
    public Animator animator;      // Player Animator 연결

    private Rigidbody2D rb;
    private bool facingRight = true;

    private bool isJumpHeld;
    private int jumpCount;

    private bool isDashing = false;
    private float lastDashTime = -999f;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        float moveX = Input.GetAxisRaw("Horizontal");

        // === 이동 ===
        if (!isDashing)
            rb.linearVelocity = new Vector2(moveX * moveSpeed, rb.linearVelocity.y);

        // 방향 전환
        if (moveX > 0.05f && !facingRight) Flip(true);
        else if (moveX < -0.05f && facingRight) Flip(false);

        // === 점프 ===
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (ground.isGrounded || jumpCount < extraJumps)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
                rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
                jumpCount++;
            }
        }

        // 점프 홀드 유지 체크
        isJumpHeld = Input.GetKey(KeyCode.Space);

        // 키 떼면 상승 속도 약화 (가벼운 점프)
        if (!isJumpHeld && rb.linearVelocity.y > 0f)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * jumpCutMultiplier);
        }

        // === 착지하면 점프 카운트 리셋 ===
        if (ground.isGrounded)
            jumpCount = 0;

        // === 대시 ===
        if (Input.GetKeyDown(KeyCode.LeftShift)
            && !isDashing
            && Time.time >= lastDashTime + dashCooldown)
        {
            StartCoroutine(Dash(moveX));
        }

        // === 애니메이션 파라미터 업데이트 ===
        animator.SetFloat("Move", Mathf.Abs(moveX));
        animator.SetBool("IsGrounded", ground.isGrounded);
        animator.SetFloat("YVelocity", rb.linearVelocity.y);
    }

    private System.Collections.IEnumerator Dash(float moveX)
    {
        isDashing = true;
        lastDashTime = Time.time;

        float dir = (Mathf.Abs(moveX) > 0.1f) ? Mathf.Sign(moveX) : (facingRight ? 1f : -1f);
        float originalGravity = rb.gravityScale;

        rb.gravityScale = 0f;
        rb.linearVelocity = new Vector2(dir * dashSpeed, 0f);

        yield return new WaitForSeconds(dashDuration);

        rb.gravityScale = originalGravity;
        isDashing = false;
    }

    private void Flip(bool toRight)
    {
        facingRight = toRight;
        Vector3 scale = transform.localScale;
        scale.x = Mathf.Abs(scale.x) * (toRight ? 1 : -1);
        transform.localScale = scale;
    }
}
