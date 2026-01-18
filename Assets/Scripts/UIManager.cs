using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using DG.Tweening;

namespace FluencyDrive
{
    /// <summary>
    /// Manages all UI screens, transitions, and HUD updates.
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        public static UIManager Instance { get; private set; }

        [Header("Screens")]
        [SerializeField] private GameObject mainMenuScreen;
        [SerializeField] private GameObject gameplayScreen;
        [SerializeField] private GameObject pauseScreen;
        [SerializeField] private GameObject winScreen;
        [SerializeField] private GameObject gameOverScreen;

        [Header("HUD Elements")]
        [SerializeField] private Text scoreText;
        [SerializeField] private Text levelText;
        [SerializeField] private Text timerText;
        [SerializeField] private Text comboText;
        [SerializeField] private Slider progressBar;
        [SerializeField] private Image progressFill;

        [Header("Word Display")]
        [SerializeField] private Text wordDisplayText;
        [SerializeField] private Text hintText;
        [SerializeField] private Transform letterContainer;

        [Header("Definition Panel")]
        [SerializeField] private GameObject definitionPanel;
        [SerializeField] private Text definitionWordText;
        [SerializeField] private Text definitionCategoryText;
        [SerializeField] private Text definitionText;
        [SerializeField] private Text pronunciationText;
        [SerializeField] private Text examplesText;

        [Header("Win Screen Elements")]
        [SerializeField] private Text winLevelText;
        [SerializeField] private Text winWordText;
        [SerializeField] private Text winScoreText;
        [SerializeField] private Text winTimeBonusText;
        [SerializeField] private Text winPerfectBonusText;
        [SerializeField] private Text winTotalText;
        [SerializeField] private GameObject starContainer;
        [SerializeField] private Image[] starImages;

        [Header("Game Over Elements")]
        [SerializeField] private Text gameOverScoreText;
        [SerializeField] private Text gameOverHighScoreText;

        [Header("Main Menu Elements")]
        [SerializeField] private Text highScoreMenuText;
        [SerializeField] private Text maxLevelMenuText;
        [SerializeField] private Button continueButton;

        [Header("Animation Settings")]
        [SerializeField] private float screenTransitionDuration = 0.3f;
        [SerializeField] private float countUpDuration = 1f;

        [Header("Colors")]
        [SerializeField] private Color normalTimerColor = Color.white;
        [SerializeField] private Color warningTimerColor = Color.yellow;
        [SerializeField] private Color criticalTimerColor = Color.red;
        [SerializeField] private Color comboColor = new Color(1f, 0.8f, 0f);

