using System.Collections;
using UnityEngine;
using UnityEngine.UI;
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

    private bool gameRunning;
    private bool gameStarted;
    private Transform playerTransform;
    private Rigidbody playerRb;
    private float displayedSpeed;
    private bool uiInitialized;

    // Start screen objects (created at runtime)
    private GameObject startScreenObj;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        if (gameOverPanel != null) gameOverPanel.SetActive(false);

        GameObject player = GameObject.FindGameObjectWithTag("CameraTarget");
        if (player != null)
        {
            playerTransform = player.transform;
            playerRb = player.GetComponent<Rigidbody>();
        }

        // Hide speed HUD until game starts
        if (distanceText != null)
            distanceText.gameObject.SetActive(false);

        ShowStartScreen();
    }

    private void Update()
    {
        // Waiting for player to start
        if (!gameStarted)
        {
            if (Input.GetKeyDown(KeyCode.Space))
                StartGame();
            return;
        }

        if (!gameRunning) return;
        UpdateSpeedUI();
        if (Input.GetKeyDown(KeyCode.R))
            RestartGame();
    }

    // ─── Start Screen ────────────────────────────────────────

    private void ShowStartScreen()
    {
        Time.timeScale = 0f;
        gameStarted = false;
        gameRunning = false;

        // Find or create Canvas
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null) return;

        // Build start screen overlay
        startScreenObj = new GameObject("StartScreen");
        startScreenObj.transform.SetParent(canvas.transform, false);

        // Full-screen dark overlay
        RectTransform overlayRT = startScreenObj.AddComponent<RectTransform>();
        overlayRT.anchorMin = Vector2.zero;
        overlayRT.anchorMax = Vector2.one;
        overlayRT.sizeDelta = Vector2.zero;
        overlayRT.anchoredPosition = Vector2.zero;

        Image bg = startScreenObj.AddComponent<Image>();
        bg.color = new Color(0, 0, 0, 0.75f);

        // Title text
        GameObject titleObj = new GameObject("TitleText");
        titleObj.transform.SetParent(startScreenObj.transform, false);
        TMP_Text titleText = titleObj.AddComponent<TextMeshProUGUI>();
        if (gameOverFont != null) titleText.font = gameOverFont;
        titleText.text = "ARCADE ESCAPE";
        titleText.fontSize = 120;
        titleText.color = new Color(1f, 0.84f, 0f); // Gold
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.enableWordWrapping = false;
        titleText.fontStyle = FontStyles.Bold;

        Material titleMat = titleText.fontMaterial;
        titleMat.EnableKeyword("OUTLINE_ON");
        titleMat.SetColor("_OutlineColor", Color.black);
        titleMat.SetFloat("_OutlineWidth", 0.5f);
        titleMat.EnableKeyword("UNDERLAY_ON");
        titleMat.SetColor("_UnderlayColor", new Color(0, 0, 0, 0.6f));
        titleMat.SetFloat("_UnderlayOffsetX", 0.15f);
        titleMat.SetFloat("_UnderlayOffsetY", -0.15f);

        RectTransform titleRT = titleText.rectTransform;
        titleRT.anchorMin = new Vector2(0.5f, 0.5f);
        titleRT.anchorMax = new Vector2(0.5f, 0.5f);
        titleRT.pivot = new Vector2(0.5f, 0.5f);
        titleRT.anchoredPosition = new Vector2(0, 80);
        titleRT.sizeDelta = new Vector2(1200, 200);

        // "Press Space" prompt
        GameObject promptObj = new GameObject("PromptText");
        promptObj.transform.SetParent(startScreenObj.transform, false);
        TMP_Text promptText = promptObj.AddComponent<TextMeshProUGUI>();
        if (gameOverFont != null) promptText.font = gameOverFont;
        promptText.text = "PRESS SPACE TO START";
        promptText.fontSize = 50;
        promptText.color = Color.white;
        promptText.alignment = TextAlignmentOptions.Center;
        promptText.enableWordWrapping = false;

        Material promptMat = promptText.fontMaterial;
        promptMat.EnableKeyword("OUTLINE_ON");
        promptMat.SetColor("_OutlineColor", Color.black);
        promptMat.SetFloat("_OutlineWidth", 0.4f);

        RectTransform promptRT = promptText.rectTransform;
        promptRT.anchorMin = new Vector2(0.5f, 0.5f);
        promptRT.anchorMax = new Vector2(0.5f, 0.5f);
        promptRT.pivot = new Vector2(0.5f, 0.5f);
        promptRT.anchoredPosition = new Vector2(0, -40);
        promptRT.sizeDelta = new Vector2(800, 100);

        // Blinking effect
        StartCoroutine(BlinkPrompt(promptText));
    }

    private IEnumerator BlinkPrompt(TMP_Text prompt)
    {
        while (!gameStarted)
        {
            prompt.alpha = Mathf.PingPong(Time.unscaledTime * 2f, 1f);
            yield return null;
        }
    }

    private void StartGame()
    {
        gameStarted = true;
        gameRunning = true;
        Time.timeScale = 1f;

        if (startScreenObj != null)
            Destroy(startScreenObj);

        if (distanceText != null)
            distanceText.gameObject.SetActive(true);
    }

    // ─── Public Events ───────────────────────────────────────

    public void OnPlayerCaught()
    {
        if (!gameRunning) return;
        EndGame("CAUGHT BY POLICE!", "Press R to Restart", new Color(0.9f, 0.15f, 0.15f));
    }

    public void OnPlayerFlipped()
    {
        if (!gameRunning) return;
        EndGame("CAR CRASHED!", "Press R to Restart", new Color(0.9f, 0.15f, 0.15f));
    }

    public void OnPlayerReachedFinish()
    {
        if (!gameRunning) return;
        EndGame("YOU ESCAPED!", "Press R to Restart", new Color(0.1f, 0.85f, 0.2f));
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
            distanceText.enableWordWrapping = false;

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
            gameOverText.enableWordWrapping = false;
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

        StartCoroutine(WaitForRestart());
    }

    private IEnumerator WaitForRestart()
    {
        yield return new WaitForSecondsRealtime(0.5f);
        while (!Input.GetKeyDown(KeyCode.R))
            yield return null;
        RestartGame();
    }

    private void RestartGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
