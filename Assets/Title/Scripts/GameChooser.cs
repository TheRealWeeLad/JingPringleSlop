using UnityEngine;
using UnityEngine.SceneManagement;

public class GameChooser : MonoBehaviour
{
    public void ChooseGame(string game)
    {
        GameManager.SetGame(game);
        SceneManager.LoadScene(game, LoadSceneMode.Single);
    }
}