using UnityEngine;
using System.Collections.Generic;
using System;

namespace FluencyDrive
{
    /// <summary>
    /// Manages words, definitions, letter tracking, and vocabulary progression.
    /// Now integrates with WordDatabaseService for expanded word lists.
    /// </summary>
    public class WordManager : MonoBehaviour
    {
        public static WordManager Instance { get; private set; }

        [Header("Data")]
        [SerializeField] private TextAsset wordsJsonFile;

        [Header("Settings")]
        [SerializeField] private int wordsPerLevel = 1;
        [SerializeField] private bool useExpandedDatabase = true;
        [SerializeField] private bool fetchDefinitionsFromAPI = true;

        [Header("Category Settings")]
        [SerializeField] private string[] enabledCategories = new[] { "common", "kids", "gre" };

        // Word data
        private WordDatabase wordDatabase;
        private List<WordData> currentLevelWords = new List<WordData>();
        private WordData currentTargetWord;
        private bool isLoadingDefinition = false;

        // Letter tracking
        private Dictionary<char, int> requiredLetters = new Dictionary<char, int>();
        private Dictionary<char, int> revealedLetters = new Dictionary<char, int>();

        // Events
        public event Action<WordData> OnWordSelected;
        public event Action<char, int, int> OnLetterProgress; // letter, revealed, required
        public event Action<WordData> OnWordCompleted;
        public event Action<List<WordData>> OnAllWordsCompleted;

        public WordData CurrentWord => currentTargetWord;
        public bool AllLettersRevealed => CheckAllLettersRevealed();
        public bool IsLoadingDefinition => isLoadingDefinition;

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

            LoadWordDatabase();
        }

        private void Start()
        {
            // Subscribe to WordDatabaseService events if available
            if (WordDatabaseService.Instance != null)
            {
                WordDatabaseService.Instance.OnWordListsLoaded += OnWordListsLoaded;
                Debug.Log($"[WordManager] Connected to WordDatabaseService with {WordDatabaseService.Instance.TotalEmbeddedWords} words");
            }
        }

        private void OnWordListsLoaded(int totalWords)
        {
            Debug.Log($"[WordManager] Word lists loaded: {totalWords} words available");
        }

        /// <summary>
        /// Load word database from JSON.
        /// </summary>
        private void LoadWordDatabase()
        {
            if (wordsJsonFile != null)
            {
                wordDatabase = JsonUtility.FromJson<WordDatabase>(wordsJsonFile.text);
                Debug.Log($"Loaded {wordDatabase.words.Length} words from database");
            }
            else
            {
                // Create default database if none provided
                wordDatabase = CreateDefaultDatabase();
            }
        }

        /// <summary>
        /// Create a default word database for testing.
        /// </summary>
        private WordDatabase CreateDefaultDatabase()
        {
            return new WordDatabase
            {
                words = new WordData[]
                {
                    new WordData { word = "FLUENT", definition = "Able to express oneself easily and articulately.", difficulty = 1, category = "Language" },
                    new WordData { word = "DRIVE", definition = "An innate, biologically determined urge to attain a goal.", difficulty = 1, category = "Motivation" },
                    new WordData { word = "LEXICON", definition = "The vocabulary of a person, language, or branch of knowledge.", difficulty = 2, category = "Language" },
                    new WordData { word = "SYNTAX", definition = "The arrangement of words and phrases to create well-formed sentences.", difficulty = 2, category = "Language" },
                    new WordData { word = "COGNATE", definition = "A word having the same linguistic derivation as another.", difficulty = 3, category = "Linguistics" },
                    new WordData { word = "ETYMOLOGY", definition = "The study of the origin of words and how their meanings have changed.", difficulty = 3, category = "Linguistics" },
                    new WordData { word = "PHONEME", definition = "The smallest unit of sound that distinguishes one word from another.", difficulty = 4, category = "Phonetics" },
                    new WordData { word = "MORPHEME", definition = "The smallest grammatical unit in a language.", difficulty = 4, category = "Grammar" },
                    new WordData { word = "PRAGMATICS", definition = "The branch of linguistics dealing with language in use and context.", difficulty = 5, category = "Linguistics" },
                    new WordData { word = "IDIOMATIC", definition = "Using expressions natural to a native speaker.", difficulty = 5, category = "Language" },
                }
            };
        }

