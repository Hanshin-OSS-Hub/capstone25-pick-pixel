using UnityEngine;

/// <summary>
/// SpriteRenderer 기반 몬스터 체력바 (URP 2D WorldSpace Canvas 렌더링 문제 우회)
/// </summary>
public class MonsterHealthBar : MonoBehaviour
{
    [Header("위치/크기")]
    public float yOffset     = 1.7f;
    public float barWidth    = 1.0f;
    public float barHeight   = 0.12f;
    public int   sortingOrder = 200;

    private Transform      fillTransform;
    private SpriteRenderer fillRenderer;

    void Awake() => BuildBar();

    void BuildBar()
    {
        Sprite centerSpr = MakeSprite(new Vector2(0.5f, 0.5f));
        Sprite leftSpr   = MakeSprite(new Vector2(0f,   0.5f));

        // ── 테두리 (검은 테두리 효과) ──────────────────────────────────────
        var border = MakeSR("HP_Border", centerSpr, sortingOrder,
                            new Color(0f, 0f, 0f, 0.9f));
        border.transform.localPosition = new Vector3(0f, yOffset, 0f);
        border.transform.localScale    = new Vector3(barWidth + 0.06f, barHeight + 0.06f, 1f);

        // ── 배경 (어두운 회색) ──────────────────────────────────────────────
        var bg = MakeSR("HP_BG", centerSpr, sortingOrder + 1,
                        new Color(0.15f, 0.15f, 0.15f, 0.85f));
        bg.transform.localPosition = new Vector3(0f, yOffset, 0f);
        bg.transform.localScale    = new Vector3(barWidth, barHeight, 1f);

        // ── 채움 바 (왼쪽 피벗 → 오른쪽으로 줄어듦) ────────────────────────
        var fill = MakeSR("HP_Fill", leftSpr, sortingOrder + 2,
                          new Color(0.2f, 0.85f, 0.2f, 1f));
        fill.transform.localPosition = new Vector3(-barWidth * 0.5f, yOffset, 0f);
        fill.transform.localScale    = new Vector3(barWidth, barHeight, 1f);

        fillTransform = fill.transform;
        fillRenderer  = fill.GetComponent<SpriteRenderer>();
    }

    /// <summary>0~1 비율로 체력바 업데이트</summary>
    public void SetFill(float ratio)
    {
        if (fillTransform == null) return;

        float r = Mathf.Clamp01(ratio);

        // 왼쪽 피벗이라 localPosition.x는 고정, localScale.x만 줄이면 됨
        Vector3 s = fillTransform.localScale;
        s.x = barWidth * r;
        fillTransform.localScale = s;

        // 색상: 초록 → 주황 → 빨강
        if (fillRenderer != null)
            fillRenderer.color = r > 0.5f ? new Color(0.2f, 0.85f, 0.2f, 1f)
                               : r > 0.25f ? new Color(1f,   0.65f, 0f,   1f)
                               :             new Color(0.9f, 0.15f, 0.15f, 1f);
    }

    // ── 헬퍼 ───────────────────────────────────────────────────────────────

    SpriteRenderer MakeSR(string goName, Sprite spr, int order, Color col)
    {
        var go = new GameObject(goName);
        go.transform.SetParent(transform, false);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite           = spr;
        sr.sortingLayerName = "Default";
        sr.sortingOrder     = order;
        sr.color            = col;
        return sr;
    }

    static Sprite MakeSprite(Vector2 pivot)
    {
        var tex = new Texture2D(4, 4, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point,
            wrapMode   = TextureWrapMode.Clamp
        };
        Color[] pixels = new Color[16];
        for (int i = 0; i < 16; i++) pixels[i] = Color.white;
        tex.SetPixels(pixels);
        tex.Apply();
        // PPU=4 → 1 world-unit = 4 pixels → sprite width/height = 1 world-unit
        return Sprite.Create(tex, new Rect(0, 0, 4, 4), pivot, 4f);
    }
}
