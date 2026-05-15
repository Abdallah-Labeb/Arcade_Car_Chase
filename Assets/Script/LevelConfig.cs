using UnityEngine;

/// <summary>
/// ScriptableObject that defines per-level settings.
/// Create one for each level via:
///   Right-click in Project → Create → Arcade Car Chase → Level Config
/// </summary>
[CreateAssetMenu(
    fileName = "LevelConfig",
    menuName = "Arcade Car Chase/Level Config",
    order = 1
)]
public class LevelConfig : ScriptableObject
{
    [Header("Level Info")]
    public string levelName = "Level 1";

    [Tooltip("Scene name to load (must match the name in Build Settings)")]
    public string sceneName = "Level";

    [Header("Cop Difficulty")]
    [Tooltip("Base throttle for the cop AI (higher = faster cop)")]
    [Range(0.5f, 1.2f)]
    public float copBaseThrottle = 0.8f;

    [Tooltip("Max speed of the cop car")]
    public float copMaxSpeed = 45f;

    [Tooltip("How close the cop starts behind the player (Z offset)")]
    public float copStartOffset = 15f;

    [Header("Player Settings")]
    [Tooltip("Player max speed for this level")]
    public float playerMaxSpeed = 45f;
}
