using UnityEngine;

public class MonsterHit : MonoBehaviour
{
    public int hp = 3;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("PlayerAttack")) return;
        hp--;
        Debug.Log($"몬스터 피격! 남은 HP: {hp}");
        if (hp <= 0)
        {
            var ai = GetComponentInParent<MonsterAI>();
            if (ai != null) ai.Die();
            else Destroy(transform.parent.gameObject);
        }
    }
}
