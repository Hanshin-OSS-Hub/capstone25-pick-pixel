using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 던전 입구 오브젝트에 붙이는 스크립트.
/// 플레이어가 범위 안에서 E키 → Stage1 씬 로드.
/// </summary>
public class DungeonEntrance : Interactable
{
    [Header("씬 전환")]
    [SerializeField] private string targetScene = "Stage1";

    public override void Interact()
    {
        SceneManager.LoadScene(targetScene);
    }
}
