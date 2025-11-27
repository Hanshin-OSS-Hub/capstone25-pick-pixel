using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 7.5f;

    [Header("Jump")]
    public float jumpForce = 14f;
    public int extraJumps = 1;
    public float jumpCutMultiplier = 0.5f;

    [Header("Dash")]
    public float dashSpeed = 18f;
    public float dashDuration = 0.15f;
    public float dashCooldown = 0.5f;

    [Header("References")]
    public GroundCheck ground;
    public Animator animator;

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

        // === 점프 & 더블점프 ===
        if (Input.GetKeyDown(KeyCode.Space))
        {
            // 첫 점프
            if (ground.isGrounded)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
                rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);

                jumpCount = 1;
                animator.SetTrigger("JumpStart");
            }
            // 더블점프
            else if (jumpCount < extraJumps)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
                rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);

                jumpCount++;
                animator.SetTrigger("DoubleJump"); 
            }
        }

        // 점프 짧게 누르기
        isJumpHeld = Input.GetKey(KeyCode.Space);

        if (!isJumpHeld && rb.linearVelocity.y > 0f)
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * jumpCutMultiplier);

        // === 착지 ===
        if (ground.isGrounded)
            jumpCount = 0;

        // === 대시 ===
        if (Input.GetKeyDown(KeyCode.LeftShift)
            && !isDashing
            && Time.time >= lastDashTime + dashCooldown)
        {
            StartCoroutine(Dash(moveX));
        }

        // === 애니메이션 파라미터 ===
        animator.SetFloat("Move", Mathf.Abs(moveX));
        animator.SetBool("IsGrounded", ground.isGrounded);
        animator.SetFloat("YVelocity", rb.linearVelocity.y);
    }

    private System.Collections.IEnumerator Dash(float moveX)
    {
        isDashing = true;
        lastDashTime = Time.time;

        animator.SetTrigger("Dash");

        float dir = (Mathf.Abs(moveX) > 0.1f) ? Mathf.Sign(moveX) : (facingRight ? 1 : -1);
        float originalGravity = rb.gravityScale;

        // 대시 중에는 중력 0
        rb.gravityScale = 0f;
        rb.linearVelocity = new Vector2(dir * dashSpeed, 0f);

        yield return new WaitForSeconds(dashDuration);

        // 원래 중력 복구
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
