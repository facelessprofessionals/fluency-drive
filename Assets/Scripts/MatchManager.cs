using UnityEngine;
using System.Collections;
using System;

namespace FluencyDrive
{
    /// <summary>
    /// Handles tile matching logic - selection, validation, and match processing.
    /// </summary>
    public class MatchManager : MonoBehaviour
    {
        public static MatchManager Instance { get; private set; }

        [Header("Match Settings")]
        [SerializeField] private int tilesRequiredForMatch = 2;
        [SerializeField] private float matchCheckDelay = 0.3f;
        [SerializeField] private float mismatchResetDelay = 0.8f;

        [Header("Scoring")]
        [SerializeField] private int baseMatchScore = 100;
        [SerializeField] private int comboMultiplier = 50;
        [SerializeField] private int maxCombo = 10;

        // Currently selected tiles
        private Tile[] selectedTiles;
        private int currentSelectionCount = 0;
        private bool isProcessingMatch = false;

        // Combo tracking
        private int currentCombo = 0;
        private float lastMatchTime;
        private float comboTimeWindow = 3f;

        // Events
        public event Action<Tile, Tile, bool> OnMatchResult; // tile1, tile2, isMatch
        public event Action<int, int> OnScoreAwarded; // score, combo
        public event Action<char> OnLetterRevealed;
        public event Action OnAllLettersRevealed;

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

            selectedTiles = new Tile[tilesRequiredForMatch];
        }

        /// <summary>
        /// Called when a tile is selected by the player.
        /// </summary>
        public void OnTileSelected(Tile tile)
        {
            if (isProcessingMatch) return;
            if (tile.IsMatched) return;
            if (tile.IsSelected) 
            {
                // Deselect if clicking the same tile
                DeselectTile(tile);
                return;
            }

            // Check if we already have max selections
            if (currentSelectionCount >= tilesRequiredForMatch)
            {
                return;
            }

            // Select this tile
            SelectTile(tile);

            // Check if we have enough tiles selected for a match
            if (currentSelectionCount >= tilesRequiredForMatch)
            {
                StartCoroutine(ProcessMatchCoroutine());
            }
        }

        /// <summary>
        /// Select a tile and add it to the selection.
        /// </summary>
        private void SelectTile(Tile tile)
        {
            selectedTiles[currentSelectionCount] = tile;
            currentSelectionCount++;
            tile.Select();
        }

        /// <summary>
        /// Deselect a tile and remove it from the selection.
        /// </summary>
        private void DeselectTile(Tile tile)
        {
            for (int i = 0; i < currentSelectionCount; i++)
            {
                if (selectedTiles[i] == tile)
                {
                    // Shift remaining tiles
                    for (int j = i; j < currentSelectionCount - 1; j++)
                    {
                        selectedTiles[j] = selectedTiles[j + 1];
                    }
                    currentSelectionCount--;
                    selectedTiles[currentSelectionCount] = null;
                    break;
                }
            }
            tile.Deselect();
        }

        /// <summary>
        /// Process the match after tiles are selected.
        /// </summary>
        private IEnumerator ProcessMatchCoroutine()
        {
            isProcessingMatch = true;

            yield return new WaitForSeconds(matchCheckDelay);

            bool isMatch = CheckMatch();

            if (isMatch)
            {
                yield return HandleSuccessfulMatch();
            }
            else
            {
                yield return HandleFailedMatch();
            }

            isProcessingMatch = false;
        }

