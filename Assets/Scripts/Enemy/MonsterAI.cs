using UnityEngine;

public enum MonsterState
{
    Patrol,
    Chase
}

public class MonsterAI : MonoBehaviour
{
    [Header("State")]
    public MonsterState state = MonsterState.Patrol;

    [Header("Movement")]
    public float moveSpeed = 1.5f;
    private int moveDir = 1;

    [Header("Detect Player")]
    public Transform player;
    public float detectRange = 5f;

    [Header("Ground Check")]
    public Transform groundCheck;
    public float groundCheckDistance = 0.5f;
    public LayerMask groundLayer;   // Ground만 체크

    [Header("Patrol Control")]
    public float flipCooldown = 0.4f;
    private float lastFlipTime;

    private Rigidbody2D rb;

    // =========================
    // 초기화
    // =========================
    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    // =========================
    // 물리 업데이트
    // =========================
    void FixedUpdate()
    {
        switch (state)
        {
            case MonsterState.Patrol:
                Patrol();
                break;

            case MonsterState.Chase:
                Chase();
                break;
        }
    }

    // =========================
    // 순찰 상태
    // =========================
    void Patrol()
    {
        // 계속 이동
        rb.linearVelocity = new Vector2(moveDir * moveSpeed, rb.linearVelocity.y);

        // 낭떠러지 감지 (Ground만 인식)
        RaycastHit2D groundHit = Physics2D.Raycast(
            groundCheck.position,
            Vector2.down,
            groundCheckDistance,
            groundLayer
        );

        if (!groundHit && Time.time > lastFlipTime + flipCooldown)
        {
            Flip();
            lastFlipTime = Time.time;
        }

        // 플레이어 감지
        float dist = Vector2.Distance(transform.position, player.position);
        if (dist < detectRange)
        {
            state = MonsterState.Chase;
        }
    }

    // =========================
    // 추적 상태
    // =========================
    void Chase()
    {
        float dir = Mathf.Sign(player.position.x - transform.position.x);

        rb.linearVelocity = new Vector2(dir * moveSpeed, rb.linearVelocity.y);

        // 추적 중 방향 전환
        if (dir != moveDir && Time.time > lastFlipTime + flipCooldown)
        {
            Flip();
            lastFlipTime = Time.time;
        }

        // 플레이어 멀어지면 다시 순찰
        float dist = Vector2.Distance(transform.position, player.position);
        if (dist > detectRange)
        {
            state = MonsterState.Patrol;
        }
    }

    // =========================
    // 방향 전환
    // =========================
    void Flip()
    {
        moveDir *= -1;

        Vector3 scale = transform.localScale;
        scale.x = Mathf.Abs(scale.x) * moveDir;
        transform.localScale = scale;
    }

    // =========================
    // 디버그용 Ray 확인
    // =========================
    void OnDrawGizmos()
    {
        if (groundCheck == null) return;

        Gizmos.color = Color.red;
        Gizmos.DrawLine(
            groundCheck.position,
            groundCheck.position + Vector3.down * groundCheckDistance
        );
    }
}
