using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class PauseManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject pauseMenuPanel;
    [SerializeField] private ConfirmationDialog confirmationDialog;

    [Header("Input")]
    [SerializeField] private Key pauseKey = Key.Escape;
    [SerializeField] private Key resetKey = Key.R;

    private bool isPaused = false;
    private bool isSinglePlayer = false;



    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if(pauseMenuPanel != null)
        {
            pauseMenuPanel.SetActive(false);
        }

        UpdateSinglePlayerStatus();
    }

    // Update is called once per frame
    void Update()
    {
        var kb = Keyboard.current;
        if (kb == null) { return; }

        if (kb[pauseKey].wasPressedThisFrame)
        {
            TogglePause();
        }

        if (kb[resetKey].wasPressedThisFrame && !isPaused)
        {
            ShowResetConfirmation();
        }
        
    }

    private void UpdateSinglePlayerStatus()
    {
        if(NetworkManager.Singleton == null)
        {
            isSinglePlayer = true;
            return;
        }

        isSinglePlayer = NetworkManager.Singleton.ConnectedClients.Count <= 1;
    }

    public void TogglePause()
    {
        if (isPaused)
        {
            Resume();
        }
        else
        {
            Pause();
        }
    }

    public void Pause()
    {
        UpdateSinglePlayerStatus();

        isPaused = true;

        if(pauseMenuPanel != null)
        {
            pauseMenuPanel.SetActive(true);
        }

        if (isSinglePlayer)
        {
            Time.timeScale = 0f;
        }
        else
        {
            DisableLocalPlayerInput();
        }

        if(AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayMenuClick();
        }
    }

    public void Resume()
    {
        isPaused = false;

        if(pauseMenuPanel != null)
        {
            pauseMenuPanel.SetActive(false);
        }

        if(isSinglePlayer)
        {
            Time.timeScale = 1.0f;
        }
        else
        {
            EnableLocalPlayerInput();
        }

        if(AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayMenuClick();
        }
    }

    private void DisableLocalPlayerInput()
    {
        var playerController = FindLocalPlayer();
        if(playerController != null)
        {
            playerController.enabled = false;
        }
    }

    private void EnableLocalPlayerInput()
    {
        var playerController = FindLocalPlayer();
        if(playerController != null)
        {
            playerController.enabled = true;
        }
    }

    private Player.PlayerController FindLocalPlayer()
    {
        if (NetworkManager.Singleton == null) return null;

        ulong localClientId = NetworkManager.Singleton.LocalClientId;
        var playerNetObj = NetworkManager.Singleton.SpawnManager.GetPlayerNetworkObject(localClientId);

        if(playerNetObj == null) return null;

        return playerNetObj.GetComponent<Player.PlayerController>();
    }

    public void OnResumeButton()
    {
        Resume();
    }

    public void OnSettingsButton()
    {
        // TODO: Implement Settings Panel
    }

    public void OnResetButton()
    {
        ShowResetConfirmation();
    }

    public void OnQuitButton()
    {
        ShowQuitConfirmation();
    }

    private void ShowResetConfirmation()
    {
        if (confirmationDialog != null)
        {
            confirmationDialog.Show(
                title: "Restart Run?",
                message: "All progress will be lost. Are you sure?",
                onConfirm: ExecuteReset,
                onCancel: null
            );
        }
        else
        {
            ExecuteReset();
        }
    }

    private void ShowQuitConfirmation()
    {
        if (confirmationDialog != null)
        {
            confirmationDialog.Show(
                title: "Quit to Desktop?",
                message: "All progress will be lost. Are you sure?",
                onConfirm: ExecuteQuit,
                onCancel: null
            );
        }
        else
        {
            ExecuteQuit();
        }
    }

    private void ExecuteReset()
    {
        if (isSinglePlayer)
        {
            Time.timeScale = 1f;
        }

        if (pauseMenuPanel != null)
        {
            pauseMenuPanel.SetActive(false);
        }

        isPaused = false;

        ResetRun();
    }

    private void ExecuteQuit()
    {
        if (isSinglePlayer)
        {
            Time.timeScale = 1f;
        }

        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
        {
            NetworkManager.Singleton.Shutdown();
        }

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void ResetRun()
    {
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
        {
            NetworkManager.Singleton.SceneManager.LoadScene(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene().name,
                UnityEngine.SceneManagement.LoadSceneMode.Single
            );
        }
        else
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex
            );
        }
    }
}
