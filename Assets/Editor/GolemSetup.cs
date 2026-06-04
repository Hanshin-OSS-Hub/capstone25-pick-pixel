#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Stage1의 Stage1_Boss 룸에 Monster_Golem 프리팹을 보스로 배치한다.
/// 메뉴: Tools/GolemSetup/Place Golem in Stage1
/// </summary>
public static class GolemSetup
{
    const string GolemPrefabPath = "Assets/Prefabs/Monster_Golem.prefab";
    const string Stage1ScenePath = "Assets/Scenes/Stage1.unity";

    [MenuItem("Tools/GolemSetup/Place Golem in Stage1")]
    public static void PlaceGolem()
    {
        // 기존 Boss 오브젝트 있으면 제거
        var existing = GameObject.Find("Boss");
        if (existing != null)
        {
            Undo.DestroyObjectImmediate(existing);
            Debug.Log("[GolemSetup] 기존 Boss 오브젝트 제거");
        }

        // 기존 Monster_Golem(보스) 있으면 제거 후 재배치
        var existingGolem = GameObject.Find("Monster_Golem_Boss");
        if (existingGolem != null)
        {
            Undo.DestroyObjectImmediate(existingGolem);
            Debug.Log("[GolemSetup] 기존 Monster_Golem_Boss 오브젝트 제거");
        }

        // Stage1_Boss 룸 찾기
        var bossRoom = GameObject.Find("Stage1_Boss");
        if (bossRoom == null)
        {
            Debug.LogError("[GolemSetup] 'Stage1_Boss' 룸을 찾을 수 없습니다!");
            return;
        }

        // 골렘 프리팹 로드
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(GolemPrefabPath);
        if (prefab == null)
        {
            Debug.LogError($"[GolemSetup] 골렘 프리팹 없음: {GolemPrefabPath}");
            return;
        }

        // 보스룸 중앙 위치 가져오기
        Vector3 spawnPos = bossRoom.transform.position + new Vector3(0f, 2f, 0f);

        // 인스턴스 생성
        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        go.name = "Monster_Golem_Boss";
        go.transform.position = spawnPos;
        go.transform.SetParent(bossRoom.transform, worldPositionStays: true);

        // 보스 스케일 키우기 (일반 골렘보다 크게)
        go.transform.localScale = new Vector3(2.5f, 2.5f, 1f);

        // MonsterAI 설정: 보스답게 강화
        var ai = go.GetComponent<MonsterAI>();
        if (ai != null)
        {
            ai.detectRange    = 14f;
            ai.attackRange    = 7f;   // 원거리 공격
            ai.attackCooldown = 2.5f;
            ai.attackDamage   = 20;
            ai.moveSpeed      = 1.8f;
            ai.maxChaseDistance = 50f;
        }

        // MonsterHit HP 높이기
        var hit = go.GetComponentInChildren<MonsterHit>(true);
        if (hit != null) hit.hp = 30;

        Undo.RegisterCreatedObjectUndo(go, "Place Golem Boss");
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());

        Debug.Log($"[GolemSetup] 골렘 보스 배치 완료! 위치: {spawnPos}, 부모: {bossRoom.name}");
        Selection.activeGameObject = go;
    }
}
#endif
