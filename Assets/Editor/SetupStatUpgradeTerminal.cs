using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class SetupStatUpgradeTerminal
{
    static readonly Color PanelDark = new Color(0.01f, 0.03f, 0.08f, 0.94f);
    static readonly Color Cyan = new Color(0.25f, 1f, 0.88f, 1f);
    static readonly Color CyanDim = new Color(0.12f, 0.55f, 0.58f, 1f);
    static readonly Color Gold = new Color(1f, 0.68f, 0.28f, 1f);
    static readonly Color Frame = new Color(0.08f, 0.20f, 0.26f, 0.98f);

    [MenuItem("Tools/Setup Stat Upgrade Terminal")]
    public static void Run()
    {
        Canvas canvas = Object.FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasGo = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas = canvasGo.GetComponent<Canvas>();

            CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
        }

        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        CanvasScaler existingScaler = canvas.GetComponent<CanvasScaler>();
        if (existingScaler != null)
        {
            existingScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            existingScaler.referenceResolution = new Vector2(1920f, 1080f);
        }

        Transform old = canvas.transform.Find("StatUpgradeTerminal");
        if (old != null)
            Object.DestroyImmediate(old.gameObject);

        Transform legacyPanel = canvas.transform.Find("스탯 패널");
        if (legacyPanel != null)
            legacyPanel.gameObject.SetActive(false);

        Transform legacyDungeonPanel = canvas.transform.Find("던전 패널");
        if (legacyDungeonPanel != null)
            legacyDungeonPanel.gameObject.SetActive(false);

        GameObject root = CreateUI("StatUpgradeTerminal", canvas.transform);
        SetLayerRecursive(root, LayerMask.NameToLayer("UI"));
        RectTransform rootRt = root.GetComponent<RectTransform>();
        rootRt.anchorMin = new Vector2(0.5f, 0.5f);
        rootRt.anchorMax = new Vector2(0.5f, 0.5f);
        rootRt.pivot = new Vector2(0.5f, 0.5f);
        rootRt.anchoredPosition = Vector2.zero;
        rootRt.sizeDelta = new Vector2(980f, 720f);

        Image rootImage = root.AddComponent<Image>();
        rootImage.color = Frame;

        StatUpgradeTerminalUI terminal = root.AddComponent<StatUpgradeTerminalUI>();
        terminal.points = 1420;
        terminal.rows = new StatUpgradeTerminalUI.StatRow[4];

        GameObject inner = CreatePanel("InnerPanel", root.transform, PanelDark);
        Stretch(inner.GetComponent<RectTransform>(), new Vector2(42f, 42f), new Vector2(-42f, -42f));

        AddBorder(root.transform, new Vector2(26f, -26f), new Vector2(928f, 638f), Cyan, 4f);
        AddBorder(root.transform, new Vector2(40f, -40f), new Vector2(900f, 610f), CyanDim, 2f);

        TextMeshProUGUI title = CreateText("Title", inner.transform, "TERMINAL > [STAT UPGRADES]", 38, Cyan);
        Place(title.rectTransform, new Vector2(70f, -55f), new Vector2(760f, 54f), new Vector2(0f, 1f));
        title.alignment = TextAlignmentOptions.Left;

        AddLine(inner.transform, "TitleLine", new Vector2(70f, -105f), new Vector2(760f, 3f), CyanDim);

        TextMeshProUGUI points = CreateText("PointText", inner.transform, "1,420 PTS", 38, Gold);
        Place(points.rectTransform, new Vector2(0f, -118f), new Vector2(360f, 54f), new Vector2(0.5f, 1f));
        points.alignment = TextAlignmentOptions.Center;
        terminal.pointsText = points;

        GameObject rowFrame = CreatePanel("RowsFrame", inner.transform, new Color(0f, 0f, 0f, 0f));
        Place(rowFrame.GetComponent<RectTransform>(), new Vector2(0f, -178f), new Vector2(760f, 360f), new Vector2(0.5f, 1f));
        AddOutline(rowFrame, CyanDim, 3f);

        string[] labels = { "ATK SPD", "MOV SPD", "HP", "PT GAIN" };
        string[] icons = { "ATK", "MOV", "HP", "PT" };
        int[] levels = { 6, 4, 8, 3 };

        for (int i = 0; i < labels.Length; i++)
        {
            terminal.rows[i] = CreateRow(rowFrame.transform, i, labels[i], icons[i], levels[i]);
        }

        GameObject infoBox = CreatePanel("PointBonusInfo", inner.transform, new Color(0.01f, 0.03f, 0.07f, 0.96f));
        Place(infoBox.GetComponent<RectTransform>(), new Vector2(0f, -552f), new Vector2(760f, 72f), new Vector2(0.5f, 1f));
        AddOutline(infoBox, CyanDim, 2f);

        TextMeshProUGUI infoTitle = CreateText("InfoTitle", infoBox.transform, "POINT BONUS INFO", 25, Cyan);
        Place(infoTitle.rectTransform, new Vector2(16f, -9f), new Vector2(340f, 30f), new Vector2(0f, 1f));
        infoTitle.alignment = TextAlignmentOptions.Left;

        TextMeshProUGUI info = CreateText("Info", infoBox.transform, ">> BONUS INFO: +5% PTS / KILL || +10% HP / LVL <<", 23, Gold);
        Place(info.rectTransform, new Vector2(18f, -40f), new Vector2(720f, 28f), new Vector2(0f, 1f));
        info.alignment = TextAlignmentOptions.Left;

        SetLayerRecursive(root, LayerMask.NameToLayer("UI"));
        root.SetActive(false);
        EditorUtility.SetDirty(root);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(root.scene);
        Debug.Log("[SetupStatUpgradeTerminal] StatUpgradeTerminal UI 생성 완료");
    }

    static void SetLayerRecursive(GameObject go, int layer)
    {
        if (layer < 0) return;
        go.layer = layer;
        foreach (Transform child in go.transform)
            SetLayerRecursive(child.gameObject, layer);
    }

    static StatUpgradeTerminalUI.StatRow CreateRow(Transform parent, int index, string label, string icon, int level)
    {
        GameObject row = CreateUI("Row_" + label.Replace(" ", "_"), parent);
        RectTransform rt = row.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 1f);
        rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0f, -16f - index * 82f);
        rt.sizeDelta = new Vector2(710f, 74f);

        GameObject iconBox = CreatePanel("Icon", row.transform, new Color(0.03f, 0.08f, 0.12f, 1f));
        Place(iconBox.GetComponent<RectTransform>(), new Vector2(0f, 0f), new Vector2(72f, 72f), new Vector2(0f, 0.5f));
        AddOutline(iconBox, Cyan, 3f);

        TextMeshProUGUI iconText = CreateText("IconText", iconBox.transform, icon, 22, icon == "HP" ? new Color(1f, 0.32f, 0.38f) : Gold);
        Stretch(iconText.rectTransform, Vector2.zero, Vector2.zero);
        iconText.alignment = TextAlignmentOptions.Center;

        TextMeshProUGUI labelText = CreateText("Label", row.transform, label, 32, Cyan);
        Place(labelText.rectTransform, new Vector2(96f, -2f), new Vector2(260f, 40f), new Vector2(0f, 1f));
        labelText.alignment = TextAlignmentOptions.Left;

        TextMeshProUGUI levelText = CreateText("Level", row.transform, "LV " + level, 24, Cyan);
        Place(levelText.rectTransform, new Vector2(420f, -4f), new Vector2(100f, 34f), new Vector2(0f, 1f));
        levelText.alignment = TextAlignmentOptions.Right;

        Image[] segments = CreateSegments(row.transform, level);

        Button plus = CreateButton("Plus", row.transform, "[+]", new Vector2(570f, -4f));
        Button minus = CreateButton("Minus", row.transform, "[-]", new Vector2(640f, -4f));

        return new StatUpgradeTerminalUI.StatRow
        {
            statName = label,
            level = level,
            maxLevel = 12,
            cost = 100,
            levelText = levelText,
            segments = segments,
            plusButton = plus,
            minusButton = minus
        };
    }

    static Image[] CreateSegments(Transform parent, int filled)
    {
        Image[] segments = new Image[12];
        for (int i = 0; i < segments.Length; i++)
        {
            GameObject seg = CreatePanel("Segment_" + i, parent, i < filled ? new Color(0.18f, 1f, 0.84f) : new Color(0.04f, 0.12f, 0.16f));
            RectTransform rt = seg.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(98f + i * 31f, -46f);
            rt.sizeDelta = new Vector2(26f, 30f);
            AddOutline(seg, CyanDim, 2f);
            segments[i] = seg.GetComponent<Image>();
        }
        return segments;
    }

    static Button CreateButton(string name, Transform parent, string text, Vector2 pos)
    {
        GameObject go = CreatePanel(name, parent, new Color(0.02f, 0.06f, 0.09f, 1f));
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(58f, 54f);
        AddOutline(go, Cyan, 3f);

        Button button = go.AddComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(0.7f, 1f, 0.95f);
        colors.pressedColor = new Color(0.35f, 0.9f, 0.85f);
        colors.disabledColor = new Color(0.3f, 0.35f, 0.36f);
        button.colors = colors;

        TextMeshProUGUI label = CreateText("Text", go.transform, text, 24, Cyan);
        Stretch(label.rectTransform, Vector2.zero, Vector2.zero);
        label.alignment = TextAlignmentOptions.Center;
        return button;
    }

    static GameObject CreateUI(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go;
    }

    static GameObject CreatePanel(string name, Transform parent, Color color)
    {
        GameObject go = CreateUI(name, parent);
        Image image = go.AddComponent<Image>();
        image.color = color;
        return go;
    }

    static TextMeshProUGUI CreateText(string name, Transform parent, string value, int size, Color color)
    {
        GameObject go = CreateUI(name, parent);
        TextMeshProUGUI text = go.AddComponent<TextMeshProUGUI>();
        text.text = value;
        text.fontSize = size;
        text.color = color;
        text.enableAutoSizing = false;
        text.raycastTarget = false;
        return text;
    }

    static void AddBorder(Transform parent, Vector2 pos, Vector2 size, Color color, float thickness)
    {
        GameObject border = CreatePanel("NeonBorder", parent, new Color(0f, 0f, 0f, 0f));
        RectTransform rt = border.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        AddOutline(border, color, thickness);
    }

    static void AddOutline(GameObject go, Color color, float thickness)
    {
        Outline outline = go.AddComponent<Outline>();
        outline.effectColor = color;
        outline.effectDistance = new Vector2(thickness, -thickness);
    }

    static void AddLine(Transform parent, string name, Vector2 pos, Vector2 size, Color color)
    {
        GameObject line = CreatePanel(name, parent, color);
        Place(line.GetComponent<RectTransform>(), pos, size, new Vector2(0f, 1f));
    }

    static void Place(RectTransform rt, Vector2 pos, Vector2 size, Vector2 anchor)
    {
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.pivot = anchor;
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
    }

    static void Stretch(RectTransform rt, Vector2 offsetMin, Vector2 offsetMax)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = offsetMin;
        rt.offsetMax = offsetMax;
    }
}
