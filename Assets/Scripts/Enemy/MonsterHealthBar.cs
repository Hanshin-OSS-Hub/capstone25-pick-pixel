using UnityEngine;
using UnityEngine.UI;

public class MonsterHealthBar : MonoBehaviour
{
    [Header("위치")]
    public float yOffset = 1.7f;

    private Image fillImage;

    void Awake()
    {
        BuildBar();
    }

    void BuildBar()
    {
        // World Space Canvas 생성
        var cvGO = new GameObject("MonsterHP_Canvas");
        cvGO.transform.SetParent(transform, false);

        // Canvas 먼저 추가해야 RectTransform이 생성됨
        // (Canvas 추가 전 Transform.localPosition 설정은 RectTransform 생성 시 초기화됨)
        var canvas = cvGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 20;

        var cvRect = cvGO.GetComponent<RectTransform>();
        cvRect.localScale    = Vector3.one * 0.016f;
        cvRect.sizeDelta     = new Vector2(120f, 16f);
        cvRect.localPosition = new Vector3(0f, yOffset, 0f);

        // 배경 (검은색)
        var bgGO = new GameObject("BG", typeof(RectTransform));
        bgGO.transform.SetParent(cvGO.transform, false);
        bgGO.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.75f);
        Stretch(bgGO.GetComponent<RectTransform>(), Vector2.zero, Vector2.zero);

        // 채우기 바 (빨간색)
        var fillGO = new GameObject("Fill", typeof(RectTransform));
        fillGO.transform.SetParent(cvGO.transform, false);
        fillImage            = fillGO.AddComponent<Image>();
        fillImage.color      = new Color(0.85f, 0.15f, 0.15f);
        fillImage.type       = Image.Type.Filled;
        fillImage.fillMethod = Image.FillMethod.Horizontal;
        fillImage.fillAmount = 1f;
        Stretch(fillGO.GetComponent<RectTransform>(), new Vector2(1f, 1f), new Vector2(-1f, -1f));
    }

    static void Stretch(RectTransform rt, Vector2 offsetMin, Vector2 offsetMax)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = offsetMin;
        rt.offsetMax = offsetMax;
    }

    /// <summary>0~1 비율로 바 업데이트</summary>
    public void SetFill(float ratio)
    {
        if (fillImage != null)
            fillImage.fillAmount = Mathf.Clamp01(ratio);
    }
}
