using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Manages level progression across the game.
/// Persists between scenes using DontDestroyOnLoad.
/// Holds the list of LevelConfigs and tracks the current level index.
/// </summary>
public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance { get; private set; }

    [Header("Level Configs (in order)")]
    [Tooltip("Assign your LevelConfig assets here, one per level, in play order.")]
    public LevelConfig[] levels;

    /// <summary>
    /// Index of the level currently being played.
    /// Persists across scene loads.
    /// </summary>
    public int CurrentLevelIndex { get; private set; }

    /// <summary>
    /// Config for the level currently being played.
    /// </summary>
    public LevelConfig CurrentLevel
    {
        get
        {
            if (levels == null || levels.Length == 0)
                return null;

            return levels[Mathf.Clamp(CurrentLevelIndex, 0, levels.Length - 1)];
        }
    }

    /// <summary>
    /// True if the current level is the last one in the list.
    /// </summary>
    public bool IsLastLevel
    {
        get
        {
            return levels == null ||
                   CurrentLevelIndex >= levels.Length - 1;
        }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    /// <summary>
    /// Load the next level. If already on the last level, restarts from level 1.
    /// </summary>
    public void LoadNextLevel()
    {
        CurrentLevelIndex++;

        if (CurrentLevelIndex >= levels.Length)
        {
            // Loop back to first level (or you could show a "You beat the game" scene)
            CurrentLevelIndex = 0;
        }

        LoadCurrentLevel();
    }

    /// <summary>
    /// Restart the current level.
    /// </summary>
    public void RestartCurrentLevel()
    {
        LoadCurrentLevel();
    }

    /// <summary>
    /// Load a specific level by index.
    /// </summary>
    public void LoadLevel(int index)
    {
        CurrentLevelIndex = Mathf.Clamp(index, 0, levels.Length - 1);
        LoadCurrentLevel();
    }

    private void LoadCurrentLevel()
    {
        Time.timeScale = 1f;

        LevelConfig config = CurrentLevel;
        if (config != null && !string.IsNullOrEmpty(config.sceneName))
        {
            SceneManager.LoadScene(config.sceneName);
        }
        else
        {
            // Fallback: reload current scene
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
}
