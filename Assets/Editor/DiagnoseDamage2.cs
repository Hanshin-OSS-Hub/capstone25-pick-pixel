using UnityEngine;
using UnityEditor;
using System.Reflection;

public static class DiagnoseDamage2
{
    [MenuItem("Tools/Diagnose Damage 2")]
    public static void Run()
    {
        // 1. HealthBar.barFill 확인
        var hbAll = Object.FindObjectsByType<HealthBar>(FindObjectsSortMode.None);
        foreach (var hb in hbAll)
        {
            var bf = typeof(HealthBar).GetField("barFill",
                BindingFlags.Public | BindingFlags.Instance)?.GetValue(hb);
            Debug.Log("[D2][HealthBar] " + hb.gameObject.name
                + "  barFill=" + (bf != null ? bf.ToString() : "NULL")
                + "  currentHp=" + hb.CurrentHp + "/" + hb.MaxHp);
        }

        // 2. MonsterAI 플래그 + player ref 확인
        var aiAll = Object.FindObjectsByType<MonsterAI>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var ai in aiAll)
        {
            var t = typeof(MonsterAI);
            bool isR = (bool)(t.GetField("isRanged",
                BindingFlags.Public | BindingFlags.Instance)?.GetValue(ai) ?? false);
            bool isD = (bool)(t.GetField("isDashMelee",
                BindingFlags.Public | BindingFlags.Instance)?.GetValue(ai) ?? false);
            var pl = t.GetField("player",
                BindingFlags.Public | BindingFlags.Instance)?.GetValue(ai) as Transform;
            Debug.Log("[D2][MonsterAI] " + ai.gameObject.name
                + "  isRanged=" + isR + "  isDashMelee=" + isD
                + "  player=" + (pl != null ? pl.name : "NULL"));
        }

        // 3. GetComponentInParent 직접 테스트
        foreach (var n in new[] { "Monster_Oni", "Monster_Tiger", "Monster_Zombie" })
        {
            var go = GameObject.Find(n);
            if (go == null) continue;
            var hc = go.transform.Find("HitCollider");
            if (hc == null) { Debug.Log("[D2][" + n + "] HitCollider child NOT FOUND"); continue; }
            var fromChild = hc.GetComponentInParent<MonsterHealthBar>();
            var fromRoot  = go.GetComponent<MonsterHealthBar>();
            Debug.Log("[D2][" + n + "] GetComponentInParent(from HitCollider)=" +
                (fromChild != null ? "FOUND" : "NULL")
                + "  GetComponent(from root)=" + (fromRoot != null ? "FOUND" : "NULL"));
        }

        Debug.Log("[D2] --- Done ---");
    }
}
