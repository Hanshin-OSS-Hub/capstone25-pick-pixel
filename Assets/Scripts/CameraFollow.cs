using UnityEngine;
using UnityEngine.Tilemaps;

public class CameraFollow : MonoBehaviour
{
    [Header("Follow Target")]
    public Transform target;

    [Header("Camera Settings")]
    public float smoothDampTime = 0.15f;
    public Vector2 deadZoneSize = new Vector2(4f, 2.5f);

    [Header("Debug")]
    public bool showGizmos = true;

    private Vector3 smoothVelocity = Vector3.zero;
    private float camWidth, camHeight;
    private float minX, maxX, minY, maxY;
    private bool hasBounds = false;

    void Start()
    {
        Camera cam = Camera.main;
        camHeight = cam.orthographicSize * 2f;
        camWidth  = camHeight * cam.aspect;
        SetNoBounds();
    }

    public void SetRoomBounds(Tilemap tilemap)
    {
        if (tilemap == null) { SetNoBounds(); return; }

        tilemap.CompressBounds();
        BoundsInt bounds   = tilemap.cellBounds;
        Vector3   minWorld = tilemap.CellToWorld(bounds.min);
        Vector3   maxWorld = tilemap.CellToWorld(bounds.max) + tilemap.layoutGrid.cellSize;

        minX = minWorld.x + camWidth  / 2f;
        maxX = maxWorld.x - camWidth  / 2f;
        minY = minWorld.y + camHeight / 2f;
        maxY = maxWorld.y - camHeight / 2f;

        if (minX > maxX) { float mid = (minWorld.x + maxWorld.x) / 2f; minX = mid; maxX = mid; }
        if (minY > maxY) { float mid = (minWorld.y + maxWorld.y) / 2f; minY = mid; maxY = mid; }

        hasBounds = true;
        Debug.Log($"[CameraFollow] Bounds 갱신 → X({minX:F1}~{maxX:F1}) Y({minY:F1}~{maxY:F1})");
    }

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
        Vector3 targetPos = cameraPos;
        float halfDX = deadZoneSize.x / 2f;
        float halfDY = deadZoneSize.y / 2f;

        if      (target.position.x < cameraPos.x - halfDX) targetPos.x = target.position.x + halfDX;
        else if (target.position.x > cameraPos.x + halfDX) targetPos.x = target.position.x - halfDX;
        if      (target.position.y < cameraPos.y - halfDY) targetPos.y = target.position.y + halfDY;
        else if (target.position.y > cameraPos.y + halfDY) targetPos.y = target.position.y - halfDY;

        float x = Mathf.SmoothDamp(cameraPos.x, targetPos.x, ref smoothVelocity.x, smoothDampTime);
        float y = Mathf.SmoothDamp(cameraPos.y, targetPos.y, ref smoothVelocity.y, smoothDampTime);

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
        Gizmos.color = new Color(1f, 1f, 0f, 0.3f);
        Gizmos.DrawWireCube(transform.position,
            new Vector3(deadZoneSize.x, deadZoneSize.y, 0f));
    }
}