        private Coroutine comboFadeCoroutine;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
                return;
            }
        }

        private void Start()
        {
            // Subscribe to game events
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGameStateChanged += HandleGameStateChanged;
                GameManager.Instance.OnScoreChanged += UpdateScore;
                GameManager.Instance.OnLevelChanged += UpdateLevel;
            }

            if (MatchManager.Instance != null)
            {
                MatchManager.Instance.OnScoreAwarded += HandleScoreAwarded;
            }

            if (WordManager.Instance != null)
            {
                WordManager.Instance.OnLetterProgress += HandleLetterProgress;
                WordManager.Instance.OnWordSelected += HandleWordSelected;
            }

            // Initialize UI
            InitializeMainMenu();
        }

        /// <summary>
        /// Initialize main menu with saved progress.
        /// </summary>
        private void InitializeMainMenu()
        {
            int highScore = PlayerPrefs.GetInt("FluencyDrive_HighScore", 0);
            int maxLevel = PlayerPrefs.GetInt("FluencyDrive_MaxLevel", 1);

            if (highScoreMenuText != null)
                highScoreMenuText.text = $"High Score: {highScore:N0}";

            if (maxLevelMenuText != null)
                maxLevelMenuText.text = $"Level Reached: {maxLevel}";

            // Enable/disable continue button based on progress
            if (continueButton != null)
                continueButton.interactable = maxLevel > 1;
        }

        /// <summary>
        /// Handle game state changes and show appropriate screens.
        /// </summary>
        private void HandleGameStateChanged(GameState newState)
        {
            switch (newState)
            {
                case GameState.Menu:
                    ShowScreen(mainMenuScreen);
                    break;
                case GameState.Playing:
                    ShowScreen(gameplayScreen);
                    HideDefinitionPanel();
                    break;
                case GameState.Paused:
                    ShowScreen(pauseScreen, true);
                    break;
                case GameState.LevelComplete:
                    // Win screen shown separately after animations
                    break;
                case GameState.GameOver:
                    ShowGameOverScreen();
                    break;
            }
        }

        /// <summary>
        /// Show a specific screen with animation.
        /// </summary>
        private void ShowScreen(GameObject screen, bool overlay = false)
        {
            if (!overlay)
            {
                // Hide all screens first
                HideAllScreens();
            }

            if (screen != null)
            {
                screen.SetActive(true);
                
                // Animate entrance
                CanvasGroup canvasGroup = screen.GetComponent<CanvasGroup>();
                if (canvasGroup == null)
                    canvasGroup = screen.AddComponent<CanvasGroup>();

                canvasGroup.alpha = 0f;
                canvasGroup.DOFade(1f, screenTransitionDuration);

                screen.transform.localScale = Vector3.one * 0.9f;
                screen.transform.DOScale(1f, screenTransitionDuration).SetEase(Ease.OutBack);
            }
        }

        /// <summary>
        /// Hide all screens.
        /// </summary>
        private void HideAllScreens()
        {
            if (mainMenuScreen != null) mainMenuScreen.SetActive(false);
            if (gameplayScreen != null) gameplayScreen.SetActive(false);
            if (pauseScreen != null) pauseScreen.SetActive(false);
            if (winScreen != null) winScreen.SetActive(false);
            if (gameOverScreen != null) gameOverScreen.SetActive(false);
        }

        // ═══════════════════════════════════════════════════════
        // HUD UPDATES
        // ═══════════════════════════════════════════════════════

        /// <summary>
        /// Update the score display.
        /// </summary>
        public void UpdateScore(int score)
        {
            if (scoreText != null)
            {
                // Animate score counting up
                scoreText.transform.DOKill();
                scoreText.transform.DOPunchScale(Vector3.one * 0.2f, 0.2f);
                scoreText.text = $"{score:N0}";
            }
        }

        /// <summary>
        /// Update the level display.
        /// </summary>
        public void UpdateLevel(int level)
        {
            if (levelText != null)
                levelText.text = $"Level {level}";
        }

        /// <summary>
        /// Update the timer display.
        /// </summary>
        public void UpdateTimer(float remainingTime, float totalTime)
        {
            if (timerText == null) return;

            int minutes = Mathf.FloorToInt(remainingTime / 60f);
            int seconds = Mathf.FloorToInt(remainingTime % 60f);
            timerText.text = $"{minutes:00}:{seconds:00}";

            // Change color based on time remaining
            float timeRatio = remainingTime / totalTime;
            if (timeRatio < 0.15f)
            {
                timerText.color = criticalTimerColor;
                // Pulse animation when critical
                if (!DOTween.IsTweening(timerText.transform))
                {
                    timerText.transform.DOScale(1.1f, 0.3f).SetLoops(-1, LoopType.Yoyo);
                }
            }
            else if (timeRatio < 0.3f)
            {
                timerText.color = warningTimerColor;
                timerText.transform.DOKill();
                timerText.transform.localScale = Vector3.one;
            }
            else
            {
                timerText.color = normalTimerColor;
            }
        }

        /// <summary>
        /// Handle score awarded event.
        /// </summary>
        private void HandleScoreAwarded(int score, int combo)
        {
            if (combo > 1)
            {
                ShowCombo(combo);
            }
        }

        /// <summary>
        /// Show combo feedback.
        /// </summary>
        private void ShowCombo(int combo)
        {
            if (comboText == null) return;

            if (comboFadeCoroutine != null)
                StopCoroutine(comboFadeCoroutine);

            comboText.gameObject.SetActive(true);
            comboText.text = $"COMBO x{combo}!";
            comboText.color = comboColor;

            comboText.transform.localScale = Vector3.zero;
            comboText.transform.DOScale(1f, 0.3f).SetEase(Ease.OutBack);

            comboFadeCoroutine = StartCoroutine(FadeComboText());
        }

        private IEnumerator FadeComboText()
        {
            yield return new WaitForSeconds(1.5f);
            
            if (comboText != null)
            {
                comboText.DOFade(0f, 0.5f).OnComplete(() =>
                {
                    comboText.gameObject.SetActive(false);
                    comboText.color = comboColor;
                });
            }
        }

        // ═══════════════════════════════════════════════════════
        // WORD DISPLAY
        // ═══════════════════════════════════════════════════════

        /// <summary>
        /// Handle when a new word is selected.
        /// </summary>
        private void HandleWordSelected(WordData word)
        {
            if (hintText != null)
                hintText.text = $"Category: {word.category}";

            UpdateWordDisplay(WordManager.Instance.GetDisplayWord());
            UpdateProgressBar(0f);
        }

        /// <summary>
        /// Handle letter progress updates.
        /// </summary>
        private void HandleLetterProgress(char letter, int revealed, int required)
        {
            if (WordManager.Instance != null)
            {
                UpdateWordDisplay(WordManager.Instance.GetDisplayWord());
                UpdateProgressBar(WordManager.Instance.GetProgressPercentage());
            }
        }

        /// <summary>
        /// Update the word display (with blanks for unrevealed letters).
        /// </summary>
        public void UpdateWordDisplay(string displayWord)
        {
            if (wordDisplayText != null)
            {
                wordDisplayText.text = displayWord;
                
                // Animate letter reveals
                wordDisplayText.transform.DOKill();
                wordDisplayText.transform.DOPunchScale(Vector3.one * 0.1f, 0.2f);
            }
        }

        /// <summary>
        /// Update progress bar.
        /// </summary>
        public void UpdateProgressBar(float progress)
        {
            if (progressBar != null)
            {
                progressBar.DOValue(progress, 0.3f).SetEase(Ease.OutQuad);
            }

            // Update fill color based on progress
            if (progressFill != null)
            {
                Color startColor = new Color(0.3f, 0.6f, 1f);
                Color endColor = new Color(0.3f, 1f, 0.5f);
                progressFill.color = Color.Lerp(startColor, endColor, progress);
            }
        }

        // ═══════════════════════════════════════════════════════
        // DEFINITION PANEL
        // ═══════════════════════════════════════════════════════

        /// <summary>
        /// Show the definition panel with word details.
        /// </summary>
        public void ShowDefinition(WordData word)
        {
            if (definitionPanel == null) return;

            definitionPanel.SetActive(true);

            // Animate entrance
            CanvasGroup canvasGroup = definitionPanel.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = definitionPanel.AddComponent<CanvasGroup>();

            canvasGroup.alpha = 0f;
            canvasGroup.DOFade(1f, 0.5f);

            definitionPanel.transform.localScale = Vector3.one * 0.8f;
            definitionPanel.transform.DOScale(1f, 0.5f).SetEase(Ease.OutBack);

            // Populate fields
            if (definitionWordText != null)
                definitionWordText.text = word.word;

            if (definitionCategoryText != null)
                definitionCategoryText.text = word.category;

            if (pronunciationText != null)
                pronunciationText.text = word.pronunciation ?? "";

            // Typewriter effect for definition
            if (definitionText != null)
            {
                StartCoroutine(TypewriterEffect(definitionText, word.definition));
            }

            // Show examples
            if (examplesText != null && word.examples != null && word.examples.Length > 0)
            {
                examplesText.text = "• " + string.Join("\n• ", word.examples);
            }
        }

        /// <summary>
        /// Typewriter effect for text.
        /// </summary>
        private IEnumerator TypewriterEffect(Text textComponent, string fullText)
        {
            textComponent.text = "";
            
            foreach (char c in fullText)
            {
                textComponent.text += c;
                yield return new WaitForSeconds(0.02f);
            }
        }

        /// <summary>
        /// Hide the definition panel.
        /// </summary>
        public void HideDefinitionPanel()
        {
            if (definitionPanel != null)
            {
                CanvasGroup canvasGroup = definitionPanel.GetComponent<CanvasGroup>();
                if (canvasGroup != null)
                {
                    canvasGroup.DOFade(0f, 0.3f).OnComplete(() =>
                    {
                        definitionPanel.SetActive(false);
                    });
                }
                else
                {
                    definitionPanel.SetActive(false);
                }
            }
        }

        // ═══════════════════════════════════════════════════════
        // WIN SCREEN
        // ═══════════════════════════════════════════════════════

        /// <summary>
        /// Show the win screen with statistics.
        /// </summary>
        public void ShowWinScreen(int level, string word, int baseScore, int timeBonus, int perfectBonus, int totalScore, int starCount)
        {
            if (winScreen == null) return;

            ShowScreen(winScreen);

            // Populate stats
            if (winLevelText != null)
                winLevelText.text = $"Level {level} Complete!";

            if (winWordText != null)
                winWordText.text = word;

            // Animate score counting
            StartCoroutine(AnimateWinScreenStats(baseScore, timeBonus, perfectBonus, totalScore, starCount));
        }

        private IEnumerator AnimateWinScreenStats(int baseScore, int timeBonus, int perfectBonus, int totalScore, int starCount)
        {
            // Base score
            if (winScoreText != null)
            {
                yield return AnimateNumber(winScoreText, 0, baseScore, 0.5f, "Score: ");
            }

            yield return new WaitForSeconds(0.2f);

            // Time bonus
            if (winTimeBonusText != null)
            {
                yield return AnimateNumber(winTimeBonusText, 0, timeBonus, 0.3f, "Time Bonus: +");
            }

            yield return new WaitForSeconds(0.2f);

            // Perfect bonus
            if (winPerfectBonusText != null)
            {
                if (perfectBonus > 0)
                {
                    winPerfectBonusText.gameObject.SetActive(true);
                    yield return AnimateNumber(winPerfectBonusText, 0, perfectBonus, 0.3f, "Perfect: +");
                }
                else
                {
                    winPerfectBonusText.gameObject.SetActive(false);
                }
            }

            yield return new WaitForSeconds(0.3f);

            // Total
            if (winTotalText != null)
            {
                winTotalText.transform.DOPunchScale(Vector3.one * 0.3f, 0.5f);
                yield return AnimateNumber(winTotalText, 0, totalScore, 0.8f, "Total: ");
            }

            // Stars
            yield return AnimateStars(starCount);
        }

        private IEnumerator AnimateNumber(Text textComponent, int from, int to, float duration, string prefix = "")
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                int current = Mathf.RoundToInt(Mathf.Lerp(from, to, elapsed / duration));
                textComponent.text = $"{prefix}{current:N0}";
                yield return null;
            }
            textComponent.text = $"{prefix}{to:N0}";
        }

        private IEnumerator AnimateStars(int count)
        {
            if (starImages == null) yield break;

            for (int i = 0; i < starImages.Length; i++)
            {
                if (starImages[i] != null)
                {
                    starImages[i].transform.localScale = Vector3.zero;
                    
                    if (i < count)
                    {
                        starImages[i].color = Color.yellow;
                        starImages[i].transform.DOScale(1f, 0.3f).SetEase(Ease.OutBack);
                        AudioManager.Instance?.PlaySound("BonusAwarded");
                    }
                    else
                    {
                        starImages[i].color = Color.gray;
                        starImages[i].transform.DOScale(0.8f, 0.2f);
                    }
                    
                    yield return new WaitForSeconds(0.2f);
                }
            }
        }

        // ═══════════════════════════════════════════════════════
        // GAME OVER SCREEN
        // ═══════════════════════════════════════════════════════

        /// <summary>
        /// Show game over screen.
        /// </summary>
        private void ShowGameOverScreen()
        {
            ShowScreen(gameOverScreen);

            int currentScore = GameManager.Instance?.CurrentScore ?? 0;
            int highScore = PlayerPrefs.GetInt("FluencyDrive_HighScore", 0);

            if (gameOverScoreText != null)
                gameOverScoreText.text = $"Score: {currentScore:N0}";

            if (gameOverHighScoreText != null)
            {
                if (currentScore > highScore)
                {
                    gameOverHighScoreText.text = "NEW HIGH SCORE!";
                    gameOverHighScoreText.color = Color.yellow;
                    gameOverHighScoreText.transform.DOPunchScale(Vector3.one * 0.3f, 0.5f).SetLoops(3);
                }
                else
                {
                    gameOverHighScoreText.text = $"High Score: {highScore:N0}";
                }
            }
        }

        // ═══════════════════════════════════════════════════════
        // BUTTON HANDLERS
        // ═══════════════════════════════════════════════════════

        public void OnPlayButton()
        {
            GameManager.Instance?.StartNewGame();
        }

        public void OnContinueButton()
        {
            GameManager.Instance?.ContinueGame();
        }

        public void OnPauseButton()
        {
            GameManager.Instance?.PauseGame();
        }

        public void OnResumeButton()
        {
            GameManager.Instance?.ResumeGame();
        }

        public void OnNextLevelButton()
        {
            GameManager.Instance?.NextLevel();
        }

        public void OnReplayButton()
        {
            GameManager.Instance?.ReplayLevel();
        }

        public void OnMainMenuButton()
        {
            GameManager.Instance?.ReturnToMenu();
            InitializeMainMenu();
        }

        public void OnQuitButton()
        {
            #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
            #else
            Application.Quit();
            #endif
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGameStateChanged -= HandleGameStateChanged;
                GameManager.Instance.OnScoreChanged -= UpdateScore;
                GameManager.Instance.OnLevelChanged -= UpdateLevel;
            }

            if (MatchManager.Instance != null)
            {
                MatchManager.Instance.OnScoreAwarded -= HandleScoreAwarded;
            }

            if (WordManager.Instance != null)
            {
                WordManager.Instance.OnLetterProgress -= HandleLetterProgress;
                WordManager.Instance.OnWordSelected -= HandleWordSelected;
            }
        }
    }
}
