using System.Collections;
using UnityEngine;

/// <summary>
/// 보스 AI - 근접(클럽 슬램) + 원거리(파이어볼) 복합 공격 패턴.
///
/// Animator 파라미터 (Boss.controller):
///   Speed    (Float)   : 이동 속도 → Idle/Walk 전환
///   Attack   (Trigger) : 클럽 슬램
///   Fireball (Trigger) : 파이어볼
///   Hit      (Trigger) : 피격
///   Dead     (Bool)    : 사망
///
/// 데미지 수신: 같은 오브젝트의 MonsterHit 이 HP/피격을 관리하며,
///   피격 시 PlayHit(), 사망 시 Die() 를 호출한다 (플레이어 OverlapBox 공격과 호환).
/// 데미지 부여: 공격 모션 시 사거리 안의 PlayerController.TakeDamage 호출.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Animator))]
public class BossAI : MonoBehaviour
{
    public enum BossState { Idle, Chase, Attack, Dead }

    [Header("State (읽기용)")]
    public BossState state = BossState.Idle;

    [Header("Movement")]
    public float moveSpeed = 2f;

    [Header("Detection / Ranges")]
    public float detectRange = 12f;
    public float meleeRange = 2.8f;
    public float fireballRange = 8f;

    [Header("Attack")]
    public float attackCooldown = 2f;     // 근접
    public float fireballCooldown = 5f;   // 원거리
    public int meleeDamage = 15;
    public int fireballDamage = 10;
    public float attackWindup = 0.35f;     // 데미지 적용까지 딜레이 (모션 임팩트 프레임)

    [Header("Refs")]
    public Transform player;
    public GameObject fireballPrefab;      // 선택: 투사체 프리팹 (없으면 즉발 판정)
    public Transform firePoint;            // 파이어볼 발사 위치 (선택)

    [Header("Flip")]
    public bool startFacingLeft = true;    // 보스 스프라이트가 왼쪽을 보면 true

    Rigidbody2D rb;
    Animator anim;
    float lastAttackTime = -99f;
    float lastFireballTime = -99f;
    bool isActing;          // 공격 모션 중
    bool isDead;
    int facing = -1;        // -1 = left, 1 = right

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        facing = startFacingLeft ? -1 : 1;
    }

    void Start()
    {
        if (player == null)
        {
            var p = GameObject.FindWithTag("Player");
            if (p != null) player = p.transform;
        }
        if (player == null)
            Debug.LogWarning($"[BossAI] {name}: Player를 찾지 못했습니다.");
    }

    void FixedUpdate()
    {
        if (isDead || player == null) return;
        if (isActing) { rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y); return; }

        float dist = Vector2.Distance(transform.position, player.position);

        if (dist > detectRange)
        {
            state = BossState.Idle;
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            anim.SetFloat("Speed", 0f);
            return;
        }

        FaceTowards(player.position.x);

        // 근접 사거리 → 클럽 슬램
        if (dist <= meleeRange && Time.time >= lastAttackTime + attackCooldown)
        {
            StartCoroutine(DoMelee());
            return;
        }
        // 원거리 → 파이어볼
        if (dist <= fireballRange && Time.time >= lastFireballTime + fireballCooldown)
        {
            StartCoroutine(DoFireball());
            return;
        }

        // 추격
        state = BossState.Chase;
        rb.linearVelocity = new Vector2(facing * moveSpeed, rb.linearVelocity.y);
        anim.SetFloat("Speed", Mathf.Abs(moveSpeed));
    }

    IEnumerator DoMelee()
    {
        isActing = true;
        state = BossState.Attack;
        lastAttackTime = Time.time;
        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        anim.SetFloat("Speed", 0f);
        anim.SetTrigger("Attack");

        yield return new WaitForSeconds(attackWindup);
        if (!isDead && player != null &&
            Vector2.Distance(transform.position, player.position) <= meleeRange + 0.5f)
            DealDamage(meleeDamage);

        yield return new WaitForSeconds(0.4f);
        isActing = false;
    }

    IEnumerator DoFireball()
    {
        isActing = true;
        state = BossState.Attack;
        lastFireballTime = Time.time;
        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        anim.SetFloat("Speed", 0f);
        anim.SetTrigger("Fireball");

        yield return new WaitForSeconds(attackWindup);
        if (!isDead && player != null)
        {
            if (fireballPrefab != null)
            {
                Vector3 spawn = firePoint != null ? firePoint.position : transform.position;
                var go = Instantiate(fireballPrefab, spawn, Quaternion.identity);
                var pj = go.GetComponent<BossProjectile>();
                if (pj != null) pj.Launch(new Vector2(facing, 0f), fireballDamage);
            }
            else if (Vector2.Distance(transform.position, player.position) <= fireballRange + 1f)
            {
                DealDamage(fireballDamage); // 투사체 프리팹 없으면 즉발 판정
            }
        }

        yield return new WaitForSeconds(0.5f);
        isActing = false;
    }

    void DealDamage(int dmg)
    {
        var pc = player.GetComponent<PlayerController>();
        if (pc != null) pc.TakeDamage(dmg);
    }

    void FaceTowards(float targetX)
    {
        int dir = targetX < transform.position.x ? -1 : 1;
        if (dir == facing) return;
        facing = dir;
        var s = transform.localScale;
        // startFacingLeft=true: 스프라이트 기본이 왼쪽을 봄.
        //   facing==-1(왼쪽) → 원본 그대로(+|x|), facing==1(오른쪽) → 반전(-|x|)
        float sign = startFacingLeft ? (facing == -1 ? 1f : -1f)
                                     : (facing ==  1 ? 1f : -1f);
        s.x = Mathf.Abs(s.x) * sign;
        transform.localScale = s;
    }

    /// <summary>MonsterHit 이 피격 시 호출 (사망이 아닐 때).</summary>
    public void PlayHit()
    {
        if (isDead) return;
        anim.SetTrigger("Hit");
    }

    /// <summary>MonsterHit 이 HP 0 일 때 호출.</summary>
    public void Die()
    {
        if (isDead) return;
        isDead = true;
        state = BossState.Dead;
        StopAllCoroutines();
        rb.linearVelocity = Vector2.zero;
        anim.SetBool("Dead", true);   // AnyState → Death 전이
        // 물리/충돌 정리
        foreach (var c in GetComponentsInChildren<Collider2D>()) c.enabled = false;
        Debug.Log("[BossAI] 보스 처치!");
        Destroy(gameObject, 2f);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;  Gizmos.DrawWireSphere(transform.position, detectRange);
        Gizmos.color = new Color(1f,0.5f,0f); Gizmos.DrawWireSphere(transform.position, fireballRange);
        Gizmos.color = Color.red;     Gizmos.DrawWireSphere(transform.position, meleeRange);
    }
}
