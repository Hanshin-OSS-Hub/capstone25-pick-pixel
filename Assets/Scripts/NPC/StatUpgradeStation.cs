using UnityEngine;

/// <summary>
/// 강화 오브젝트에 붙이는 스크립트.
/// 플레이어가 범위 안에서 E키 → 강화 패널 오픈.
/// </summary>
public class StatUpgradeStation : Interactable
{
    public override void Interact()
    {
        StatUpgradePanelUI.Instance?.Open();
    }
}
