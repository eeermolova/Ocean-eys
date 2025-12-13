using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneController : MonoBehaviour
{
    // Метод для загрузки следующей сцены
    public void LoadNextScene()
    {
        int currentSceneIndex = SceneManager.GetActiveScene().buildIndex;
        int nextSceneIndex = currentSceneIndex + 1;

        if (nextSceneIndex < SceneManager.sceneCountInBuildSettings)
        {
            SceneManager.LoadScene(nextSceneIndex);
            Debug.Log($"Загружаем сцену {nextSceneIndex}");
        }
        else
        {
            SceneManager.LoadScene(0); // Возврат в меню
            Debug.Log("Возврат в главное меню");
        }
    }

    // Дополнительные методы:
    public void LoadMainMenu() => SceneManager.LoadScene(0);
    public void RestartScene() => SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    public void QuitGame() => Application.Quit();
}