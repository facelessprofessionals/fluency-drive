using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System;
using DG.Tweening;

namespace FluencyDrive
{
    /// <summary>
    /// Main game manager - handles game state, level flow, scoring, and progression.
    /// Implements the level completion sequence:
    /// IF all_letters_revealed == true
    ///   → pause gameplay
    ///   → animate word assembly
    ///   → display definition
    ///   → award bonuses
    ///   → unlock next level
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Game State")]
        [SerializeField] private GameState currentState = GameState.Menu;

        [Header("Level Settings")]
        [SerializeField] private int currentLevel = 1;
        [SerializeField] private float levelTimeLimit = 120f; // 2 minutes

        [Header("Scoring")]
        [SerializeField] private int timeBonus = 500;
        [SerializeField] private int perfectMatchBonus = 1000;
        [SerializeField] private int levelCompleteBonus = 250;

        [Header("References")]
        [SerializeField] private GridManager gridManager;
        [SerializeField] private WordManager wordManager;
        [SerializeField] private MatchManager matchManager;

        [Header("UI References")]
        [SerializeField] private Text scoreText;
        [SerializeField] private Text levelText;
        [SerializeField] private Text timerText;
        [SerializeField] private Text wordDisplayText;
        [SerializeField] private Text definitionText;
        [SerializeField] private Slider progressBar;
        [SerializeField] private GameObject winScreenPanel;
        [SerializeField] private Transform wordAssemblyContainer;

        // Game data
        private int currentScore = 0;
        private float remainingTime;
        private int totalMatches = 0;
        private int perfectMatches = 0; // Matches without misses
        private bool isPaused = false;

        // Events
        public event Action<int> OnScoreChanged;
        public event Action<int> OnLevelChanged;
        public event Action<GameState> OnGameStateChanged;
        public event Action OnLevelCompleted;
        public event Action OnGameOver;

