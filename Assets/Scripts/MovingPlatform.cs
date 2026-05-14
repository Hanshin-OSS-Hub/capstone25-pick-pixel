using UnityEngine;

public class MovingPlatform : MonoBehaviour
{
    [Header("이동 설정")]
    public Vector2 startPoint;
    public Vector2 endPoint;
    public float speed = 2f;

    private Vector2 target;

    void Start()
    {
        startPoint = transform.position;
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