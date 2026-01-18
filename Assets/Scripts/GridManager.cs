using UnityEngine;
using System.Collections.Generic;
using DG.Tweening;

namespace FluencyDrive
{
    /// <summary>
    /// Manages the tile grid - spawning, layout, and tile access.
    /// </summary>
    public class GridManager : MonoBehaviour
    {
        [Header("Grid Settings")]
        [SerializeField] private int gridWidth = 6;
        [SerializeField] private int gridHeight = 6;
        [SerializeField] private float tileSize = 100f;
        [SerializeField] private float tileSpacing = 10f;

        [Header("References")]
        [SerializeField] private GameObject tilePrefab;
        [SerializeField] private Transform gridContainer;
        [SerializeField] private Sprite[] tileSprites;

        [Header("Tile Types")]
        [SerializeField] private int numberOfTileTypes = 6;

        // The grid of tiles
        private Tile[,] grid;
        private List<Tile> allTiles = new List<Tile>();

        public int Width => gridWidth;
        public int Height => gridHeight;
        public List<Tile> AllTiles => allTiles;

        /// <summary>
        /// Initialize and create the grid with assigned letters.
        /// </summary>
        public void InitializeGrid(char[] lettersToAssign)
        {
            ClearGrid();
            
            grid = new Tile[gridWidth, gridHeight];
            allTiles.Clear();

            // Calculate starting position to center the grid
            float startX = -(gridWidth - 1) * (tileSize + tileSpacing) / 2f;
            float startY = (gridHeight - 1) * (tileSize + tileSpacing) / 2f;

            // Create letter assignment queue (shuffled)
            Queue<char> letterQueue = CreateLetterQueue(lettersToAssign);

            // Spawn tiles
            for (int y = 0; y < gridHeight; y++)
            {
                for (int x = 0; x < gridWidth; x++)
                {
                    Vector3 position = new Vector3(
                        startX + x * (tileSize + tileSpacing),
                        startY - y * (tileSize + tileSpacing),
                        0
                    );

                    Tile tile = SpawnTile(position, x, y, letterQueue);
                    grid[x, y] = tile;
                    allTiles.Add(tile);
                }
            }

            // Ensure no initial matches (optional - can be enabled for puzzle mode)
            // EnsureNoInitialMatches();

            // Animate tiles appearing
            AnimateGridAppear();
        }

        /// <summary>
        /// Create a shuffled queue of letters to distribute across pairs of tiles.
        /// </summary>
        private Queue<char> CreateLetterQueue(char[] letters)
        {
            int totalTiles = gridWidth * gridHeight;
            int pairsNeeded = totalTiles / 2;
            
            List<char> letterList = new List<char>();

            // Distribute word letters across pairs
            for (int i = 0; i < pairsNeeded; i++)
            {
                char letter = letters[i % letters.Length];
                letterList.Add(letter);
                letterList.Add(letter); // Add twice for matching pair
            }

            // Shuffle the letter list
            ShuffleList(letterList);

            return new Queue<char>(letterList);
        }

        /// <summary>
        /// Spawn a single tile at the given position.
        /// </summary>
        private Tile SpawnTile(Vector3 position, int x, int y, Queue<char> letterQueue)
        {
            GameObject tileObj = Instantiate(tilePrefab, gridContainer);
            tileObj.transform.localPosition = position;
            tileObj.name = $"Tile_{x}_{y}";

            Tile tile = tileObj.GetComponent<Tile>();
            
            // Assign random tile type
            int tileType = Random.Range(0, numberOfTileTypes);
            
            // Get letter from queue
            char letter = letterQueue.Count > 0 ? letterQueue.Dequeue() : ' ';
            
            // Get sprite for this tile type
            Sprite icon = tileSprites != null && tileType < tileSprites.Length 
                ? tileSprites[tileType] 
                : null;

            tile.Initialize(tileType, letter, new Vector2Int(x, y), icon);

            // Subscribe to click events
            tile.OnTileClicked += OnTileClicked;

            return tile;
        }