        public int CurrentLevel => currentLevel;
        public int CurrentScore => currentScore;
        public GameState State => currentState;

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
            InitializeGame();
        }

        private void Update()
        {
            if (currentState == GameState.Playing && !isPaused)
            {
                UpdateTimer();
                UpdateUI();
            }
        }

        /// <summary>
        /// Initialize the game systems.
        /// </summary>
        private void InitializeGame()
        {
            // Subscribe to events
            if (matchManager != null)
            {
                matchManager.OnScoreAwarded += HandleScoreAwarded;
                matchManager.OnMatchResult += HandleMatchResult;
                matchManager.OnAllLettersRevealed += HandleAllLettersRevealed;
            }

            if (wordManager != null)
            {
                wordManager.OnWordCompleted += HandleWordCompleted;
                wordManager.OnLetterProgress += HandleLetterProgress;
            }

            // Load saved progress
            LoadProgress();

            // Start at menu or auto-start level
            ChangeState(GameState.Menu);
        }

        /// <summary>
        /// Start a new game from level 1.
        /// </summary>
        public void StartNewGame()
        {
            currentLevel = 1;
            currentScore = 0;
            StartLevel(currentLevel);
        }

        /// <summary>
        /// Continue from saved progress.
        /// </summary>
        public void ContinueGame()
        {
            StartLevel(currentLevel);
        }

        /// <summary>
        /// Start a specific level.
        /// </summary>
        public void StartLevel(int level)
        {
            currentLevel = level;
            remainingTime = levelTimeLimit;
            totalMatches = 0;
            perfectMatches = 0;

            // Setup word for this level
            wordManager?.SetupLevel(level);

            // Get letters for the grid
            char[] letters = wordManager?.GetLettersForGrid() ?? new char[0];

            // Initialize the grid
            gridManager?.InitializeGrid(letters);

            // Reset match manager
            matchManager?.ResetForNewLevel();

            // Update UI
            UpdateLevelUI();
            
            if (winScreenPanel != null)
                winScreenPanel.SetActive(false);

            // Change to playing state
            ChangeState(GameState.Playing);

            OnLevelChanged?.Invoke(level);
        }

        /// <summary>
        /// Handle when score is awarded from matching.
        /// </summary>
        private void HandleScoreAwarded(int score, int combo)
        {
            AddScore(score);
            
            // Visual feedback for combo
            if (combo > 1)
            {
                ShowComboFeedback(combo);
            }
        }

        /// <summary>
        /// Handle match results for tracking perfect games.
        /// </summary>
        private void HandleMatchResult(Tile tile1, Tile tile2, bool wasMatch)
        {
            if (wasMatch)
            {
                totalMatches++;
                perfectMatches++;
            }
            else
            {
                // Reset perfect match streak
                perfectMatches = 0;
            }
        }

        /// <summary>
        /// Handle letter progress updates.
        /// </summary>
        private void HandleLetterProgress(char letter, int revealed, int required)
        {
            // Update progress bar
            if (progressBar != null && wordManager != null)
            {
                progressBar.value = wordManager.GetProgressPercentage();
            }

            // Update word display
            if (wordDisplayText != null && wordManager != null)
            {
                wordDisplayText.text = wordManager.GetDisplayWord();
            }
        }

        /// <summary>
        /// Handle when all letters are revealed - TRIGGER LEVEL COMPLETION SEQUENCE.
        /// </summary>
        private void HandleAllLettersRevealed()
        {
            StartCoroutine(LevelCompleteSequence());
        }

        /// <summary>
        /// Handle word completion (may have multiple words per level in future).
        /// </summary>
        private void HandleWordCompleted(WordData word)
        {
            Debug.Log($"Word completed: {word.word}");
        }

        /// <summary>
        /// THE LEVEL COMPLETION SEQUENCE
        /// IF all_letters_revealed == true
        ///   → pause gameplay
        ///   → animate word assembly
        ///   → display definition
        ///   → award bonuses
        ///   → unlock next level
        /// </summary>
        private IEnumerator LevelCompleteSequence()
        {
            // ═══════════════════════════════════════════════════════
            // STEP 1: PAUSE GAMEPLAY
            // ═══════════════════════════════════════════════════════
            ChangeState(GameState.LevelComplete);
            isPaused = true;
            
            // Disable grid interaction
            gridManager?.SetGridInteractable(false);

            // Play level complete sound
            AudioManager.Instance?.PlaySound("LevelComplete");

            yield return new WaitForSeconds(0.5f);

            // ═══════════════════════════════════════════════════════
            // STEP 2: ANIMATE WORD ASSEMBLY
            // ═══════════════════════════════════════════════════════
            yield return AnimateWordAssembly();

            yield return new WaitForSeconds(0.8f);

            // ═══════════════════════════════════════════════════════
            // STEP 3: DISPLAY DEFINITION
            // ═══════════════════════════════════════════════════════
            yield return DisplayDefinition();

            yield return new WaitForSeconds(1.5f);

            // ═══════════════════════════════════════════════════════
            // STEP 4: AWARD BONUSES
            // ═══════════════════════════════════════════════════════
            yield return AwardBonuses();

            yield return new WaitForSeconds(1f);

            // ═══════════════════════════════════════════════════════
            // STEP 5: UNLOCK NEXT LEVEL
            // ═══════════════════════════════════════════════════════
            UnlockNextLevel();

            // Show win screen
            ShowWinScreen();
        }

        /// <summary>
        /// Animate tiles flying into position to form the word.
        /// </summary>
        private IEnumerator AnimateWordAssembly()
        {
            if (wordManager?.CurrentWord == null || wordAssemblyContainer == null)
                yield break;

            string word = wordManager.CurrentWord.word.ToUpper();
            
            // Get all matched tiles
            var allTiles = gridManager?.AllTiles;
            if (allTiles == null) yield break;

            // Calculate positions for assembled word
            float letterSpacing = 80f;
            float startX = -(word.Length - 1) * letterSpacing / 2f;
            Vector3 assemblyStart = wordAssemblyContainer.position;

            int letterIndex = 0;
            foreach (char c in word)
            {
                if (!char.IsLetter(c))
                {
                    letterIndex++;
                    continue;
                }

                // Find a matched tile with this letter
                Tile tileToAnimate = null;
                foreach (Tile tile in allTiles)
                {
                    if (tile.IsMatched && tile.Letter == c)
                    {
                        tileToAnimate = tile;
                        break;
                    }
                }

                if (tileToAnimate != null)
                {
                    Vector3 targetPos = assemblyStart + new Vector3(startX + letterIndex * letterSpacing, 0, 0);
                    tileToAnimate.AnimateToPosition(targetPos, letterIndex * 0.1f);
                }

                letterIndex++;
            }

            // Wait for animations to complete
            yield return new WaitForSeconds(word.Length * 0.1f + 0.6f);

            // Play word complete sound
            AudioManager.Instance?.PlaySound("WordAssembled");
        }

        /// <summary>
        /// Display the word definition with animation.
        /// </summary>
        private IEnumerator DisplayDefinition()
        {
            if (definitionText == null || wordManager?.CurrentWord == null)
                yield break;

            WordData word = wordManager.CurrentWord;

            // Setup definition text
            definitionText.text = "";
            definitionText.gameObject.SetActive(true);

            // Fade in
            CanvasGroup canvasGroup = definitionText.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = definitionText.gameObject.AddComponent<CanvasGroup>();

            canvasGroup.alpha = 0f;
            canvasGroup.DOFade(1f, 0.5f);

            // Typewriter effect for definition
            string fullDefinition = $"<b>{word.word}</b>\n<i>{word.category}</i>\n\n{word.definition}";
            
            foreach (char c in fullDefinition)
            {
                definitionText.text += c;
                
                if (c != ' ' && c != '\n')
                {
                    yield return new WaitForSeconds(0.02f);
                }
            }

            // Play definition sound
            AudioManager.Instance?.PlaySound("ShowDefinition");
        }

        /// <summary>
        /// Calculate and award bonuses.
        /// </summary>
        private IEnumerator AwardBonuses()
        {
            int totalBonus = 0;

            // Time bonus - based on remaining time
            if (remainingTime > 0)
            {
                int timeBonusAwarded = Mathf.RoundToInt((remainingTime / levelTimeLimit) * timeBonus);
                totalBonus += timeBonusAwarded;
                ShowBonusFeedback($"Time Bonus: +{timeBonusAwarded}");
                yield return new WaitForSeconds(0.3f);
            }

            // Perfect match bonus - if no mismatches
            if (perfectMatches == totalMatches && totalMatches > 0)
            {
                totalBonus += perfectMatchBonus;
                ShowBonusFeedback($"Perfect: +{perfectMatchBonus}");
                yield return new WaitForSeconds(0.3f);
            }

            // Level completion bonus
            totalBonus += levelCompleteBonus;
            ShowBonusFeedback($"Level Clear: +{levelCompleteBonus}");
            yield return new WaitForSeconds(0.3f);

            // Add total bonus to score
            AddScore(totalBonus);

            // Play bonus sound
            AudioManager.Instance?.PlaySound("BonusAwarded");
        }

        /// <summary>
        /// Unlock the next level and save progress.
        /// </summary>
        private void UnlockNextLevel()
        {
            int nextLevel = currentLevel + 1;
            
            // Save progress
            SaveProgress(nextLevel);
            
            Debug.Log($"Level {currentLevel} complete! Next level: {nextLevel} unlocked.");
            
            OnLevelCompleted?.Invoke();
        }

        /// <summary>
        /// Show the win screen with options to continue or replay.
        /// </summary>
        private void ShowWinScreen()
        {
            if (winScreenPanel != null)
            {
                winScreenPanel.SetActive(true);
                
                // Animate win screen appearance
                CanvasGroup canvasGroup = winScreenPanel.GetComponent<CanvasGroup>();
                if (canvasGroup != null)
                {
                    canvasGroup.alpha = 0f;
                    canvasGroup.DOFade(1f, 0.5f);
                }

                winScreenPanel.transform.localScale = Vector3.zero;
                winScreenPanel.transform.DOScale(1f, 0.5f).SetEase(Ease.OutBack);
            }
        }

        /// <summary>
        /// Proceed to the next level.
        /// </summary>
        public void NextLevel()
        {
            // Clean up current level
            gridManager?.AnimateGridDisappear(() =>
            {
                gridManager?.ClearGrid();
                
                // Hide win screen
                if (winScreenPanel != null)
                    winScreenPanel.SetActive(false);

                // Hide definition
                if (definitionText != null)
                    definitionText.gameObject.SetActive(false);

                // Start next level
                isPaused = false;
                StartLevel(currentLevel + 1);
            });
        }

        /// <summary>
        /// Replay the current level.
        /// </summary>
        public void ReplayLevel()
        {
            gridManager?.AnimateGridDisappear(() =>
            {
                gridManager?.ClearGrid();
                
                if (winScreenPanel != null)
                    winScreenPanel.SetActive(false);

                if (definitionText != null)
                    definitionText.gameObject.SetActive(false);

                isPaused = false;
                StartLevel(currentLevel);
            });
        }

        /// <summary>
        /// Add points to the score.
        /// </summary>
        private void AddScore(int points)
        {
            currentScore += points;
            OnScoreChanged?.Invoke(currentScore);
            UpdateScoreUI();
        }

        /// <summary>
        /// Update the timer each frame.
        /// </summary>
        private void UpdateTimer()
        {
            remainingTime -= Time.deltaTime;
            
            if (remainingTime <= 0)
            {
                remainingTime = 0;
                HandleTimeUp();
            }
        }

        /// <summary>
        /// Handle time running out.
        /// </summary>
        private void HandleTimeUp()
        {
            ChangeState(GameState.GameOver);
            gridManager?.SetGridInteractable(false);
            
            OnGameOver?.Invoke();
            
            // Show game over screen
            AudioManager.Instance?.PlaySound("GameOver");
        }

        /// <summary>
        /// Change the current game state.
        /// </summary>
        private void ChangeState(GameState newState)
        {
            currentState = newState;
            OnGameStateChanged?.Invoke(newState);
        }

        /// <summary>
        /// Save game progress.
        /// </summary>
        private void SaveProgress(int unlockedLevel)
        {
            PlayerPrefs.SetInt("FluencyDrive_MaxLevel", Mathf.Max(unlockedLevel, PlayerPrefs.GetInt("FluencyDrive_MaxLevel", 1)));
            PlayerPrefs.SetInt("FluencyDrive_HighScore", Mathf.Max(currentScore, PlayerPrefs.GetInt("FluencyDrive_HighScore", 0)));
            PlayerPrefs.Save();
        }

        /// <summary>
        /// Load saved progress.
        /// </summary>
        private void LoadProgress()
        {
            currentLevel = PlayerPrefs.GetInt("FluencyDrive_MaxLevel", 1);
        }

        // ═══════════════════════════════════════════════════════
        // UI UPDATE METHODS
        // ═══════════════════════════════════════════════════════

        private void UpdateUI()
        {
            UpdateScoreUI();
            UpdateTimerUI();
        }

        private void UpdateLevelUI()
        {
            if (levelText != null)
                levelText.text = $"Level {currentLevel}";

            if (wordDisplayText != null && wordManager != null)
                wordDisplayText.text = wordManager.GetDisplayWord();

            if (progressBar != null)
                progressBar.value = 0f;
        }

        private void UpdateScoreUI()
        {
            if (scoreText != null)
                scoreText.text = $"Score: {currentScore:N0}";
        }

        private void UpdateTimerUI()
        {
            if (timerText != null)
            {
                int minutes = Mathf.FloorToInt(remainingTime / 60f);
                int seconds = Mathf.FloorToInt(remainingTime % 60f);
                timerText.text = $"{minutes:00}:{seconds:00}";

                // Change color when low on time
                if (remainingTime < 30f)
                    timerText.color = Color.red;
                else
                    timerText.color = Color.white;
            }
        }

        private void ShowComboFeedback(int combo)
        {
            Debug.Log($"COMBO x{combo}!");
            // TODO: Implement combo UI feedback
        }

        private void ShowBonusFeedback(string message)
        {
            Debug.Log(message);
            // TODO: Implement bonus UI feedback
        }

        /// <summary>
        /// Pause the game.
        /// </summary>
        public void PauseGame()
        {
            if (currentState != GameState.Playing) return;
            
            isPaused = true;
            ChangeState(GameState.Paused);
            gridManager?.SetGridInteractable(false);
        }

        /// <summary>
        /// Resume the game.
        /// </summary>
        public void ResumeGame()
        {
            if (currentState != GameState.Paused) return;
            
            isPaused = false;
            ChangeState(GameState.Playing);
            gridManager?.SetGridInteractable(true);
        }

        /// <summary>
        /// Return to main menu.
        /// </summary>
        public void ReturnToMenu()
        {
            gridManager?.ClearGrid();
            ChangeState(GameState.Menu);
        }

        private void OnDestroy()
        {
            // Unsubscribe from events
            if (matchManager != null)
            {
                matchManager.OnScoreAwarded -= HandleScoreAwarded;
                matchManager.OnMatchResult -= HandleMatchResult;
                matchManager.OnAllLettersRevealed -= HandleAllLettersRevealed;
            }

            if (wordManager != null)
            {
                wordManager.OnWordCompleted -= HandleWordCompleted;
                wordManager.OnLetterProgress -= HandleLetterProgress;
            }
        }
    }

    /// <summary>
    /// Possible game states.
    /// </summary>
    public enum GameState
    {
        Menu,
        Playing,
        Paused,
        LevelComplete,
        GameOver
    }
}