        /// <summary>
        /// Set up a new level with a word of appropriate difficulty.
        /// </summary>
        public void SetupLevel(int level)
        {
            currentLevelWords.Clear();

            // Calculate target difficulty based on level
            int targetDifficulty = Mathf.Clamp((level / 3) + 1, 1, 5);

            // Try to get words from WordDatabaseService first
            if (useExpandedDatabase && WordDatabaseService.Instance != null)
            {
                SetupLevelFromExpandedDatabase(level, targetDifficulty);
            }
            else
            {
                SetupLevelFromLocalDatabase(targetDifficulty);
            }
        }

        /// <summary>
        /// Set up level using the expanded WordDatabaseService
        /// </summary>
        private void SetupLevelFromExpandedDatabase(int level, int targetDifficulty)
        {
            var service = WordDatabaseService.Instance;
            List<string> candidateWords = service.GetWordsByDifficulty(targetDifficulty);

            if (candidateWords.Count == 0)
            {
                // Fallback to local database
                Debug.LogWarning("[WordManager] No words found in expanded database, using local");
                SetupLevelFromLocalDatabase(targetDifficulty);
                return;
            }

            // Shuffle and select a word
            ShuffleList(candidateWords);
            string selectedWord = candidateWords[0];

            // Create WordData for the selected word
            WordData wordData = new WordData
            {
                word = selectedWord.ToUpper(),
                definition = "Loading definition...",
                difficulty = targetDifficulty,
                category = GetCategoryForWord(selectedWord)
            };

            currentLevelWords.Add(wordData);
            SetCurrentWord(wordData);

            // Fetch full definition from API if enabled
            if (fetchDefinitionsFromAPI)
            {
                FetchWordDefinition(selectedWord);
            }
        }

        /// <summary>
        /// Set up level from local JSON database (fallback)
        /// </summary>
        private void SetupLevelFromLocalDatabase(int targetDifficulty)
        {
            // Find words matching the difficulty
            List<WordData> availableWords = new List<WordData>();
            foreach (WordData word in wordDatabase.words)
            {
                if (word.difficulty <= targetDifficulty)
                {
                    availableWords.Add(word);
                }
            }

            // Shuffle and select words for this level
            ShuffleList(availableWords);
            
            for (int i = 0; i < wordsPerLevel && i < availableWords.Count; i++)
            {
                currentLevelWords.Add(availableWords[i]);
            }

            if (currentLevelWords.Count > 0)
            {
                SetCurrentWord(currentLevelWords[0]);
            }
        }

        /// <summary>
        /// Fetch word definition from API
        /// </summary>
        private void FetchWordDefinition(string word)
        {
            if (WordDatabaseService.Instance == null) return;

            isLoadingDefinition = true;
            
            WordDatabaseService.Instance.FetchWordData(word, 
                (fetchedData) =>
                {
                    // Update current word with fetched data
                    if (currentTargetWord != null && currentTargetWord.word.Equals(word, StringComparison.OrdinalIgnoreCase))
                    {
                        currentTargetWord.definition = fetchedData.definition;
                        currentTargetWord.pronunciation = fetchedData.pronunciation;
                        currentTargetWord.examples = fetchedData.examples;
                        currentTargetWord.category = fetchedData.category ?? currentTargetWord.category;
                        
                        Debug.Log($"[WordManager] Definition fetched for: {word}");
                    }
                    isLoadingDefinition = false;
                },
                (error) =>
                {
                    Debug.LogWarning($"[WordManager] Failed to fetch definition for {word}: {error}");
                    // Keep the placeholder definition
                    if (currentTargetWord != null)
                    {
                        currentTargetWord.definition = $"A word meaning {word.ToLower()}.";
                    }
                    isLoadingDefinition = false;
                }
            );
        }

