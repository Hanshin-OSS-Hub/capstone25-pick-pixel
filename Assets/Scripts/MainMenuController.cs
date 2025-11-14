using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
    [Header("Panels")]
    public GameObject mainMenuPanel;
    public GameObject saveSelectPanel;
    public GameObject settingsPanel;

    // ===== 메인 메뉴 =====
    public void OnStartButton()
    {
        mainMenuPanel.SetActive(false);
        saveSelectPanel.SetActive(true);
    }

    public void OnSettingsButton()
    {
        mainMenuPanel.SetActive(false);
        settingsPanel.SetActive(true);
    }

    public void OnQuitButton()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // ===== 공용 Back =====
    public void OnBackToMain()
    {
        saveSelectPanel.SetActive(false);
        settingsPanel.SetActive(false);
        mainMenuPanel.SetActive(true);
    }

    // ===== 세이브 슬롯 선택 =====
    public void OnSelectSaveSlot(int slotIndex)
    {
        // 테스트: 선택한 슬롯을 저장해두고 Stage1로 이동
        PlayerPrefs.SetInt("SelectedSaveSlot", slotIndex);
        PlayerPrefs.Save();
        SceneManager.LoadScene("Stage1");
    }
}