using UnityEngine;
using UnityEngine.Tilemaps;

public class CameraFollow : MonoBehaviour
{
    [Header("Follow Target")]
    public Transform target;

    [Header("Tilemap (World Bounds)")]
    public Tilemap tilemap;

    [Header("Camera Settings")]
    public float smoothDampTime = 0.15f;
    public Vector2 deadZoneSize = new Vector2(4f, 2.5f);  // Deadzone (절대 움직이지 않는 영역)

    [Header("Room Settings (Optional)")]
    public bool useRoomMode = false;
    public Vector2 roomSize = new Vector2(16f, 9f); // 방 크기
    private Vector2 currentRoomCenter;

    private Vector3 smoothVelocity = Vector3.zero;

    private float camWidth;
    private float camHeight;
    private float minX, maxX, minY, maxY;

    void Start()
    {
        // ==== 카메라 크기 계산 ====
        Camera cam = Camera.main;
        camHeight = cam.orthographicSize * 2;
        camWidth = camHeight * cam.aspect;

        // ==== 타일맵 경계 압축 ====
        tilemap.CompressBounds();
        BoundsInt bounds = tilemap.cellBounds;

        Vector3 minWorld = tilemap.CellToWorld(bounds.min);
        Vector3 maxWorld = tilemap.CellToWorld(bounds.max) + tilemap.layoutGrid.cellSize;

        // ==== Clamp 계산 ====
        minX = minWorld.x + camWidth / 2f;
        maxX = maxWorld.x - camWidth / 2f;

        minY = minWorld.y + camHeight / 2f;
        maxY = maxWorld.y - camHeight / 2f;

        if (useRoomMode)
        {
            currentRoomCenter = GetRoomCenter(target.position);
        }
    }

    void LateUpdate()
    {
        if (!target) return;

        Vector3 cameraPos = transform.position;

        // ================ DEATHZONE 계산 ================
        Vector2 deadMin = new Vector2(cameraPos.x - deadZoneSize.x / 2f, cameraPos.y - deadZoneSize.y / 2f);
        Vector2 deadMax = new Vector2(cameraPos.x + deadZoneSize.x / 2f, cameraPos.y + deadZoneSize.y / 2f);

        Vector3 targetPos = cameraPos;

        if (target.position.x < deadMin.x) targetPos.x = target.position.x;
        if (target.position.x > deadMax.x) targetPos.x = target.position.x;
        if (target.position.y < deadMin.y) targetPos.y = target.position.y;
        if (target.position.y > deadMax.y) targetPos.y = target.position.y;

        // ================================================
        // ROOM MODE (던그리드식 방 전환)
        // ================================================
        if (useRoomMode)
        {
            Vector2 roomCenter = GetRoomCenter(target.position);

            if (roomCenter != currentRoomCenter)
                currentRoomCenter = Vector2.Lerp(currentRoomCenter, roomCenter, 0.12f);

            targetPos = new Vector3(currentRoomCenter.x, currentRoomCenter.y, cameraPos.z);
        }

        // ================ 부드러운 따라가기 ================
        float x = Mathf.SmoothDamp(cameraPos.x, targetPos.x, ref smoothVelocity.x, smoothDampTime);
        float y = Mathf.SmoothDamp(cameraPos.y, targetPos.y, ref smoothVelocity.y, smoothDampTime);

        // ================ Clamp (타일 경계) ================
        x = Mathf.Clamp(x, minX, maxX);
        y = Mathf.Clamp(y, minY, maxY);

        transform.position = new Vector3(x, y, cameraPos.z);
    }

    // 방의 중심 좌표 구하기
    Vector2 GetRoomCenter(Vector2 pos)
    {
        float roomX = Mathf.Floor(pos.x / roomSize.x) * roomSize.x + roomSize.x / 2f;
        float roomY = Mathf.Floor(pos.y / roomSize.y) * roomSize.y + roomSize.y / 2f;
        return new Vector2(roomX, roomY);
    }
}
