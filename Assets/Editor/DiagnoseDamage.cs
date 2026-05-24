using UnityEngine;
using UnityEditor;
using System.Reflection;

public static class DiagnoseDamage
{
    [MenuItem("Tools/Diagnose Damage")]
    public static void Run()
    {
        // 1. PlayerController healthBar
        var pc = Object.FindFirstObjectByType<PlayerController>();
        if (pc != null)
        {
            var hb = typeof(PlayerController).GetField("healthBar",
                BindingFlags.Public | BindingFlags.Instance)?.GetValue(pc) as HealthBar;
            Debug.Log("[D][PC] healthBar=" + (hb != null ? hb.gameObject.name : "NULL"));
        }
        else Debug.Log("[D][PC] NOT FOUND");

        // 2. 각 몬스터 진단
        foreach (var n in new[] { "Monster_Oni", "Monster_Tiger", "Monster_Zombie" })
        {
            var go = GameObject.Find(n);
            if (go == null) { Debug.Log("[D][" + n + "] NOT FOUND"); continue; }

            // 계층 구조
            string parent = go.transform.parent != null ? go.transform.parent.name : "ROOT";
            Debug.Log("[D][" + n + "] parent=" + parent);

            // MonsterHit
            var mh = go.GetComponentInChildren<MonsterHit>(true);
            if (mh == null) { Debug.Log("[D][" + n + "] MonsterHit NOT FOUND"); continue; }
            Debug.Log("[D][" + n + "] MonsterHit on=" + mh.gameObject.name + "  root=" + mh.transform.root.name);

            // MonsterHit.healthBar
            var hbField = typeof(MonsterHit).GetField("healthBar",
                BindingFlags.NonPublic | BindingFlags.Instance);
            var hbVal = hbField?.GetValue(mh) as MonsterHealthBar;
            Debug.Log("[D][" + n + "] MonsterHit.healthBar=" + (hbVal != null ? hbVal.gameObject.name : "NULL"));

            if (hbVal != null)
            {
                // fillImage
                var fi = typeof(MonsterHealthBar).GetField("fillImage",
                    BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(hbVal);
                Debug.Log("[D][" + n + "] fillImage=" + (fi != null ? fi.ToString() : "NULL"));
            }

            // hp
            Debug.Log("[D][" + n + "] hp=" + mh.hp);

            // MonsterHealthBar on root
            var mhb = go.GetComponent<MonsterHealthBar>();
            Debug.Log("[D][" + n + "] MonsterHealthBar on root=" + (mhb != null ? "YES" : "NO"));
        }

        // 3. 콜라이더 레이어 확인
        foreach (var n in new[] { "Monster_Oni", "Monster_Tiger", "Monster_Zombie" })
        {
            var go = GameObject.Find(n);
            if (go == null) continue;
            foreach (var col in go.GetComponentsInChildren<Collider2D>(true))
                Debug.Log("[D][" + n + "] Collider=" + col.gameObject.name
                    + " layer=" + LayerMask.LayerToName(col.gameObject.layer)
                    + " isTrigger=" + col.isTrigger
                    + " enabled=" + col.enabled
                    + " GOactive=" + col.gameObject.activeSelf);
        }

        Debug.Log("[D] --- Done ---");
    }
}
