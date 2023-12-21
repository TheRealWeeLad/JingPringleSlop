using UnityEngine;
using UnityEngine.UI;

public class UIManagement : MonoBehaviour
{
    // Layers
    GameObject _pauseOuterLayer;
    GameObject _pauseSettings;
    GameObject _crossSettings;

    // Settings
    Slider _sensitivitySlider;

    [Header("Player")]
    public GameObject player;

    [Header("Menu Objects")]
    public GameObject pauseMenuObj;
    public GameObject crosshairObj;
    public GameObject abilityObj;
    public GameObject HUD;

    [Header("Mouse Cursor Settings")]
    public bool cursorLocked = true;

    public static bool paused = false;

    private void Awake()
    {
        if (paused) TogglePause();
    }

    // Start is called before the first frame update
    void Start()
    {
        // Pause Outer Layer
        _pauseOuterLayer = pauseMenuObj.transform.GetChild(0).gameObject;

        // Pause Settings
        _pauseSettings = pauseMenuObj.transform.GetChild(1).gameObject;
        _sensitivitySlider = _pauseSettings.transform.GetChild(2).GetComponent<Slider>();
        _crossSettings = pauseMenuObj.transform.GetChild(2).gameObject;

        // Initialize value of Sensitivity Slider
        _sensitivitySlider.value = FPSController.RotationSpeed;

        // Pause Game to Select Abilities
        if (GameManager.CurrentGame.Equals("GlimboKiller")) Cursor.lockState = CursorLockMode.None;

        // If in editor
        if (Application.isEditor && UnityEngine.SceneManagement.SceneManager.GetActiveScene().name.Equals("GlimboKiller")) Cursor.lockState = CursorLockMode.None;
    }

    // Sensitivity Slider
    public void ChangeSensitivity() => FPSController.RotationSpeed = _sensitivitySlider.value;

    // Toggle value of paused
    public static void TogglePause()
    {
        if (paused)
        {
            paused = false;
            Resume();
        }
        else
        {
            paused = true;
            Pause();
        }
    }

    // Get current pause state
    public static bool IsPaused()
    {
        return paused;
    }

    // Resume Gameplay
    public static void Resume()
    {
        EnemyManager.StartEnemyMovement();
    }
    // Pause Gameplay
    public static void Pause()
    {
        EnemyManager.StopEnemyMovement();
    }

    // Say whether menu is in crosshair settings
    public bool InCross()
    {
        _crossSettings = pauseMenuObj.transform.GetChild(2).gameObject;
        return _crossSettings.activeInHierarchy;
    }

    // Exit pause menu
    public void OnResume()
    {
        pauseMenuObj.SetActive(false);
        crosshairObj.SetActive(true);
        Cursor.lockState = cursorLocked ? CursorLockMode.Locked : CursorLockMode.None;
        TogglePause();
    }

    // Close Ability Chooser
    public void CloseAbilityChooser()
    {
        abilityObj.SetActive(false);
        HUD.SetActive(true);
        Cursor.lockState = CursorLockMode.Locked;
    }

    // Open Settings
    public void OpenSettings()
    {
        _pauseOuterLayer.SetActive(false);
        _pauseSettings.SetActive(true);
    }

    // Close Settings
    public void CloseSettings()
    {
        _pauseOuterLayer.SetActive(true);
        _pauseSettings.SetActive(false);
    }

    // Open Crosshair Settings
    public void OpenCrosshairSettings()
    {
        _pauseSettings.SetActive(false);
        _crossSettings.SetActive(true);
        crosshairObj.SetActive(true);
    }

    // Close Crosshair Settings
    public void CloseCrosshairSettings()
    {
        _pauseSettings.SetActive(true);
        _crossSettings.SetActive(false);
        crosshairObj.SetActive(false);
    }

    // Quit Game
    public void Quit()
    {
        TogglePause();
        GameManager.QuitGame();
    }
}
