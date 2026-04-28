using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    [Header("Panels")]
    public GameObject tutorialPanel;
    public GameObject creditsPanel;

    public void StartGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainScene");
    }

    public void QuitGame()
    {
        Debug.Log("Game Quitting...");
        Application.Quit();
    }

    public void OpenTutorial()
    {
        if (tutorialPanel != null) tutorialPanel.SetActive(true);
        if (creditsPanel != null) creditsPanel.SetActive(false);
    }

    public void OpenCredits()
    {
        if (creditsPanel != null) creditsPanel.SetActive(true);
        if (tutorialPanel != null) tutorialPanel.SetActive(false);
    }

    public void ClosePanels()
    {
        if (tutorialPanel != null) tutorialPanel.SetActive(false);
        if (creditsPanel != null) creditsPanel.SetActive(false);
    }
}
