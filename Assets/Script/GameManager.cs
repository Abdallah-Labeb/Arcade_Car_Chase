using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("UI References")]
    public TMP_Text distanceText;
    public GameObject gameOverPanel;
    public TMP_Text gameOverText;
    public TMP_FontAsset gameOverFont;

    [Header("Level Display")]
    public TMP_Text levelNameText;

    private bool gameRunning = true;
    private bool levelWon;
    private Transform playerTransform;
    private Rigidbody playerRb;
    private float displayedSpeed;
    private bool uiInitialized;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void Start()
    {
        Time.timeScale = 1f;
        if (gameOverPanel != null) gameOverPanel.SetActive(false);

        GameObject player = GameObject.FindGameObjectWithTag("CameraTarget");
        if (player != null)
        {
            playerTransform = player.transform;
            playerRb = player.GetComponent<Rigidbody>();
        }

        if (distanceText != null)
        {
            distanceText.text = "";
            distanceText.gameObject.SetActive(true);
        }

        // Show level name briefly
        ShowLevelName();
    }

    private void Update()
    {
        if (!gameRunning) return;
        UpdateSpeedUI();
        if (Input.GetKeyDown(KeyCode.R))
            RestartGame();
    }

    // ─── Public Events ───────────────────────────────────────

    public void OnPlayerCaught()
    {
        if (!gameRunning) return;
        levelWon = false;
        EndGame("CAUGHT BY POLICE!", "Press R to Restart", new Color(0.9f, 0.15f, 0.15f));
    }

    public void OnPlayerFlipped()
    {
        if (!gameRunning) return;
        levelWon = false;
        EndGame("CAR CRASHED!", "Press R to Restart", new Color(0.9f, 0.15f, 0.15f));
    }

    public void OnPlayerReachedFinish()
    {
        if (!gameRunning) return;
        levelWon = true;

        bool isLast = LevelManager.Instance == null || LevelManager.Instance.IsLastLevel;

        if (isLast)
        {
            EndGame("YOU WIN!", "Press R to Play Again", new Color(1f, 0.84f, 0f));
        }
        else
        {
            EndGame("LEVEL COMPLETE!", "Press SPACE for Next Level  |  R to Restart", new Color(0.1f, 0.85f, 0.2f));
        }
    }

    // ─── Level Name Display ──────────────────────────────────

    private void ShowLevelName()
    {
        if (levelNameText == null) return;

        string name = "Level 1";

        if (LevelManager.Instance != null && LevelManager.Instance.CurrentLevel != null)
            name = LevelManager.Instance.CurrentLevel.levelName;

        if (gameOverFont != null)
            levelNameText.font = gameOverFont;

        levelNameText.text = name;
        levelNameText.gameObject.SetActive(true);

        // Fade out after a few seconds
        StartCoroutine(FadeLevelName());
    }

    private IEnumerator FadeLevelName()
    {
        // Show for 2 seconds
        yield return new WaitForSeconds(2f);

        // Fade out over 1 second
        float elapsed = 0f;
        Color startColor = levelNameText.color;

        while (elapsed < 1f)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, elapsed);
            levelNameText.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
            yield return null;
        }

        levelNameText.gameObject.SetActive(false);
        levelNameText.color = startColor; // Reset for next time
    }

    // ─── Speed HUD ───────────────────────────────────────────

    private void UpdateSpeedUI()
    {
        if (distanceText == null || playerRb == null) return;

        if (!uiInitialized && gameOverFont != null)
        {
            distanceText.font = gameOverFont;
            RectTransform rt = distanceText.rectTransform;
            rt.anchorMin = new Vector2(1, 0);
            rt.anchorMax = new Vector2(1, 0);
            rt.pivot = new Vector2(1, 0);
            rt.anchoredPosition = new Vector2(-60, 60);
            distanceText.fontSize = 100;
            distanceText.alignment = TextAlignmentOptions.BottomRight;
            distanceText.textWrappingMode = TextWrappingModes.NoWrap;

            Material mat = distanceText.fontMaterial;
            mat.EnableKeyword("OUTLINE_ON");
            mat.SetColor("_OutlineColor", Color.black);
            mat.SetFloat("_OutlineWidth", 0.45f);

            uiInitialized = true;
        }

        displayedSpeed = Mathf.Lerp(displayedSpeed, playerRb.linearVelocity.magnitude * 3.6f, Time.deltaTime * 5f);
        distanceText.text = $"{Mathf.RoundToInt(displayedSpeed)} KM/H";
    }

    // ─── Game Over ───────────────────────────────────────────

    private void EndGame(string title, string subtitle, Color titleColor)
    {
        gameRunning = false;
        Time.timeScale = 0f;

        if (gameOverPanel != null) gameOverPanel.SetActive(true);

        if (gameOverText != null)
        {
            if (gameOverFont != null)
                gameOverText.font = gameOverFont;

            gameOverText.margin = new Vector4(-100, -50, -100, -50);
            gameOverText.rectTransform.sizeDelta = new Vector2(1000, 400);
            gameOverText.rectTransform.anchoredPosition = new Vector2(0, 180f);
            gameOverText.fontStyle = FontStyles.Bold;
            gameOverText.textWrappingMode = TextWrappingModes.NoWrap;
            gameOverText.enableAutoSizing = true;
            gameOverText.fontSizeMin = 40;
            gameOverText.fontSizeMax = 150;
            gameOverText.alignment = TextAlignmentOptions.Center;

            Material mat = gameOverText.fontMaterial;
            mat.EnableKeyword("OUTLINE_ON");
            mat.SetColor("_OutlineColor", Color.black);
            mat.SetFloat("_OutlineWidth", 0.5f);
            mat.EnableKeyword("UNDERLAY_ON");
            mat.SetColor("_UnderlayColor", new Color(0, 0, 0, 0.5f));
            mat.SetFloat("_UnderlayOffsetX", 0.1f);
            mat.SetFloat("_UnderlayOffsetY", -0.1f);

            string hex = ColorUtility.ToHtmlStringRGB(titleColor);
            gameOverText.text = $"<color=#{hex}><nobr>{title}</nobr></color>\n<size=50><color=#FFFFFF>{subtitle}</color></size>";
        }

        StartCoroutine(WaitForInput());
    }

    private IEnumerator WaitForInput()
    {
        yield return new WaitForSecondsRealtime(0.5f);

        while (true)
        {
            // R always restarts current level
            if (Input.GetKeyDown(KeyCode.R))
            {
                RestartGame();
                yield break;
            }

            // SPACE advances to next level (only after winning and not on the last level)
            if (levelWon && Input.GetKeyDown(KeyCode.Space))
            {
                LoadNextLevel();
                yield break;
            }

            yield return new WaitForSecondsRealtime(0f);
        }
    }

    private void RestartGame()
    {
        Time.timeScale = 1f;

        if (LevelManager.Instance != null)
        {
            LevelManager.Instance.RestartCurrentLevel();
        }
        else
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }

    private void LoadNextLevel()
    {
        Time.timeScale = 1f;

        if (LevelManager.Instance != null)
        {
            LevelManager.Instance.LoadNextLevel();
        }
    }
}