        /// <summary>
        /// Check if the selected tiles form a valid match.
        /// Tiles match if they have the same TYPE (not letter).
        /// </summary>
        private bool CheckMatch()
        {
            if (currentSelectionCount < tilesRequiredForMatch)
                return false;

            // Check if all selected tiles have the same type
            int firstType = selectedTiles[0].TileType;
            
            for (int i = 1; i < tilesRequiredForMatch; i++)
            {
                if (selectedTiles[i].TileType != firstType)
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Handle a successful match.
        /// </summary>
        private IEnumerator HandleSuccessfulMatch()
        {
            // Update combo
            UpdateCombo(true);

            // Calculate score
            int score = CalculateMatchScore();
            OnScoreAwarded?.Invoke(score, currentCombo);

            // Get the letter from the matched tiles
            char revealedLetter = selectedTiles[0].Letter;

            // Mark tiles as matched
            int matchedCount = 0;
            for (int i = 0; i < tilesRequiredForMatch; i++)
            {
                int index = i; // Capture for closure
                selectedTiles[i].SetMatched(() =>
                {
                    matchedCount++;
                });
            }

            // Wait for match animations
            yield return new WaitForSeconds(0.5f);

            // Fire match result event
            OnMatchResult?.Invoke(selectedTiles[0], selectedTiles[1], true);

            // Notify that a letter was revealed
            OnLetterRevealed?.Invoke(revealedLetter);

            // Clear selection
            ClearSelection();

            // Check if all letters are revealed
            CheckAllLettersRevealed();
        }

        /// <summary>
        /// Handle a failed match.
        /// </summary>
        private IEnumerator HandleFailedMatch()
        {
            // Reset combo
            UpdateCombo(false);

            // Shake tiles to indicate invalid match
            for (int i = 0; i < tilesRequiredForMatch; i++)
            {
                selectedTiles[i].ShakeInvalid();
            }

            // Fire match result event
            OnMatchResult?.Invoke(selectedTiles[0], selectedTiles[1], false);

            yield return new WaitForSeconds(mismatchResetDelay);

            // Deselect all tiles
            for (int i = 0; i < tilesRequiredForMatch; i++)
            {
                selectedTiles[i].Deselect();
            }

            // Clear selection
            ClearSelection();
        }

        /// <summary>
        /// Clear the current tile selection.
        /// </summary>
        private void ClearSelection()
        {
            for (int i = 0; i < selectedTiles.Length; i++)
            {
                selectedTiles[i] = null;
            }
            currentSelectionCount = 0;
        }

        /// <summary>
        /// Deselect all currently selected tiles.
        /// </summary>
        public void DeselectAll()
        {
            for (int i = 0; i < currentSelectionCount; i++)
            {
                if (selectedTiles[i] != null)
                {
                    selectedTiles[i].Deselect();
                }
            }
            ClearSelection();
        }

        /// <summary>
        /// Update the combo counter.
        /// </summary>
        private void UpdateCombo(bool wasSuccessful)
        {
            if (wasSuccessful)
            {
                // Check if within combo time window
                if (Time.time - lastMatchTime <= comboTimeWindow)
                {
                    currentCombo = Mathf.Min(currentCombo + 1, maxCombo);
                }
                else
                {
                    currentCombo = 1;
                }
                lastMatchTime = Time.time;
            }
            else
            {
                currentCombo = 0;
            }
        }

        /// <summary>
        /// Calculate score for the current match.
        /// </summary>
        private int CalculateMatchScore()
        {
            int score = baseMatchScore + (currentCombo * comboMultiplier);
            return score;
        }

        /// <summary>
        /// Check if all tiles have been matched (all letters revealed).
        /// </summary>
        private void CheckAllLettersRevealed()
        {
            GridManager gridManager = FindObjectOfType<GridManager>();
            if (gridManager != null && gridManager.AreAllTilesMatched())
            {
                OnAllLettersRevealed?.Invoke();
            }
        }

        /// <summary>
        /// Reset the match manager state for a new level.
        /// </summary>
        public void ResetForNewLevel()
        {
            ClearSelection();
            currentCombo = 0;
            isProcessingMatch = false;
        }

        /// <summary>
        /// Get current combo count.
        /// </summary>
        public int GetCurrentCombo()
        {
            return currentCombo;
        }
    }
}
