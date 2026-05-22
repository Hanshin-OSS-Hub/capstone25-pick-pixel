using UnityEngine;

/// <summary>
/// 어느 씬에서 시작하든 Resources/PauseMenu 프리팹을 자동 생성해
/// PauseMenuController 를 항상 살아있게 만든다.
/// 사용 방법:
///   1) Panel_Settings 를 가진 Canvas 를 프리팹으로 만들고
///   2) 루트에 PauseMenuController 를 붙인 뒤
///   3) Assets/Resources/PauseMenu.prefab 로 저장하면 끝.
/// 프리팹을 안 쓰고 각 씬에 직접 배치해도 동작한다 (그 경우 이 파일은 무시됨).
/// </summary>
public static class PauseMenuBootstrap
{
    private const string PrefabPath = "PauseMenu";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Init()
    {
        if (PauseMenuController.Instance != null) return;

        var prefab = Resources.Load<GameObject>(PrefabPath);
        if (prefab == null) return;

        Object.Instantiate(prefab);
    }
}
