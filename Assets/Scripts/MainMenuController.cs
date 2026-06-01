using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
    [Header("Panels")]
    public GameObject mainMenuPanel;
    public GameObject saveSelectPanel;
    public GameObject settingsPanel;

    // ===== ���� �޴� =====
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

    // ===== ���� Back =====
    public void OnBackToMain()
    {
        saveSelectPanel.SetActive(false);
        settingsPanel.SetActive(false);
        mainMenuPanel.SetActive(true);
    }

    // ===== ���̺� ���� ���� =====
    public void OnSelectSaveSlot(int slotIndex)
    {
        // 선택한 세이브 슬롯을 저장하고 로비(Lobby_V2)로 이동
        PlayerPrefs.SetInt("SelectedSaveSlot", slotIndex);
        PlayerPrefs.Save();
        SceneManager.LoadScene("Lobby_V2");
    }
}