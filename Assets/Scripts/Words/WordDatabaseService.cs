using UnityEngine;
using UnityEngine.Networking;
using System;
using System.Collections;
using System.Collections.Generic;

namespace FluencyDrive
{
    /// <summary>
    /// Service for fetching word data from external APIs and managing embedded word lists.
    /// Supports Free Dictionary API and Datamuse API for unlimited word expansion.
    /// </summary>
    public class WordDatabaseService : MonoBehaviour
    {
        public static WordDatabaseService Instance { get; private set; }

        [Header("API Settings")]
        [SerializeField] private bool enableApiCalls = true;
        [SerializeField] private float apiCacheExpiryHours = 24f;

        [Header("Word Lists")]
        [SerializeField] private bool loadEmbeddedLists = true;

        // API Endpoints
        private const string FREE_DICTIONARY_API = "https://api.dictionaryapi.dev/api/v2/entries/en/";
        private const string DATAMUSE_API = "https://api.datamuse.com/words";

        // Embedded word lists by category
        private Dictionary<string, List<string>> embeddedWordLists = new Dictionary<string, List<string>>();
        
        // Cache for API responses
        private Dictionary<string, CachedWordData> wordCache = new Dictionary<string, CachedWordData>();

        // Events
        public event Action<string, WordData> OnWordDataFetched;
        public event Action<string, string> OnWordFetchFailed;
        public event Action<int> OnWordListsLoaded;

