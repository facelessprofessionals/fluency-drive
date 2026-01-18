using UnityEngine;

namespace FluencyDrive
{
    /// <summary>
    /// ScriptableObject for game configuration settings.
    /// Create via: Assets > Create > Fluency Drive > Game Settings
    /// </summary>
    [CreateAssetMenu(fileName = "GameSettings", menuName = "Fluency Drive/Game Settings")]
    public class GameSettings : ScriptableObject
    {
        [Header("Grid Settings")]
        [Tooltip("Number of columns in the tile grid")]
        [Range(4, 10)]
        public int gridWidth = 6;
        
        [Tooltip("Number of rows in the tile grid")]
        [Range(4, 10)]
        public int gridHeight = 6;
        
        [Tooltip("Size of each tile in pixels")]
        public float tileSize = 100f;
        
        [Tooltip("Space between tiles in pixels")]
        public float tileSpacing = 10f;
        
        [Tooltip("Number of different tile types/colors")]
        [Range(4, 8)]
        public int numberOfTileTypes = 6;

        [Header("Gameplay Settings")]
        [Tooltip("Time limit per level in seconds")]
        public float levelTimeLimit = 120f;
        
        [Tooltip("Number of tiles required to make a match")]
        [Range(2, 4)]
        public int tilesRequiredForMatch = 2;
        
        [Tooltip("Delay before checking match validity")]
        public float matchCheckDelay = 0.3f;
        
        [Tooltip("Delay before resetting mismatched tiles")]
        public float mismatchResetDelay = 0.8f;
        
        [Tooltip("Time window for maintaining combo")]
        public float comboTimeWindow = 3f;

        [Header("Scoring")]
        [Tooltip("Points awarded for each successful match")]
        public int baseMatchScore = 100;
        
        [Tooltip("Additional points per combo level")]
        public int comboMultiplier = 50;
        
        [Tooltip("Maximum combo level")]
        public int maxCombo = 10;
        
        [Tooltip("Bonus points based on remaining time")]
        public int timeBonus = 500;
        
        [Tooltip("Bonus for completing level without mistakes")]
        public int perfectMatchBonus = 1000;
        
        [Tooltip("Bonus for completing any level")]
        public int levelCompleteBonus = 250;

        [Header("Difficulty Scaling")]
        [Tooltip("Levels before increasing difficulty")]
        public int levelsPerDifficultyIncrease = 3;
        
        [Tooltip("Time reduction per difficulty level (seconds)")]
        public float timeReductionPerLevel = 5f;
        
        [Tooltip("Minimum time limit")]
        public float minimumTimeLimit = 60f;

        [Header("Animation Settings")]
        [Tooltip("Duration of tile selection animation")]
        public float selectAnimationDuration = 0.2f;
        
        [Tooltip("Scale multiplier when tile is selected")]
        public float selectScaleMultiplier = 1.15f;
        
        [Tooltip("Duration of screen transitions")]
        public float screenTransitionDuration = 0.3f;
        
        [Tooltip("Duration for score count-up animations")]
        public float countUpDuration = 1f;

        [Header("Visual Settings")]
        [Tooltip("Colors for different tile types")]
        public Color[] tileColors = new Color[]
        {
            new Color(0.94f, 0.35f, 0.35f), // Red
            new Color(0.35f, 0.70f, 0.94f), // Blue
            new Color(0.35f, 0.94f, 0.45f), // Green
            new Color(0.94f, 0.85f, 0.35f), // Yellow
            new Color(0.75f, 0.35f, 0.94f), // Purple
            new Color(0.94f, 0.55f, 0.35f), // Orange
        };

        [Tooltip("Color when tile is selected")]
        public Color selectedColor = new Color(0.9f, 0.7f, 0.2f);
        
        [Tooltip("Color when tile is matched")]
        public Color matchedColor = new Color(0.4f, 0.9f, 0.4f);
        
        [Tooltip("Color on hover")]
        public Color hoverColor = new Color(0.5f, 0.7f, 0.95f);

        [Header("Audio Settings")]
        [Tooltip("Master volume for sound effects")]
        [Range(0f, 1f)]
        public float sfxVolume = 1f;
        
        [Tooltip("Master volume for background music")]
        [Range(0f, 1f)]
        public float musicVolume = 0.5f;

        /// <summary>
        /// Get the time limit for a specific level (with difficulty scaling).
        /// </summary>
        public float GetTimeLimitForLevel(int level)
        {
            int difficultyLevel = (level - 1) / levelsPerDifficultyIncrease;
            float adjustedTime = levelTimeLimit - (difficultyLevel * timeReductionPerLevel);
            return Mathf.Max(adjustedTime, minimumTimeLimit);
        }

        /// <summary>
        /// Get the word difficulty for a specific level.
        /// </summary>
        public int GetWordDifficultyForLevel(int level)
        {
            return Mathf.Clamp((level - 1) / levelsPerDifficultyIncrease + 1, 1, 5);
        }

        /// <summary>
        /// Get color for a tile type.
        /// </summary>
        public Color GetTileColor(int tileType)
        {
            if (tileColors != null && tileType < tileColors.Length)
                return tileColors[tileType];
            return Color.gray;
        }

        /// <summary>
        /// Calculate star rating based on performance.
        /// </summary>
        public int CalculateStars(float timeRatio, bool isPerfect, int comboMax)
        {
            int stars = 1; // Base star for completion

            if (timeRatio > 0.5f) stars++; // Finished with >50% time
            if (isPerfect) stars++; // No mistakes
            
            return Mathf.Min(stars, 3);
        }
    }
}
