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

    private bool gameRunning = true;
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

        StartCoroutine(WaitForRestart());
    }

    private IEnumerator WaitForRestart()
    {
        yield return new WaitForSecondsRealtime(0.5f);
        while (!Input.GetKeyDown(KeyCode.R))
            yield return new WaitForSecondsRealtime(0f);
        RestartGame();
    }

    private void RestartGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
