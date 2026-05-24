using UnityEngine;
using UnityEditor;

public static class FixAttackRange
{
    [MenuItem("Tools/Fix Attack Range")]
    public static void Run()
    {
        // Player 찾기
        var playerGO = GameObject.FindWithTag("Player");
        if (playerGO == null)
        {
            // 태그 없으면 이름으로
            playerGO = GameObject.Find("Player");
        }

        if (playerGO == null)
        {
            Debug.LogError("[FixAttackRange] Player를 찾을 수 없습니다!");
            return;
        }

        var pc = playerGO.GetComponent<PlayerController>();
        if (pc == null)
        {
            // 자식 포함 탐색
            pc = playerGO.GetComponentInChildren<PlayerController>(true);
        }

        if (pc == null)
        {
            Debug.LogError("[FixAttackRange] PlayerController를 찾을 수 없습니다! GO=" + playerGO.name);
            // 어떤 컴포넌트가 있는지 출력
            foreach (var c in playerGO.GetComponents<Component>())
                Debug.Log("  Component: " + c.GetType().Name);
            return;
        }

        // Undo 등록
        Undo.RecordObject(pc, "Fix Attack Range");
        pc.attackHitOffset = 1.4f;
        pc.attackHitWidth  = 2.2f;
        EditorUtility.SetDirty(pc);

        Debug.Log($"[FixAttackRange] {playerGO.name}/{pc.gameObject.name}"
            + $"  attackHitOffset={pc.attackHitOffset}"
            + $"  attackHitWidth={pc.attackHitWidth}");
    }
}
