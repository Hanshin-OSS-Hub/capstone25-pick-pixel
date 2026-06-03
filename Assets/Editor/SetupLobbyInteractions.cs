using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class SetupLobbyInteractions
{
    [MenuItem("Tools/Setup Lobby Interactions")]
    public static void Run()
    {
        GameObject statPanel = GameObject.Find("StatUpgradeTerminal");
        SetupStatStation(statPanel);
        SetupDungeonPortal();

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        Debug.Log("[SetupLobbyInteractions] Lobby interaction prompts/highlights configured.");
    }

    static void SetupStatStation(GameObject statPanel)
    {
        GameObject station = GameObject.Find("스탯 강화");
        if (station == null)
        {
            Debug.LogWarning("[SetupLobbyInteractions] 스탯 강화 object not found.");
            return;
        }

        BoxCollider2D collider = station.GetComponent<BoxCollider2D>();
        if (collider == null) collider = station.AddComponent<BoxCollider2D>();
        collider.isTrigger = true;

        GameObject highlight = CreateOrReplaceHighlight(station.transform, "InteractionHighlight", collider, new Vector2(0.18f, 0.18f));
        GameObject prompt = CreateOrReplacePrompt(station.transform, "InteractionPrompt", "E", "STAT");

        StatUpgradeStation component = station.GetComponent<StatUpgradeStation>();
        if (component == null) component = station.AddComponent<StatUpgradeStation>();

        Undo.RecordObject(component, "Setup Stat Upgrade Station");
        component.upgradePanel = statPanel;
        component.promptUI = prompt;
        component.highlightObject = highlight;
        component.interactKey = KeyCode.E;

        highlight.SetActive(false);
        prompt.SetActive(false);
        if (statPanel != null) statPanel.SetActive(false);
        EditorUtility.SetDirty(component);
    }

    static void SetupDungeonPortal()
    {
        GameObject portal = GameObject.Find("던전 입구");
        if (portal == null)
        {
            Debug.LogWarning("[SetupLobbyInteractions] 던전 입구 object not found.");
            return;
        }

        BoxCollider2D collider = portal.GetComponent<BoxCollider2D>();
        if (collider == null) collider = portal.AddComponent<BoxCollider2D>();
        collider.isTrigger = true;

        GameObject highlight = CreateOrReplaceHighlight(portal.transform, "InteractionHighlight", collider, new Vector2(0.28f, 0.28f));
        GameObject prompt = CreateOrReplacePrompt(portal.transform, "InteractionPrompt", "E", "DUNGEON");

        ScenePortal component = portal.GetComponent<ScenePortal>();
        if (component == null) component = portal.AddComponent<ScenePortal>();

        Undo.RecordObject(component, "Setup Scene Portal Interaction");
        component.promptUI = prompt;
        component.highlightObject = highlight;
        component.interactKey = KeyCode.E;

        highlight.SetActive(false);
        prompt.SetActive(false);
        EditorUtility.SetDirty(component);
    }

    static GameObject CreateOrReplaceHighlight(Transform parent, string name, BoxCollider2D collider, Vector2 padding)
    {
        Transform old = parent.Find(name);
        if (old != null) Object.DestroyImmediate(old.gameObject);

        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = collider.offset;

        LineRenderer line = go.AddComponent<LineRenderer>();
        line.useWorldSpace = false;
        line.loop = true;
        line.positionCount = 4;
        line.startWidth = 0.055f;
        line.endWidth = 0.055f;
        line.numCornerVertices = 3;
        line.numCapVertices = 3;
        line.sortingOrder = 100;
        line.material = new Material(Shader.Find("Sprites/Default"));
        line.startColor = Color.white;
        line.endColor = Color.white;

        Vector2 half = collider.size * 0.5f + padding;
        line.SetPosition(0, new Vector3(-half.x, -half.y, -0.01f));
        line.SetPosition(1, new Vector3(-half.x,  half.y, -0.01f));
        line.SetPosition(2, new Vector3( half.x,  half.y, -0.01f));
        line.SetPosition(3, new Vector3( half.x, -half.y, -0.01f));

        return go;
    }

    static GameObject CreateOrReplacePrompt(Transform parent, string name, string keyText, string actionText)
    {
        Transform old = parent.Find(name);
        if (old != null) Object.DestroyImmediate(old.gameObject);

        GameObject root = new GameObject(name, typeof(RectTransform), typeof(Canvas));
        root.transform.SetParent(parent, false);

        Canvas canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 120;

        RectTransform rt = root.GetComponent<RectTransform>();
        rt.localPosition = new Vector3(0f, 1.35f, 0f);
        rt.localScale = Vector3.one * 0.018f;
        rt.sizeDelta = new Vector2(180f, 54f);

        GameObject bg = new GameObject("BG", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        bg.transform.SetParent(root.transform, false);
        RectTransform bgRt = bg.GetComponent<RectTransform>();
        bgRt.anchorMin = Vector2.zero;
        bgRt.anchorMax = Vector2.one;
        bgRt.offsetMin = Vector2.zero;
        bgRt.offsetMax = Vector2.zero;
        bg.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.72f);
        Outline bgOutline = bg.AddComponent<Outline>();
        bgOutline.effectColor = Color.white;
        bgOutline.effectDistance = new Vector2(2f, -2f);

        TextMeshProUGUI text = CreateText(root.transform, "Text", $"[{keyText}] {actionText}", 24);
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white;
        RectTransform textRt = text.rectTransform;
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = Vector2.zero;
        textRt.offsetMax = Vector2.zero;

        return root;
    }

    static TextMeshProUGUI CreateText(Transform parent, string name, string value, int size)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        TextMeshProUGUI text = go.GetComponent<TextMeshProUGUI>();
        text.text = value;
        text.fontSize = size;
        text.raycastTarget = false;
        return text;
    }
}
