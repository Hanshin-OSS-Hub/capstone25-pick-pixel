using UnityEngine;

public class MovingPlatform : MonoBehaviour
{
    [Header("이동 설정")]
    public Vector2 moveOffset = new Vector2(0f, 3f); // 얼마나 이동할지 (상대 거리)
    public float speed = 2f;

    private Vector2 startPoint;
    private Vector2 endPoint;
    private Vector2 target;

    void Start()
    {
        startPoint = transform.position;
        endPoint = startPoint + moveOffset;
        target = endPoint;
    }

    void Update()
    {
        transform.position = Vector2.MoveTowards(
            transform.position, target, speed * Time.deltaTime);

        if (Vector2.Distance(transform.position, target) < 0.01f)
            target = (target == endPoint) ? startPoint : endPoint;
    }
}