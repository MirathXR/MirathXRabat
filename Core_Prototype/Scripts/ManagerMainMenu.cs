using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ManagerMainMenu : MonoBehaviour
{
    public string BaseScene;
    //public string MainMenuScene;
    public string BuilderScene;
    public GameObject MainMenu;
    public GameObject PlayMenu;
    public GameObject AboutMenu;
    public GameObject SettingsMenu;
    // Start is called before the first frame update
    public void OpenPlay()
    {
        PlayMenu.SetActive(true);
        AboutMenu.SetActive(false);
        MainMenu.SetActive(false);
        SettingsMenu.SetActive(false);
    }

    public void OpenNew()
    {
        SceneManager.LoadScene("2_BaseScene");
    }
    public void OpenAbout()
    {
        AboutMenu.SetActive(true);
        MainMenu.SetActive(false);
        PlayMenu.SetActive(false);
        SettingsMenu.SetActive(false);
    }

    public void BacktoMenu()
    {
        AboutMenu.SetActive(false);
        MainMenu.SetActive(true);
        PlayMenu.SetActive(false);
        SettingsMenu.SetActive(false);
    }

    public void QuitGame()
    {
        Application.Quit();
        Debug.Log("Quitting");
    }

    public void OpenSettings()
    {
        SettingsMenu.SetActive(true);
        AboutMenu.SetActive(false);
        MainMenu.SetActive(false);
        PlayMenu.SetActive(false);
    }

    public void SetFullscreen (bool isFullscreen)
    {
        Screen.fullScreen = isFullscreen;
    }
    public void Openlink()
    {
        Application.OpenURL("https://whc.unesco.org/en/volunteers2025");
    }

    //ButtonGO in TreasureHuntScene
    public void LoadBuilderScene()
    {
        SceneManager.LoadScene("4_BuilderScene");
    }

}

