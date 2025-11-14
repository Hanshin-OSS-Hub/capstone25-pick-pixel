using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 7.5f;
    public float jumpForce = 14f;
    public int extraJumps = 1;

    [Header("Variable Jump")]
    public float jumpCutMultiplier = 0.5f; // 점프 키를 짧게 눌렀을 때 속도 감소 비율

    [Header("Dash Settings")]
    public float dashSpeed = 18f;
    public float dashDuration = 0.15f;
    public float dashCooldown = 0.5f;

    [Header("Ground Check")]
    public Transform groundCheck;
    public LayerMask groundMask;
    private const float groundRadius = 0.18f;

    private Rigidbody2D rb;
    private bool facingRight = true;
    private bool isGrounded;
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
        // Ground check
        bool wasGrounded = isGrounded;
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundRadius, groundMask);
        if (isGrounded && !wasGrounded)
            jumpCount = 0;

        float moveX = Input.GetAxisRaw("Horizontal");

        if (!isDashing)
            rb.linearVelocity = new Vector2(moveX * moveSpeed, rb.linearVelocity.y);

        if (moveX > 0.05f && !facingRight) Flip(true);
        else if (moveX < -0.05f && facingRight) Flip(false);

        // 점프 입력
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (isGrounded || jumpCount < extraJumps)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
                rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
                jumpCount++;
            }
        }

        // 점프 키 홀드 상태 저장
        if (Input.GetKey(KeyCode.Space)) isJumpHeld = true;
        else isJumpHeld = false;

        // 짧게 누를 때 상승 중 속도 줄이기
        if (!isJumpHeld && rb.linearVelocity.y > 0f)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * jumpCutMultiplier);
        }

        // 대시
        if (Input.GetKeyDown(KeyCode.LeftShift) && !isDashing && Time.time >= lastDashTime + dashCooldown)
        {
            StartCoroutine(Dash(moveX));
        }
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
        Vector3 s = transform.localScale;
        s.x = Mathf.Abs(s.x) * (toRight ? 1 : -1);
        transform.localScale = s;
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (groundCheck == null) return;
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(groundCheck.position, groundRadius);
    }
#endif
}
