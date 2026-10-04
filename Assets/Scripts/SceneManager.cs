using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneManager : MonoBehaviour
{
    public void StartGame()
    {
        Sfx.Play(Sfx.Sounds.UIClick);
        UnityEngine.SceneManagement.SceneManager.LoadScene(1);
    }

    public void QuitGame()
    {
        Application.Quit();
    }

    public void LoadMainMenu()
    {
        Sfx.Play(Sfx.Sounds.UIClick);
        UnityEngine.SceneManagement.SceneManager.LoadScene(0);
    }
}