        /// <summary>
        /// Determine category based on word source
        /// </summary>
        private string GetCategoryForWord(string word)
        {
            if (WordDatabaseService.Instance == null) return "General";

            var categories = WordDatabaseService.Instance.GetAvailableCategories();
            foreach (string category in categories)
            {
                var words = WordDatabaseService.Instance.GetWordsForCategory(category);
                if (words.Contains(word.ToUpper()))
                {
                    return CapitalizeCategory(category);
                }
            }
            return "General";
        }

        private string CapitalizeCategory(string category)
        {
            if (string.IsNullOrEmpty(category)) return category;
            return char.ToUpper(category[0]) + category.Substring(1);
        }

        /// <summary>
        /// Set the current target word.
        /// </summary>
        private void SetCurrentWord(WordData word)
        {
            currentTargetWord = word;
            InitializeLetterTracking(word.word);
            OnWordSelected?.Invoke(word);
        }

        /// <summary>
        /// Initialize letter tracking for the current word.
        /// </summary>
        private void InitializeLetterTracking(string word)
        {
            requiredLetters.Clear();
            revealedLetters.Clear();

            foreach (char c in word.ToUpper())
            {
                if (char.IsLetter(c))
                {
                    if (requiredLetters.ContainsKey(c))
                        requiredLetters[c]++;
                    else
                        requiredLetters[c] = 1;

                    if (!revealedLetters.ContainsKey(c))
                        revealedLetters[c] = 0;
                }
            }
        }

        /// <summary>
        /// Get the letters needed for the current word (for tile assignment).
        /// </summary>
        public char[] GetLettersForGrid()
        {
            if (currentTargetWord == null)
                return new char[0];

            List<char> letters = new List<char>();
            
            foreach (char c in currentTargetWord.word.ToUpper())
            {
                if (char.IsLetter(c))
                {
                    letters.Add(c);
                }
            }

            return letters.ToArray();
        }

        /// <summary>
        /// Called when a letter is revealed through matching.
        /// </summary>
        public void OnLetterRevealed(char letter)
        {
            letter = char.ToUpper(letter);

            if (revealedLetters.ContainsKey(letter))
            {
                revealedLetters[letter]++;
                
                int revealed = revealedLetters[letter];
                int required = requiredLetters.ContainsKey(letter) ? requiredLetters[letter] : 0;
                
                OnLetterProgress?.Invoke(letter, revealed, required);

                // Check if word is complete
                if (CheckAllLettersRevealed())
                {
                    OnWordCompleted?.Invoke(currentTargetWord);
                    
                    // Check if all words for level are done
                    if (AreAllLevelWordsCompleted())
                    {
                        OnAllWordsCompleted?.Invoke(currentLevelWords);
                    }
                }
            }
        }

        /// <summary>
        /// Check if all required letters have been revealed.
        /// </summary>
        private bool CheckAllLettersRevealed()
        {
            foreach (var kvp in requiredLetters)
            {
                char letter = kvp.Key;
                int required = kvp.Value;
                int revealed = revealedLetters.ContainsKey(letter) ? revealedLetters[letter] : 0;
                
                if (revealed < required)
                    return false;
            }
            return requiredLetters.Count > 0;
        }

        /// <summary>
        /// Check if all words for the level have been completed.
        /// </summary>
        private bool AreAllLevelWordsCompleted()
        {
            // For now, we only have one word per level
            return CheckAllLettersRevealed();
        }

