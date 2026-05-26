
using UnityEngine;
using UnityEditor;

public static class DiagnoseScene
{
    [MenuItem("Tools/Diagnose Scene")]
    public static void Run()
    {
        // MapManager
        var mm = Object.FindFirstObjectByType<MapManager>();
        if (mm != null)
        {
            var t = typeof(MapManager);
            var sr = (t.GetField("startRoom",   System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance)?.GetValue(mm) as GameObject)?.name ?? "NULL";
            var er = (t.GetField("exitRoom",    System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance)?.GetValue(mm) as GameObject)?.name ?? "NULL";
            var cr =  t.GetField("combatRooms", System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance)?.GetValue(mm) as GameObject[];
            Debug.Log("[D][MapManager] startRoom=" + sr + "  exitRoom=" + er + "  combatRooms=" + (cr != null ? cr.Length.ToString() : "NULL"));
        }
        else Debug.Log("[D][MapManager] NOT FOUND");

        // MonsterHealthBar duplicate
        foreach (var n in new[]{"Monster_Oni","Monster_Tiger","Monster_Zombie"})
        {
            var go = GameObject.Find(n);
            if (go == null) { Debug.Log("[D]" + n + ": NOT FOUND"); continue; }
            Debug.Log("[D]" + n + ": MonsterHealthBar x" + go.GetComponents<MonsterHealthBar>().Length);
        }

        // MonsterAI timing vs animation
        var ai = Object.FindFirstObjectByType<MonsterAI>();
        if (ai != null) Debug.Log("[D][MonsterAI] attackDuration=" + ai.attackDuration + "s  attackCooldown=" + ai.attackCooldown + "s  attackDamage=" + ai.attackDamage);

        // MovingPlatform
        var mp = Object.FindFirstObjectByType<MovingPlatform>();
        Debug.Log("[D][MovingPlatform] " + (mp != null ? mp.gameObject.name : "NOT FOUND"));

        Debug.Log("[D] --- Done ---");
    }

    static string GetPath(Transform t)
    {
        if (t.parent == null) return t.name;
        return GetPath(t.parent) + "/" + t.name;
    }
}
