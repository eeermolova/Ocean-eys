using UnityEngine;
using UnityEngine.SceneManagement;

public class GameUIManager : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject pauseButton;
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private GameObject deathPanel;

    [Header("Scenes")]
    [SerializeField] private string mainMenuSceneName = "Menu"; // впиши имя сцены меню как в Build Settings

    [Header("Что отключать при паузе/смерти (перетащи сюда PlayerController, SimpleCombat и т.д.)")]
    [SerializeField] private PlayerController playerController;
    [SerializeField] private SimpleCombat simpleCombat;

    private bool paused;
    private bool dead;

    private Health playerHealth;

    private void Awake()
    {
        Time.timeScale = 1f;
        if (pausePanel) pausePanel.SetActive(false);
        if (deathPanel) deathPanel.SetActive(false);
        if (pauseButton) pauseButton.SetActive(true);
    }

    private void Start()
    {
        FindAndSubscribePlayer();
    }

    private void OnDestroy()
    {
        UnsubscribePlayer();
    }

    private void FindAndSubscribePlayer()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (!playerObj) return;

        playerHealth = playerObj.GetComponent<Health>();
        if (!playerHealth) return;

        playerHealth.Died -= OnPlayerDied;
        playerHealth.Died += OnPlayerDied;
    }

    private void UnsubscribePlayer()
    {
        if (playerHealth != null)
            playerHealth.Died -= OnPlayerDied;
    }

    private void OnPlayerDied()
    {
        if (dead) return;
        dead = true;

        // смерть = пауза + death panel
        Time.timeScale = 0f;
        SetGameplayEnabled(false);

        if (pausePanel) pausePanel.SetActive(false);
        if (deathPanel) deathPanel.SetActive(true);
        if (pauseButton) pauseButton.SetActive(false);
    }

    public void TogglePause()
    {
        if (dead) return;

        if (paused) ResumeGame();
        else PauseGame();
    }

    public void PauseGame()
    {
        if (dead) return;
        paused = true;

        Time.timeScale = 0f;
        SetGameplayEnabled(false);

        if (pausePanel) pausePanel.SetActive(true);
        if (deathPanel) deathPanel.SetActive(false);
        if (pauseButton) pauseButton.SetActive(true);
    }

    public void ResumeGame()
    {
        if (dead) return;
        paused = false;

        Time.timeScale = 1f;
        SetGameplayEnabled(true);

        if (pausePanel) pausePanel.SetActive(false);
        if (deathPanel) deathPanel.SetActive(false);
        if (pauseButton) pauseButton.SetActive(true);
    }

    public void RestartLevel()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void GoToMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(mainMenuSceneName);
    }

    private void SetGameplayEnabled(bool enabled)
    {
        if (simpleCombat == null || playerController == null) return;

        simpleCombat.enabled = enabled;
        playerController.enabled = enabled;
    }
}

