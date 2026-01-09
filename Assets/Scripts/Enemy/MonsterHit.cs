using UnityEngine;

public class MonsterHit : MonoBehaviour
{
    public int hp = 3;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("PlayerAttack"))
        {
            hp--;
            Debug.Log("몬스터 피격! 남은 HP: " + hp);

            if (hp <= 0)
            {
                Destroy(transform.parent.gameObject);
            }
        }
    }
}
