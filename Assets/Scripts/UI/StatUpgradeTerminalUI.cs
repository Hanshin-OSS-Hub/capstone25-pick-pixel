using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StatUpgradeTerminalUI : MonoBehaviour
{
    [Serializable]
    public class StatRow
    {
        public string statName;
        public int level;
        public int maxLevel = 12;
        public int cost = 100;
        public TextMeshProUGUI levelText;
        public Image[] segments;
        public Button plusButton;
        public Button minusButton;
    }

    [Header("Points")]
    public int points = 1420;
    public TextMeshProUGUI pointsText;

    [Header("Rows")]
    public StatRow[] rows;

    void Awake()
    {
        WireButtons();
        Refresh();
    }

    void OnValidate()
    {
        if (Application.isPlaying) return;
        Refresh();
    }

    void WireButtons()
    {
        if (rows == null) return;

        for (int i = 0; i < rows.Length; i++)
        {
            int index = i;
            if (rows[i].plusButton != null)
            {
                rows[i].plusButton.onClick.RemoveAllListeners();
                rows[i].plusButton.onClick.AddListener(() => Upgrade(index));
            }

            if (rows[i].minusButton != null)
            {
                rows[i].minusButton.onClick.RemoveAllListeners();
                rows[i].minusButton.onClick.AddListener(() => Downgrade(index));
            }
        }
    }

    public void Upgrade(int index)
    {
        if (!IsValidIndex(index)) return;
        StatRow row = rows[index];
        if (row.level >= row.maxLevel || points < row.cost) return;

        row.level++;
        points -= row.cost;
        Refresh();
    }

    public void Downgrade(int index)
    {
        if (!IsValidIndex(index)) return;
        StatRow row = rows[index];
        if (row.level <= 0) return;

        row.level--;
        points += row.cost;
        Refresh();
    }

    void Refresh()
    {
        if (pointsText != null)
            pointsText.text = points.ToString("N0") + " PTS";

        if (rows == null) return;

        foreach (StatRow row in rows)
        {
            if (row == null) continue;

            if (row.levelText != null)
                row.levelText.text = "LV " + row.level;

            if (row.segments != null)
            {
                for (int i = 0; i < row.segments.Length; i++)
                {
                    if (row.segments[i] == null) continue;
                    bool filled = i < row.level;
                    row.segments[i].color = filled
                        ? new Color(0.18f, 1f, 0.84f, 1f)
                        : new Color(0.04f, 0.12f, 0.16f, 1f);
                }
            }

            if (row.plusButton != null)
                row.plusButton.interactable = row.level < row.maxLevel && points >= row.cost;

            if (row.minusButton != null)
                row.minusButton.interactable = row.level > 0;
        }
    }

    bool IsValidIndex(int index)
    {
        return rows != null && index >= 0 && index < rows.Length && rows[index] != null;
    }
}