        /// <summary>
        /// Handle tile click - forward to MatchManager.
        /// </summary>
        private void OnTileClicked(Tile tile)
        {
            MatchManager.Instance?.OnTileSelected(tile);
        }

        /// <summary>
        /// Animate all tiles appearing with a cascade effect.
        /// </summary>
        private void AnimateGridAppear()
        {
            foreach (Tile tile in allTiles)
            {
                tile.transform.localScale = Vector3.zero;
            }

            float delay = 0f;
            for (int y = 0; y < gridHeight; y++)
            {
                for (int x = 0; x < gridWidth; x++)
                {
                    Tile tile = grid[x, y];
                    tile.transform.DOScale(1f, 0.3f)
                        .SetDelay(delay)
                        .SetEase(Ease.OutBack);
                    
                    delay += 0.02f;
                }
            }
        }

        /// <summary>
        /// Get tile at specific grid position.
        /// </summary>
        public Tile GetTile(int x, int y)
        {
            if (x < 0 || x >= gridWidth || y < 0 || y >= gridHeight)
                return null;
            
            return grid[x, y];
        }

        /// <summary>
        /// Get tile at specific grid position.
        /// </summary>
        public Tile GetTile(Vector2Int position)
        {
            return GetTile(position.x, position.y);
        }

        /// <summary>
        /// Get all tiles that haven't been matched yet.
        /// </summary>
        public List<Tile> GetUnmatchedTiles()
        {
            List<Tile> unmatched = new List<Tile>();
            foreach (Tile tile in allTiles)
            {
                if (!tile.IsMatched)
                    unmatched.Add(tile);
            }
            return unmatched;
        }

        /// <summary>
        /// Get all tiles with a specific letter that are matched.
        /// </summary>
        public List<Tile> GetMatchedTilesWithLetter(char letter)
        {
            List<Tile> result = new List<Tile>();
            foreach (Tile tile in allTiles)
            {
                if (tile.IsMatched && tile.Letter == letter)
                    result.Add(tile);
            }
            return result;
        }

        /// <summary>
        /// Check if all tiles are matched.
        /// </summary>
        public bool AreAllTilesMatched()
        {
            foreach (Tile tile in allTiles)
            {
                if (!tile.IsMatched)
                    return false;
            }
            return true;
        }

        /// <summary>
        /// Set all tiles interactable state.
        /// </summary>
        public void SetGridInteractable(bool interactable)
        {
            foreach (Tile tile in allTiles)
            {
                tile.SetInteractable(interactable);
            }
        }

        /// <summary>
        /// Clear the grid and destroy all tiles.
        /// </summary>
        public void ClearGrid()
        {
            foreach (Tile tile in allTiles)
            {
                if (tile != null)
                {
                    tile.OnTileClicked -= OnTileClicked;
                    Destroy(tile.gameObject);
                }
            }
            
            allTiles.Clear();
            grid = null;
        }

        /// <summary>
        /// Shuffle a list using Fisher-Yates algorithm.
        /// </summary>
        private void ShuffleList<T>(List<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                T temp = list[i];
                list[i] = list[j];
                list[j] = temp;
            }
        }

        /// <summary>
        /// Animate grid disappearing (for level transition).
        /// </summary>
        public void AnimateGridDisappear(System.Action onComplete = null)
        {
            float delay = 0f;
            int completedCount = 0;
            int totalTiles = allTiles.Count;

            foreach (Tile tile in allTiles)
            {
                tile.transform.DOScale(0f, 0.2f)
                    .SetDelay(delay)
                    .SetEase(Ease.InBack)
                    .OnComplete(() =>
                    {
                        completedCount++;
                        if (completedCount >= totalTiles)
                        {
                            onComplete?.Invoke();
                        }
                    });
                
                delay += 0.01f;
            }
        }

        private void OnDestroy()
        {
            ClearGrid();
        }
    }
}