        // Statistics
        public int TotalEmbeddedWords { get; private set; }
        public int CachedWords => wordCache.Count;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
                return;
            }
        }

        private void Start()
        {
            if (loadEmbeddedLists)
            {
                LoadAllEmbeddedWordLists();
            }
        }

        #region Embedded Word Lists

        /// <summary>
        /// Load all embedded word list files from Resources/WordLists/
        /// </summary>
        public void LoadAllEmbeddedWordLists()
        {
            embeddedWordLists.Clear();
            TotalEmbeddedWords = 0;

            // Load each category's word list
            LoadWordList("common", "WordLists/common_words");
            LoadWordList("gre", "WordLists/gre_words");
            LoadWordList("medical", "WordLists/medical_words");
            LoadWordList("legal", "WordLists/legal_words");
            LoadWordList("kids", "WordLists/kids_words");
            LoadWordList("tech", "WordLists/tech_words");
            LoadWordList("phonics", "WordLists/phonics_words");
            LoadWordList("nursing", "WordLists/nursing_words");

            Debug.Log($"[WordDatabaseService] Loaded {TotalEmbeddedWords} words across {embeddedWordLists.Count} categories");
            OnWordListsLoaded?.Invoke(TotalEmbeddedWords);
        }

        /// <summary>
        /// Load a single word list file
        /// </summary>
        private void LoadWordList(string category, string resourcePath)
        {
            TextAsset wordFile = Resources.Load<TextAsset>(resourcePath);
            
            if (wordFile == null)
            {
                Debug.LogWarning($"[WordDatabaseService] Word list not found: {resourcePath}");
                return;
            }

            List<string> words = new List<string>();
            string[] lines = wordFile.text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (string line in lines)
            {
                string word = line.Trim().ToUpper();
                if (!string.IsNullOrEmpty(word) && word.Length >= 2)
                {
                    words.Add(word);
                }
            }

            embeddedWordLists[category] = words;
            TotalEmbeddedWords += words.Count;

            Debug.Log($"[WordDatabaseService] Loaded {words.Count} words for category: {category}");
        }

        /// <summary>
        /// Get all words from a specific category
        /// </summary>
        public List<string> GetWordsForCategory(string category)
        {
            category = category.ToLower();
            if (embeddedWordLists.TryGetValue(category, out List<string> words))
            {
                return new List<string>(words);
            }
            return new List<string>();
        }

        /// <summary>
        /// Get random words from a category
        /// </summary>
        public List<string> GetRandomWords(string category, int count)
        {
            List<string> categoryWords = GetWordsForCategory(category);
            if (categoryWords.Count == 0) return new List<string>();

            ShuffleList(categoryWords);
            return categoryWords.GetRange(0, Mathf.Min(count, categoryWords.Count));
        }

        /// <summary>
        /// Get a random word from any category, filtered by length
        /// </summary>
        public string GetRandomWord(int minLength = 3, int maxLength = 12)
        {
            List<string> allWords = new List<string>();
            foreach (var kvp in embeddedWordLists)
            {
                foreach (string word in kvp.Value)
                {
                    if (word.Length >= minLength && word.Length <= maxLength)
                    {
                        allWords.Add(word);
                    }
                }
            }

            if (allWords.Count == 0) return "FLUENT";
            return allWords[UnityEngine.Random.Range(0, allWords.Count)];
        }

        /// <summary>
        /// Get words by difficulty (based on length and category)
        /// </summary>
        public List<string> GetWordsByDifficulty(int difficulty)
        {
            List<string> result = new List<string>();
            
            // Difficulty 1: kids, phonics (3-5 letters)
            // Difficulty 2: common (4-6 letters)
            // Difficulty 3: common, tech (5-8 letters)
            // Difficulty 4: gre, legal (6-10 letters)
            // Difficulty 5: gre, medical, legal, nursing (8+ letters)

            int minLength = 3 + difficulty;
            int maxLength = 5 + difficulty * 2;

            string[] categories;
            switch (difficulty)
            {
                case 1:
                    categories = new[] { "kids", "phonics" };
                    minLength = 3; maxLength = 5;
                    break;
                case 2:
                    categories = new[] { "common", "kids" };
                    minLength = 4; maxLength = 6;
                    break;
                case 3:
                    categories = new[] { "common", "tech" };
                    minLength = 5; maxLength = 8;
                    break;
                case 4:
                    categories = new[] { "gre", "legal", "tech" };
                    minLength = 6; maxLength = 10;
                    break;
                case 5:
                default:
                    categories = new[] { "gre", "medical", "legal", "nursing" };
                    minLength = 7; maxLength = 15;
                    break;
            }

            foreach (string category in categories)
            {
                if (embeddedWordLists.TryGetValue(category, out List<string> words))
                {
                    foreach (string word in words)
                    {
                        if (word.Length >= minLength && word.Length <= maxLength)
                        {
                            result.Add(word);
                        }
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Search for words matching a pattern (for word games)
        /// </summary>
        public List<string> SearchWords(string pattern)
        {
            List<string> results = new List<string>();
            pattern = pattern.ToUpper();

            foreach (var kvp in embeddedWordLists)
            {
                foreach (string word in kvp.Value)
                {
                    if (MatchesPattern(word, pattern))
                    {
                        results.Add(word);
                    }
                }
            }

            return results;
        }

        private bool MatchesPattern(string word, string pattern)
        {
            if (pattern.Length != word.Length) return false;

            for (int i = 0; i < pattern.Length; i++)
            {
                if (pattern[i] != '_' && pattern[i] != word[i])
                    return false;
            }
            return true;
        }

        #endregion

        #region API Integration

        /// <summary>
        /// Fetch word data from Free Dictionary API
        /// </summary>
        public void FetchWordData(string word, Action<WordData> onSuccess, Action<string> onError = null)
        {
            if (!enableApiCalls)
            {
                onError?.Invoke("API calls disabled");
                return;
            }

            // Check cache first
            string cacheKey = word.ToLower();
            if (wordCache.TryGetValue(cacheKey, out CachedWordData cached))
            {
                if (!cached.IsExpired(apiCacheExpiryHours))
                {
                    onSuccess?.Invoke(cached.data);
                    return;
                }
            }

            StartCoroutine(FetchWordDataCoroutine(word, onSuccess, onError));
        }

        private IEnumerator FetchWordDataCoroutine(string word, Action<WordData> onSuccess, Action<string> onError)
        {
            string url = FREE_DICTIONARY_API + UnityWebRequest.EscapeURL(word.ToLower());

            using (UnityWebRequest request = UnityWebRequest.Get(url))
            {
                request.timeout = 10;
                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    try
                    {
                        WordData data = ParseDictionaryResponse(word, request.downloadHandler.text);
                        
                        // Cache the result
                        wordCache[word.ToLower()] = new CachedWordData
                        {
                            data = data,
                            fetchTime = DateTime.Now
                        };

                        onSuccess?.Invoke(data);
                        OnWordDataFetched?.Invoke(word, data);
                    }
                    catch (Exception e)
                    {
                        string error = $"Parse error for '{word}': {e.Message}";
                        onError?.Invoke(error);
                        OnWordFetchFailed?.Invoke(word, error);
                    }
                }
                else
                {
                    string error = $"API error for '{word}': {request.error}";
                    onError?.Invoke(error);
                    OnWordFetchFailed?.Invoke(word, error);
                }
            }
        }

        private WordData ParseDictionaryResponse(string word, string json)
        {
            // Parse Free Dictionary API response
            // Response format: [{ word, phonetic, phonetics, meanings, ... }]
            
            WordData data = new WordData
            {
                word = word.ToUpper(),
                category = "General",
                difficulty = CalculateDifficulty(word)
            };

            try
            {
                // Simple JSON parsing (Unity's JsonUtility doesn't handle arrays at root)
                // Extract phonetic
                int phoneticIndex = json.IndexOf("\"phonetic\":");
                if (phoneticIndex >= 0)
                {
                    int start = json.IndexOf('"', phoneticIndex + 11) + 1;
                    int end = json.IndexOf('"', start);
                    if (start > 0 && end > start)
                    {
                        data.pronunciation = json.Substring(start, end - start);
                    }
                }

                // Extract first definition
                int defIndex = json.IndexOf("\"definition\":");
                if (defIndex >= 0)
                {
                    int start = json.IndexOf('"', defIndex + 13) + 1;
                    int end = json.IndexOf('"', start);
                    if (start > 0 && end > start)
                    {
                        data.definition = json.Substring(start, end - start);
                    }
                }

                // Extract part of speech for category
                int posIndex = json.IndexOf("\"partOfSpeech\":");
                if (posIndex >= 0)
                {
                    int start = json.IndexOf('"', posIndex + 15) + 1;
                    int end = json.IndexOf('"', start);
                    if (start > 0 && end > start)
                    {
                        data.category = CapitalizeFirst(json.Substring(start, end - start));
                    }
                }

                // Extract example if available
                int exampleIndex = json.IndexOf("\"example\":");
                if (exampleIndex >= 0)
                {
                    int start = json.IndexOf('"', exampleIndex + 10) + 1;
                    int end = json.IndexOf('"', start);
                    if (start > 0 && end > start)
                    {
                        data.examples = new[] { json.Substring(start, end - start) };
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[WordDatabaseService] Error parsing response for {word}: {e.Message}");
            }

            // Fallback definition
            if (string.IsNullOrEmpty(data.definition))
            {
                data.definition = $"Definition for {word}";
            }

            return data;
        }

        /// <summary>
        /// Fetch related words using Datamuse API
        /// </summary>
        public void FetchRelatedWords(string word, RelationType type, Action<List<string>> onSuccess, Action<string> onError = null)
        {
            if (!enableApiCalls)
            {
                onError?.Invoke("API calls disabled");
                return;
            }

            StartCoroutine(FetchRelatedWordsCoroutine(word, type, onSuccess, onError));
        }

        private IEnumerator FetchRelatedWordsCoroutine(string word, RelationType type, Action<List<string>> onSuccess, Action<string> onError)
        {
            string param;
            switch (type)
            {
                case RelationType.Synonym:
                    param = "rel_syn";
                    break;
                case RelationType.Antonym:
                    param = "rel_ant";
                    break;
                case RelationType.Rhyme:
                    param = "rel_rhy";
                    break;
                case RelationType.SoundsLike:
                    param = "sl";
                    break;
                case RelationType.SpelledLike:
                    param = "sp";
                    break;
                default:
                    param = "ml"; // means like
                    break;
            }

            string url = $"{DATAMUSE_API}?{param}={UnityWebRequest.EscapeURL(word)}&max=20";

            using (UnityWebRequest request = UnityWebRequest.Get(url))
            {
                request.timeout = 10;
                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    List<string> words = ParseDatamuseResponse(request.downloadHandler.text);
                    onSuccess?.Invoke(words);
                }
                else
                {
                    onError?.Invoke(request.error);
                }
            }
        }

        /// <summary>
        /// Fetch words by topic using Datamuse API
        /// </summary>
        public void FetchWordsByTopic(string topic, Action<List<string>> onSuccess, Action<string> onError = null)
        {
            if (!enableApiCalls)
            {
                onError?.Invoke("API calls disabled");
                return;
            }

            StartCoroutine(FetchWordsByTopicCoroutine(topic, onSuccess, onError));
        }

        private IEnumerator FetchWordsByTopicCoroutine(string topic, Action<List<string>> onSuccess, Action<string> onError)
        {
            string url = $"{DATAMUSE_API}?topics={UnityWebRequest.EscapeURL(topic)}&max=100";

            using (UnityWebRequest request = UnityWebRequest.Get(url))
            {
                request.timeout = 10;
                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    List<string> words = ParseDatamuseResponse(request.downloadHandler.text);
                    onSuccess?.Invoke(words);
                }
                else
                {
                    onError?.Invoke(request.error);
                }
            }
        }

        private List<string> ParseDatamuseResponse(string json)
        {
            List<string> words = new List<string>();

            // Datamuse returns: [{"word":"...","score":...}, ...]
            int index = 0;
            while ((index = json.IndexOf("\"word\":\"", index)) >= 0)
            {
                int start = index + 8;
                int end = json.IndexOf('"', start);
                if (end > start)
                {
                    string word = json.Substring(start, end - start).ToUpper();
                    if (!string.IsNullOrEmpty(word) && word.Length >= 2)
                    {
                        words.Add(word);
                    }
                }
                index = end + 1;
            }

            return words;
        }

        #endregion

        #region Utility Methods

        private int CalculateDifficulty(string word)
        {
            int length = word.Length;
            if (length <= 4) return 1;
            if (length <= 6) return 2;
            if (length <= 8) return 3;
            if (length <= 10) return 4;
            return 5;
        }

        private string CapitalizeFirst(string s)
        {
            if (string.IsNullOrEmpty(s)) return s;
            return char.ToUpper(s[0]) + s.Substring(1);
        }

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

        /// <summary>
        /// Clear the API cache
        /// </summary>
        public void ClearCache()
        {
            wordCache.Clear();
        }

        /// <summary>
        /// Get all available categories
        /// </summary>
        public List<string> GetAvailableCategories()
        {
            return new List<string>(embeddedWordLists.Keys);
        }

        /// <summary>
        /// Get statistics about loaded words
        /// </summary>
        public Dictionary<string, int> GetWordCountByCategory()
        {
            Dictionary<string, int> counts = new Dictionary<string, int>();
            foreach (var kvp in embeddedWordLists)
            {
                counts[kvp.Key] = kvp.Value.Count;
            }
            return counts;
        }

        #endregion
    }

    /// <summary>
    /// Types of word relationships for Datamuse API
    /// </summary>
    public enum RelationType
    {
        Synonym,
        Antonym,
        Rhyme,
        SoundsLike,
        SpelledLike,
        MeansLike
    }

    /// <summary>
    /// Cached word data with timestamp
    /// </summary>
    [Serializable]
    public class CachedWordData
    {
        public WordData data;
        public DateTime fetchTime;

        public bool IsExpired(float expiryHours)
        {
            return (DateTime.Now - fetchTime).TotalHours > expiryHours;
        }
    }
}
