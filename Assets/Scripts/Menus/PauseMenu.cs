using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseMenu : MonoBehaviour
{
    public static bool GameIsPaused = false;
    public GameObject pause_menu;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.K)) // This is temporary for testing purposes since it conflicts with Esc when in Play Mode
        //if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (GameIsPaused)
            {
                Resume();
            }
            else
            {
                Pause();
            }
        }
    }

    public void Pause()
    {
        pause_menu.SetActive(true);
        Time.timeScale = 0f;
        GameIsPaused = true;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void Resume()
    {
        pause_menu.SetActive(false);
        Time.timeScale = 1f;
        GameIsPaused = false;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void MainMenuButton()
    {
        MenuController menuController = FindAnyObjectByType<MenuController>();
        if (menuController != null)
        {
            menuController.PlayButtonSound();
        }

        pause_menu.SetActive(false);
        Time.timeScale = 1f;
        GameIsPaused = false;

        SceneManager.LoadScene(0);

        AudioManager.audioManagerInstance.StopMusic();
        AudioManager.audioManagerInstance.PlayMusic(AudioManager.audioManagerInstance.menuBackground);
    }
}