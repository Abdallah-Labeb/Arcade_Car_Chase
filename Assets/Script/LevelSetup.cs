using UnityEngine;

/// <summary>
/// Place this on any GameObject in each level scene.
/// On Start, it reads the current LevelConfig from LevelManager
/// and applies the settings to the player car and cop AI.
/// </summary>
public class LevelSetup : MonoBehaviour
{
    [Header("Fallback Config (used if LevelManager is not present)")]
    [Tooltip("Optional fallback config when testing a scene directly without going through LevelManager")]
    public LevelConfig fallbackConfig;

    private void Start()
    {
        LevelConfig config = null;

        // Get config from LevelManager (persists across scenes)
        if (LevelManager.Instance != null)
            config = LevelManager.Instance.CurrentLevel;

        // Fallback for testing individual scenes directly
        if (config == null)
            config = fallbackConfig;

        if (config == null)
        {
            Debug.Log("[LevelSetup] No LevelConfig found. Using default scene values.");
            return;
        }

        Debug.Log($"[LevelSetup] Applying config: {config.levelName}");

        ApplyConfig(config);
    }

    private void ApplyConfig(LevelConfig config)
    {
        // --- Apply to Player ---
        GameObject playerObj = GameObject.FindGameObjectWithTag("CameraTarget");
        if (playerObj != null)
        {
            CarController playerCar = playerObj.GetComponent<CarController>();
            if (playerCar != null)
            {
                playerCar.maxSpeed = config.playerMaxSpeed;
            }
        }

        // --- Apply to Cop ---
        CarChaseAI[] cops = Object.FindObjectsByType<CarChaseAI>(FindObjectsSortMode.None);
        foreach (CarChaseAI cop in cops)
        {
            cop.baseThrottle = config.copBaseThrottle;

            CarController copCar = cop.GetComponent<CarController>();
            if (copCar != null)
            {
                copCar.maxSpeed = config.copMaxSpeed;
            }
        }
    }
}