        /// <summary>
        /// Get the current word as a display string with unrevealed letters hidden.
        /// </summary>
        public string GetDisplayWord()
        {
            if (currentTargetWord == null)
                return "";

            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            Dictionary<char, int> tempRevealed = new Dictionary<char, int>(revealedLetters);

            foreach (char c in currentTargetWord.word.ToUpper())
            {
                if (!char.IsLetter(c))
                {
                    sb.Append(c);
                    continue;
                }

                if (tempRevealed.ContainsKey(c) && tempRevealed[c] > 0)
                {
                    sb.Append(c);
                    tempRevealed[c]--;
                }
                else
                {
                    sb.Append("_");
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// Get progress as percentage (0-1).
        /// </summary>
        public float GetProgressPercentage()
        {
            if (requiredLetters.Count == 0)
                return 0f;

            int totalRequired = 0;
            int totalRevealed = 0;

            foreach (var kvp in requiredLetters)
            {
                totalRequired += kvp.Value;
                totalRevealed += Mathf.Min(
                    revealedLetters.ContainsKey(kvp.Key) ? revealedLetters[kvp.Key] : 0,
                    kvp.Value
                );
            }

            return totalRequired > 0 ? (float)totalRevealed / totalRequired : 0f;
        }

        /// <summary>
        /// Get a random word from the database.
        /// </summary>
        public WordData GetRandomWord(int maxDifficulty = 5)
        {
            // Try expanded database first
            if (useExpandedDatabase && WordDatabaseService.Instance != null)
            {
                var words = WordDatabaseService.Instance.GetWordsByDifficulty(maxDifficulty);
                if (words.Count > 0)
                {
                    string randomWord = words[UnityEngine.Random.Range(0, words.Count)];
                    return new WordData
                    {
                        word = randomWord,
                        definition = "Loading...",
                        difficulty = maxDifficulty,
                        category = GetCategoryForWord(randomWord)
                    };
                }
            }

            // Fallback to local database
            List<WordData> eligible = new List<WordData>();
            
            foreach (WordData word in wordDatabase.words)
            {
                if (word.difficulty <= maxDifficulty)
                    eligible.Add(word);
            }

            if (eligible.Count == 0)
                return wordDatabase.words[0];

            return eligible[UnityEngine.Random.Range(0, eligible.Count)];
        }

        /// <summary>
        /// Get words from a specific category
        /// </summary>
        public List<string> GetWordsForCategory(string category, int count = 10)
        {
            if (WordDatabaseService.Instance != null)
            {
                return WordDatabaseService.Instance.GetRandomWords(category, count);
            }
            return new List<string>();
        }

        /// <summary>
        /// Get all available categories
        /// </summary>
        public List<string> GetAvailableCategories()
        {
            if (WordDatabaseService.Instance != null)
            {
                return WordDatabaseService.Instance.GetAvailableCategories();
            }
            return new List<string>();
        }

        /// <summary>
        /// Get total number of available words
        /// </summary>
        public int GetTotalWordCount()
        {
            int count = wordDatabase?.words?.Length ?? 0;
            if (WordDatabaseService.Instance != null)
            {
                count += WordDatabaseService.Instance.TotalEmbeddedWords;
            }
            return count;
        }

        /// <summary>
        /// Shuffle a list using Fisher-Yates algorithm.
        /// </summary>
        private void ShuffleList<T>(List<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                T temp = list[i];
                list[i] = list[j];
                list[j] = temp;
            }
        }

        private void OnDestroy()
        {
            if (WordDatabaseService.Instance != null)
            {
                WordDatabaseService.Instance.OnWordListsLoaded -= OnWordListsLoaded;
            }
        }
    }

    /// <summary>
    /// Container for word database.
    /// </summary>
    [Serializable]
    public class WordDatabase
    {
        public WordData[] words;
    }

    /// <summary>
    /// Data for a single word entry.
    /// </summary>
    [Serializable]
    public class WordData
    {
        public string word;
        public string definition;
        public int difficulty;
        public string category;
        public string pronunciation;
        public string[] examples;
        public string[] synonyms;
    }
}
