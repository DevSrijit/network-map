using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    public void GoGraphButton()
    {
        // load graph scene
        SceneManager.LoadScene(1);
        // unload main menu scene
        SceneManager.UnloadScene(0);
    }

  public void GoQuitButton()
    {
        print("exiting");
        Application.Quit();
    }
}
