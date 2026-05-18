using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Button_Back 같은 닫기 버튼에 붙이면 자동으로 onClick 에
/// PauseMenuController.Close 를 연결한다.
/// 프리팹 와이어링용 — UnityEvent 를 외부 도구로 설정하기 어려워 만든 작은 헬퍼.
/// </summary>
[RequireComponent(typeof(Button))]
public class PauseMenuCloseButton : MonoBehaviour
{
    void Awake()
    {
        GetComponent<Button>().onClick.AddListener(OnClick);
    }

    void OnClick()
    {
        if (PauseMenuController.Instance != null)
            PauseMenuController.Instance.Close();
    }
}
