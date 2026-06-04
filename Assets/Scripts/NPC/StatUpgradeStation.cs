using UnityEngine;

/// <summary>
/// 강화 오브젝트에 붙이는 스크립트.
/// 플레이어가 범위 안에서 E키 → 강화 패널 오픈.
/// </summary>
public class StatUpgradeStation : Interactable
{
    [Header("Terminal UI")]
    public KeyCode interactKey = KeyCode.E;
    public GameObject upgradePanel;
    public GameObject promptUI;
    public GameObject highlightObject;

    protected override void Awake()
    {
        if (interactHint == null) interactHint = promptUI;
        ResolveUpgradePanel();
        base.Awake();

        if (highlightObject != null) highlightObject.SetActive(false);
        if (upgradePanel != null) upgradePanel.SetActive(false);
    }

    public override void Interact()
    {
        ResolveUpgradePanel();
        if (upgradePanel == null)
        {
            Debug.LogWarning("[StatUpgradeStation] StatUpgradeTerminal을 찾을 수 없습니다.", this);
            return;
        }

        StatUpgradeTerminalUI terminal = upgradePanel.GetComponent<StatUpgradeTerminalUI>();
        if (terminal != null) terminal.Toggle();
        else upgradePanel.SetActive(!upgradePanel.activeSelf);
    }

    protected override void OnTriggerEnter2D(Collider2D other)
    {
        base.OnTriggerEnter2D(other);
        if (other.CompareTag("Player") && highlightObject != null)
            highlightObject.SetActive(true);
    }

    protected override void OnTriggerExit2D(Collider2D other)
    {
        base.OnTriggerExit2D(other);
        if (!other.CompareTag("Player")) return;

        if (highlightObject != null) highlightObject.SetActive(false);
        StatUpgradeTerminalUI terminal = upgradePanel != null
            ? upgradePanel.GetComponent<StatUpgradeTerminalUI>()
            : null;
        if (terminal != null) terminal.Close();
        else if (upgradePanel != null) upgradePanel.SetActive(false);
    }

    void ResolveUpgradePanel()
    {
        if (upgradePanel != null) return;

        StatUpgradeTerminalUI[] terminals = Resources.FindObjectsOfTypeAll<StatUpgradeTerminalUI>();
        foreach (StatUpgradeTerminalUI terminal in terminals)
        {
            if (!terminal.gameObject.scene.IsValid()) continue;
            upgradePanel = terminal.gameObject;
            return;
        }
    }
}
