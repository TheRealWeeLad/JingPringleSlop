using UnityEngine.SceneManagement;

public static class GameManager
{
    static string _currentGame = "Title";
    public static string CurrentGame { get => _currentGame; set => _currentGame = value; }

    public static void SetGame(string game) { _currentGame = game; }

    public static void QuitGame()
    {
        SetGame("Title");
        SceneManager.LoadScene(0);
    }
}
