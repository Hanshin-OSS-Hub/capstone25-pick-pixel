using UnityEngine;

/// <summary>
/// NPC 대화 상호작용. NPC GameObject에 붙인다.
/// CircleCollider2D (IsTrigger) + 이 스크립트만 추가하면 됨.
/// </summary>
public class NPCInteraction : Interactable
{
    [Header("NPC Info")]
    public string npcName = "NPC";
    [TextArea(3, 8)]
    public string dialogueText = "안녕하세요!";
    public Sprite portrait;

    public override void Interact()
    {
        if (interactHint != null) interactHint.SetActive(false);
        NPCPanelUI.Instance?.Open(this);
    }
}
