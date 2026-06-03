using UnityEngine;

/// <summary>
/// NPC 대화 패널 UI.
/// TODO: 문영진 — NPC 대화창 UI 구현 예정.
/// 현재는 컴파일 오류 방지용 stub.
/// </summary>
public class NPCPanelUI : MonoBehaviour
{
    public static NPCPanelUI Instance { get; private set; }

    public bool IsOpen { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    public void Open(NPCInteraction npc)
    {
        // TODO: 대화창 표시
        IsOpen = true;
        Debug.Log($"[NPCPanelUI] {npc.npcName}: {npc.dialogueText}");
    }

    public void Close()
    {
        IsOpen = false;
    }
}
