using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// DeathZone — 플레이어가 낙사 구역에 닿으면 즉사 처리
///
/// 사용법:
/// 1. DeathZone용 빈 오브젝트(또는 기존 Trigger 오브젝트)에 이 스크립트 부착
/// 2. Collider2D 를 추가하고 반드시 [Is Trigger] ✓ 체크
/// 3. Player 오브젝트의 Tag 가 "Player" 로 등록되어 있어야 함
/// 4. Stage1_Combat_D / Stage1_Vertical_B 의 DeathZone 오브젝트에 적용
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class DeathZone : MonoBehaviour
{
    [Header("=== 씬 재로드 설정 ===")]
    [Tooltip("true: 사망 후 씬을 다시 로드 (새 런 시작)\nfalse: GameOverController 가 패널을 띄워 처리")]
    [SerializeField] private bool reloadScene = false;

    [Tooltip("사망 애니메이션이 끝난 뒤 씬을 재로드할 때까지 기다리는 시간(초)")]
    [SerializeField] private float respawnDelay = 1.8f;

    // ─────────────────────────────────────────────────────────────────────
    private void Awake()
    {
        // Trigger 설정 강제
        Collider2D col = GetComponent<Collider2D>();
        col.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Player 태그 확인
        if (!other.CompareTag("Player")) return;

        PlayerController pc = other.GetComponent<PlayerController>();

        // 이미 사망 상태면 무시
        if (pc == null || pc.IsDead) return;

        pc.Die();
        Debug.Log($"[DeathZone] '{gameObject.name}' 구역 낙사 → 플레이어 사망");

        // GameOverController 가 패널을 띄우는 경우 씬 재로드는 생략(중복 처리 방지)
        if (reloadScene && GameOverController.Instance == null)
            StartCoroutine(ReloadAfterDelay());
    }

    // ── 씬 재로드 코루틴 ─────────────────────────────────────────────────
    private IEnumerator ReloadAfterDelay()
    {
        yield return new WaitForSeconds(respawnDelay);
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    // ── 에디터에서 구역 시각화 ───────────────────────────────────────────
    private void OnDrawGizmos()
    {
        Collider2D col = GetComponent<Collider2D>();
        if (col == null) return;

        Gizmos.color = new Color(1f, 0f, 0f, 0.35f);

        if (col is BoxCollider2D box)
        {
            Gizmos.matrix = Matrix4x4.TRS(
                transform.position + (Vector3)box.offset,
                transform.rotation,
                transform.lossyScale);
            Gizmos.DrawCube(Vector3.zero, box.size);
            Gizmos.color = new Color(1f, 0f, 0f, 0.9f);
            Gizmos.DrawWireCube(Vector3.zero, box.size);
        }
        else
        {
            Gizmos.DrawSphere(transform.position, 0.3f);
        }
    }
}
