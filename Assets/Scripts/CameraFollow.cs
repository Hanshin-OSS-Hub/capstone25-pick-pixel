using UnityEngine;
using UnityEngine.Tilemaps;

public class CameraFollow : MonoBehaviour
{
    [Header("Follow Target")]
    public Transform target;

    [Header("Camera Settings")]
    public float smoothDampTime = 0.15f;
    public float snapDistance   = 10f;  // 방 이동 시 즉시 스냅 거리

    [Header("Debug")]
    public bool showGizmos = true;

    private Vector3 smoothVelocity = Vector3.zero;
    private float camWidth, camHeight;
    private float minX, maxX, minY, maxY;
    private bool hasBounds = false;

    void Awake()
    {
        Camera cam = Camera.main;
        camHeight = cam.orthographicSize * 2f;
        camWidth  = camHeight * cam.aspect;
        SetNoBounds();

        // Inspector에 target이 연결되지 않은 경우 Player 태그로 자동 탐색
        if (target == null)
        {
            GameObject player = GameObject.FindWithTag("Player");
            if (player != null)
            {
                target = player.transform;
                Debug.Log("[CameraFollow] Player 자동 탐색 성공");
            }
            else
            {
                Debug.LogWarning("[CameraFollow] Player 태그 오브젝트를 찾지 못했습니다. Inspector에서 target을 연결해주세요.");
            }
        }
    }

    void Start()
    {
        // 시작 시 카메라를 플레이어 위치로 즉시 스냅 (잘못된 위치에서 시작하는 문제 방지)
        if (target != null)
        {
            transform.position = new Vector3(
                Mathf.Clamp(target.position.x, minX, maxX),
                Mathf.Clamp(target.position.y, minY, maxY),
                transform.position.z);
            smoothVelocity = Vector3.zero;
        }
    }

    // MapManager에서 유니온 Bounds로 호출
    public void SetRoomBounds(Bounds worldBounds)
    {
        minX = worldBounds.min.x + camWidth  / 2f;
        maxX = worldBounds.max.x - camWidth  / 2f;
        minY = worldBounds.min.y + camHeight / 2f;
        maxY = worldBounds.max.y - camHeight / 2f;

        if (minX > maxX) { minX = float.MinValue; maxX = float.MaxValue; }
        if (minY > maxY) { minY = float.MinValue; maxY = float.MaxValue; }

        hasBounds = true;
        Debug.Log($"[CameraFollow] Bounds 갱신 → X({minX:F1}~{maxX:F1}) Y({minY:F1}~{maxY:F1})");
    }

    // 이름 기반 구버전 호환 (직접 Tilemap 넘길 때)
    public void SetRoomBounds(Tilemap tilemap)
    {
        if (tilemap == null) { SetNoBounds(); return; }

        tilemap.CompressBounds();
        BoundsInt bi = tilemap.cellBounds;
        Vector3 minW = tilemap.CellToWorld(bi.min);
        Vector3 maxW = tilemap.CellToWorld(bi.max) + tilemap.layoutGrid.cellSize;
        Bounds b = new Bounds();
        b.SetMinMax(minW, maxW);
        SetRoomBounds(b);
    }

    public void ClearBounds() { SetNoBounds(); }

    void SetNoBounds()
    {
        minX = float.MinValue; maxX = float.MaxValue;
        minY = float.MinValue; maxY = float.MaxValue;
        hasBounds = false;
    }

    void LateUpdate()
    {
        if (!target) return;

        Vector3 cameraPos = transform.position;
        Vector2 cam2D     = new Vector2(cameraPos.x, cameraPos.y);
        Vector2 target2D  = new Vector2(target.position.x, target.position.y);

        // 방 이동처럼 멀리 떨어지면 즉시 스냅
        if (Vector2.Distance(cam2D, target2D) > snapDistance)
        {
            smoothVelocity = Vector3.zero;
            transform.position = new Vector3(
                Mathf.Clamp(target2D.x, minX, maxX),
                Mathf.Clamp(target2D.y, minY, maxY),
                cameraPos.z);
            return;
        }

        float x = Mathf.SmoothDamp(cameraPos.x, target.position.x, ref smoothVelocity.x, smoothDampTime);
        float y = Mathf.SmoothDamp(cameraPos.y, target.position.y, ref smoothVelocity.y, smoothDampTime);

        x = Mathf.Clamp(x, minX, maxX);
        y = Mathf.Clamp(y, minY, maxY);

        transform.position = new Vector3(x, y, cameraPos.z);
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
