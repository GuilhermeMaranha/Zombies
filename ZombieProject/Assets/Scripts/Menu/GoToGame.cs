using UnityEngine;
using UnityEngine.SceneManagement;

public class GoToGame : MonoBehaviour
{
    #region Methods
    public void Load()
    {
        SceneManager.LoadScene("game");
    }

    public void Exit()
    {
        Application.Quit();
    }

    #endregion
}
