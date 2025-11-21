using UnityEngine;

public class GroundCheck : MonoBehaviour
{
    public LayerMask groundLayer;
    public float checkRadius = 0.1f;
    public bool isGrounded;

    private void Update()
    {
        isGrounded = Physics2D.OverlapCircle(transform.position, checkRadius, groundLayer);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, checkRadius);
    }
}
