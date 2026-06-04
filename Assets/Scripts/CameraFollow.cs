using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// 플레이어를 따라가는 2D 카메라 컨트롤러
/// [DefaultExecutionOrder(1000)] 로 모든 다른 스크립트의 Awake/Start/LateUpdate 이후 실행
/// → MapManager.Start() 가 플레이어를 이미 배치한 뒤 CameraFollow.Start() 가 호출되므로
///    코루틴(yield return null) 없이 즉시 스냅 가능
/// </summary>
[DefaultExecutionOrder(1000)]
public class CameraFollow : MonoBehaviour
{
    [Header("추적 대상")]
    public Transform target;

    [Header("이동 설정")]
    public float smoothTime   = 0.15f;
    public float snapDistance = 8f;   // 이 거리 이상이면 즉시 스냅

    [Header("경계 보정 (단위: 유니티)")]
    [Tooltip("오른쪽 경계를 이 값만큼 안쪽으로 당김. 오른쪽 공허가 보이면 값을 올려줘.")]
    public float rightPadding = 2f;
    [Tooltip("왼쪽 경계 보정")]
    public float leftPadding  = 0f;
    [Tooltip("위쪽 경계 보정")]
    public float topPadding   = 0f;
    [Tooltip("아래쪽 경계 보정")]
    public float bottomPadding = 0f;

    [Header("디버그")]
    public bool showGizmos = true;

    // ─── 내부 상태 ─────────────────────────────────────────────
    private Vector3 velocity = Vector3.zero;
    private float   camW, camH;

    private float minX = float.MinValue, maxX = float.MaxValue;
    private float minY = float.MinValue, maxY = float.MaxValue;
    private bool  hasBounds = false;

    // ═══════════════════════════════════════════════════════════
    void Awake()
    {
        Camera cam = GetComponent<Camera>();
        if (cam != null)
        {
            camH = cam.orthographicSize * 2f;
            camW = camH * cam.aspect;
            // 배경색 검정으로 설정 → 공허가 하늘색 대신 검정으로 보임
            cam.backgroundColor = Color.black;
        }

        // ── Pixel Perfect Camera 비활성화 (위치 덮어쓰기 방지) ──
        foreach (var b in GetComponents<Behaviour>())
        {
            if (b.GetType().Name == "PixelPerfectCamera")
            {
                b.enabled = false;
                Debug.Log("[CameraFollow] Pixel Perfect Camera 비활성화 완료");
                break;
            }
        }

        if (target == null) TryFindPlayer();
    }

    void Start()
    {
        // [DefaultExecutionOrder(1000)] 로 인해 MapManager.Start() 가 먼저 실행됨
        // → 플레이어가 이미 startRoom 에 배치된 상태이므로 코루틴 없이 즉시 스냅
        if (target == null) TryFindPlayer();
        SnapToTarget();
    }

    /// <summary>
    /// 카메라를 target 위치로 즉시 스냅. MapManager에서 플레이어 이동 후 호출 가능.
    /// </summary>
    public void SnapToTarget()
    {
        if (target == null) TryFindPlayer();
        if (target == null) return;

        transform.position = new Vector3(
            target.position.x,
            target.position.y,
            transform.position.z);
        velocity = Vector3.zero;
        Debug.Log($"[CameraFollow] 스냅 → {transform.position}");
    }

    void TryFindPlayer()
    {
        GameObject p = GameObject.FindWithTag("Player");
        if (p != null)
        {
            target = p.transform;
            Debug.Log("[CameraFollow] Player 태그로 탐색 성공");
            return;
        }
        var pc = FindAnyObjectByType<PlayerController>();
        if (pc != null)
        {
            target = pc.transform;
            Debug.Log("[CameraFollow] PlayerController 로 탐색 성공");
        }
    }

    // ── 매 프레임 카메라 이동 (모든 LateUpdate 이후 실행) ──────
    void LateUpdate()
    {
        if (target == null) { TryFindPlayer(); return; }

        float tx = target.position.x;
        float ty = target.position.y;
        float cx = transform.position.x;
        float cy = transform.position.y;

        float dist = Vector2.Distance(new Vector2(cx, cy), new Vector2(tx, ty));

        // 포탈 이동 등 → bounds 클램프 후 즉시 스냅
        if (dist > snapDistance)
        {
            float sx = hasBounds ? Mathf.Clamp(tx, minX, maxX) : tx;
            float sy = hasBounds ? Mathf.Clamp(ty, minY, maxY) : ty;
            transform.position = new Vector3(sx, sy, transform.position.z);
            velocity = Vector3.zero;
            return;
        }

        // 일반 추적 : 방 경계 안에서만 클램프
        float destX = hasBounds ? Mathf.Clamp(tx, minX, maxX) : tx;
        float destY = hasBounds ? Mathf.Clamp(ty, minY, maxY) : ty;

        float x = Mathf.SmoothDamp(cx, destX, ref velocity.x, smoothTime);
        float y = Mathf.SmoothDamp(cy, destY, ref velocity.y, smoothTime);

        transform.position = new Vector3(x, y, transform.position.z);
    }

    // ── MapManager 에서 호출하는 Bounds 설정 ─────────────────
    public void SetRoomBounds(Bounds worldBounds)
    {
        minX = worldBounds.min.x + camW / 2f + leftPadding;
        maxX = worldBounds.max.x - camW / 2f - rightPadding;
        minY = worldBounds.min.y + camH / 2f + bottomPadding;
        maxY = worldBounds.max.y - camH / 2f - topPadding;

        // 방이 카메라보다 좁을 때: 삭제 대신 방 중앙에 고정
        if (minX > maxX) { float cx = (worldBounds.min.x + worldBounds.max.x) / 2f; minX = maxX = cx; }
        if (minY > maxY) { float cy = (worldBounds.min.y + worldBounds.max.y) / 2f; minY = maxY = cy; }

        hasBounds = true;
        Debug.Log($"[CameraFollow] Bounds X({minX:F1}~{maxX:F1}) Y({minY:F1}~{maxY:F1}) padding R={rightPadding}");
    }

    public void SetRoomBounds(Tilemap tilemap)
    {
        if (tilemap == null) { ClearBounds(); return; }
        tilemap.CompressBounds();
        BoundsInt bi = tilemap.cellBounds;
        var b = new Bounds();
        b.SetMinMax(tilemap.CellToWorld(bi.min),
                    tilemap.CellToWorld(bi.max) + tilemap.layoutGrid.cellSize);
        SetRoomBounds(b);
    }

    public void ClearBounds()
    {
        minX = float.MinValue; maxX = float.MaxValue;
        minY = float.MinValue; maxY = float.MaxValue;
        hasBounds = false;
    }

    void OnDrawGizmos()
    {
        if (!showGizmos || !hasBounds) return;
        Gizmos.color = new Color(0f, 1f, 0f, 0.4f);
        Gizmos.DrawWireCube(
            new Vector3((minX + maxX) / 2f, (minY + maxY) / 2f, 0f),
            new Vector3(maxX - minX, maxY - minY, 0f));
    }
}
