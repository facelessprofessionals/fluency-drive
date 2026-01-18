#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using FluencyDrive;

namespace FluencyDrive.Editor
{
    /// <summary>
    /// Editor utility to auto-generate Fluency Drive prefabs and scene setup.
    /// Access via Unity Menu: Tools > Fluency Drive > Setup
    /// </summary>
    public class FluencyDriveSetup : EditorWindow
    {
        private static Color primaryColor = new Color(0.2f, 0.6f, 0.9f);
        private static Color secondaryColor = new Color(0.9f, 0.7f, 0.2f);
        private static Color backgroundColor = new Color(0.15f, 0.15f, 0.2f);

        [MenuItem("Tools/Fluency Drive/Setup Window")]
        public static void ShowWindow()
        {
            GetWindow<FluencyDriveSetup>("Fluency Drive Setup");
        }

        [MenuItem("Tools/Fluency Drive/Create Tile Prefab")]
        public static void CreateTilePrefab()
        {
            // Create root object
            GameObject tileRoot = new GameObject("TilePrefab");
            
            // Add RectTransform
            RectTransform rootRect = tileRoot.AddComponent<RectTransform>();
            rootRect.sizeDelta = new Vector2(100, 100);

            // Add Background Image
            Image bgImage = tileRoot.AddComponent<Image>();
            bgImage.color = primaryColor;
            bgImage.raycastTarget = true;

            // Add Button for click detection
            Button button = tileRoot.AddComponent<Button>();
            button.transition = Selectable.Transition.None;

            // Create Icon child
            GameObject iconObj = new GameObject("Icon");
            iconObj.transform.SetParent(tileRoot.transform);
            RectTransform iconRect = iconObj.AddComponent<RectTransform>();
            iconRect.anchorMin = Vector2.zero;
            iconRect.anchorMax = Vector2.one;
            iconRect.offsetMin = new Vector2(15, 15);
            iconRect.offsetMax = new Vector2(-15, -15);
            Image iconImage = iconObj.AddComponent<Image>();
            iconImage.color = Color.white;
            iconImage.raycastTarget = false;

            // Create Letter Text child
            GameObject letterObj = new GameObject("LetterText");
            letterObj.transform.SetParent(tileRoot.transform);
            RectTransform letterRect = letterObj.AddComponent<RectTransform>();
            letterRect.anchorMin = Vector2.zero;
            letterRect.anchorMax = Vector2.one;
            letterRect.offsetMin = Vector2.zero;
            letterRect.offsetMax = Vector2.zero;
            Text letterText = letterObj.AddComponent<Text>();
            letterText.text = "A";
            letterText.fontSize = 48;
            letterText.fontStyle = FontStyle.Bold;
            letterText.alignment = TextAnchor.MiddleCenter;
            letterText.color = Color.white;
            letterText.raycastTarget = false;
            letterObj.SetActive(false); // Hidden by default

            // Create particle system child (optional)
            GameObject particleObj = new GameObject("MatchParticles");
            particleObj.transform.SetParent(tileRoot.transform);
            particleObj.transform.localPosition = Vector3.zero;
            ParticleSystem ps = particleObj.AddComponent<ParticleSystem>();
            
            // Configure particle system
            var main = ps.main;
            main.startLifetime = 0.5f;
            main.startSpeed = 100f;
            main.startSize = 10f;
            main.startColor = secondaryColor;
            main.maxParticles = 20;
            main.playOnAwake = false;
            
            var emission = ps.emission;
            emission.rateOverTime = 0;
            emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 15) });
            
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 30f;

            // Add Tile script
            Tile tileScript = tileRoot.AddComponent<Tile>();

            // Save as prefab
            string prefabPath = "Assets/Prefabs";
            if (!AssetDatabase.IsValidFolder(prefabPath))
            {
                AssetDatabase.CreateFolder("Assets", "Prefabs");
            }

            string fullPath = $"{prefabPath}/TilePrefab.prefab";
            
            // Check if prefab already exists
            if (AssetDatabase.LoadAssetAtPath<GameObject>(fullPath) != null)
            {
                if (!EditorUtility.DisplayDialog("Prefab Exists", 
                    "TilePrefab already exists. Overwrite?", "Yes", "No"))
                {
                    DestroyImmediate(tileRoot);
                    return;
                }
            }

            PrefabUtility.SaveAsPrefabAsset(tileRoot, fullPath);
            DestroyImmediate(tileRoot);

            Debug.Log($"✅ Tile Prefab created at {fullPath}");
            EditorUtility.DisplayDialog("Success", "Tile Prefab created successfully!\n\nDon't forget to assign the component references in the Inspector.", "OK");

            // Select the new prefab
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<GameObject>(fullPath);
        }

        [MenuItem("Tools/Fluency Drive/Setup Scene")]
        public static void SetupScene()
        {
            // Check for existing setup
            if (Object.FindObjectOfType<GameManager>() != null)
            {
                if (!EditorUtility.DisplayDialog("Scene Already Setup", 
                    "GameManager already exists in the scene. Setup again?", "Yes", "No"))
                {
                    return;
                }
            }

            // Create Managers
            CreateManagers();

            // Create Canvas and UI
            Canvas canvas = CreateMainCanvas();
            CreateUIStructure(canvas);

            Debug.Log("✅ Fluency Drive scene setup complete!");
            EditorUtility.DisplayDialog("Success", 
                "Scene setup complete!\n\n" +
                "Next steps:\n" +
                "1. Assign prefab references in GridManager\n" +
                "2. Assign UI references in UIManager\n" +
                "3. Import DOTween from Asset Store\n" +
                "4. Press Play to test!", "OK");
        }

        private static void CreateManagers()
        {
            // Game Managers Parent
            GameObject managersParent = new GameObject("--- MANAGERS ---");

            // Game Manager
            GameObject gmObj = new GameObject("GameManager");
            gmObj.transform.SetParent(managersParent.transform);
            gmObj.AddComponent<GameManager>();

            // Grid Manager
            GameObject gridObj = new GameObject("GridManager");
            gridObj.transform.SetParent(managersParent.transform);
            gridObj.AddComponent<GridManager>();

            // Match Manager
            GameObject matchObj = new GameObject("MatchManager");
            matchObj.transform.SetParent(managersParent.transform);
            matchObj.AddComponent<MatchManager>();

            // Word Manager
            GameObject wordObj = new GameObject("WordManager");
            wordObj.transform.SetParent(managersParent.transform);
            wordObj.AddComponent<WordManager>();

            // Word Database Service (NEW - for expanded word lists)
            GameObject wordDbObj = new GameObject("WordDatabaseService");
            wordDbObj.transform.SetParent(managersParent.transform);
            wordDbObj.AddComponent<WordDatabaseService>();

            // UI Manager
            GameObject uiMgrObj = new GameObject("UIManager");
            uiMgrObj.transform.SetParent(managersParent.transform);
            uiMgrObj.AddComponent<UIManager>();

            // Audio Manager
            GameObject audioObj = new GameObject("AudioManager");
            audioObj.transform.SetParent(managersParent.transform);
            AudioManager audioMgr = audioObj.AddComponent<AudioManager>();
            
            // Add audio sources
            AudioSource sfxSource = audioObj.AddComponent<AudioSource>();
            sfxSource.playOnAwake = false;
            
            AudioSource musicSource = audioObj.AddComponent<AudioSource>();
            musicSource.playOnAwake = false;
            musicSource.loop = true;

            Debug.Log("Created all manager GameObjects (including WordDatabaseService)");
        }

        private static Canvas CreateMainCanvas()
        {
            // Main Canvas
            GameObject canvasObj = new GameObject("Canvas");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            
            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            canvasObj.AddComponent<GraphicRaycaster>();

            // Event System (if not exists)
            if (Object.FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                GameObject eventSystem = new GameObject("EventSystem");
                eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
                eventSystem.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            }

            return canvas;
        }

        private static void CreateUIStructure(Canvas canvas)
        {
            // ═══════════════════════════════════════════════════════
            // MAIN MENU SCREEN
            // ═══════════════════════════════════════════════════════
            GameObject mainMenu = CreateScreen(canvas.transform, "MainMenuScreen", backgroundColor);
            
            // Title
            CreateText(mainMenu.transform, "Title", "FLUENCY DRIVE", 72, new Vector2(0, 200), true);
            
            // Subtitle
            CreateText(mainMenu.transform, "Subtitle", "Match • Reveal • Learn", 28, new Vector2(0, 120));
            
            // Play Button
            CreateButton(mainMenu.transform, "PlayButton", "PLAY", new Vector2(0, -50), new Vector2(300, 70), "OnPlayButton");
            
            // Continue Button
            CreateButton(mainMenu.transform, "ContinueButton", "CONTINUE", new Vector2(0, -140), new Vector2(300, 70), "OnContinueButton");
            
            // High Score Text
            CreateText(mainMenu.transform, "HighScoreText", "High Score: 0", 24, new Vector2(0, -250));
            
            // Max Level Text
            CreateText(mainMenu.transform, "MaxLevelText", "Level Reached: 1", 20, new Vector2(0, -290));

            // ═══════════════════════════════════════════════════════
            // GAMEPLAY SCREEN
            // ═══════════════════════════════════════════════════════
            GameObject gameplayScreen = CreateScreen(canvas.transform, "GameplayScreen", Color.clear);
            gameplayScreen.SetActive(false);

            // Top HUD Bar
            GameObject topBar = CreatePanel(gameplayScreen.transform, "TopBar", 
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -50), new Vector2(1800, 80));

            CreateText(topBar.transform, "LevelText", "Level 1", 32, new Vector2(-400, 0));
            CreateText(topBar.transform, "ScoreText", "Score: 0", 32, new Vector2(0, 0));
            CreateText(topBar.transform, "TimerText", "02:00", 32, new Vector2(400, 0));

            // Pause Button
            CreateButton(topBar.transform, "PauseButton", "| |", new Vector2(800, 0), new Vector2(60, 60), "OnPauseButton");

            // Word Display Area
            GameObject wordArea = CreatePanel(gameplayScreen.transform, "WordArea",
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -150), new Vector2(800, 100));

            CreateText(wordArea.transform, "WordDisplayText", "_ _ _ _ _ _", 48, Vector2.zero, true);
            CreateText(wordArea.transform, "HintText", "Category: Language", 20, new Vector2(0, -45));

            // Progress Bar
            CreateSlider(gameplayScreen.transform, "ProgressBar", new Vector2(0, -220), new Vector2(600, 20));

            // Grid Container
            GameObject gridContainer = new GameObject("GridContainer");
            gridContainer.transform.SetParent(gameplayScreen.transform);
            RectTransform gridRect = gridContainer.AddComponent<RectTransform>();
            gridRect.anchorMin = new Vector2(0.5f, 0.5f);
            gridRect.anchorMax = new Vector2(0.5f, 0.5f);
            gridRect.anchoredPosition = new Vector2(0, -50);
            gridRect.sizeDelta = new Vector2(700, 700);

            // Combo Text
            GameObject comboText = CreateText(gameplayScreen.transform, "ComboText", "COMBO x2!", 48, new Vector2(0, 300), true);
            comboText.SetActive(false);

            // ═══════════════════════════════════════════════════════
            // PAUSE SCREEN
            // ═══════════════════════════════════════════════════════
            GameObject pauseScreen = CreateScreen(canvas.transform, "PauseScreen", new Color(0, 0, 0, 0.8f));
            pauseScreen.SetActive(false);

            CreateText(pauseScreen.transform, "PausedTitle", "PAUSED", 64, new Vector2(0, 150), true);
            CreateButton(pauseScreen.transform, "ResumeButton", "RESUME", new Vector2(0, 0), new Vector2(300, 70), "OnResumeButton");
            CreateButton(pauseScreen.transform, "MenuButton", "MAIN MENU", new Vector2(0, -90), new Vector2(300, 70), "OnMainMenuButton");

            // ═══════════════════════════════════════════════════════
            // WIN SCREEN
            // ═══════════════════════════════════════════════════════
            GameObject winScreen = CreateScreen(canvas.transform, "WinScreen", new Color(0, 0, 0, 0.9f));
            winScreen.SetActive(false);

            CreateText(winScreen.transform, "WinLevelText", "Level 1 Complete!", 48, new Vector2(0, 280), true);
            CreateText(winScreen.transform, "WinWordText", "FLUENT", 64, new Vector2(0, 200), true);

            // Stats
            CreateText(winScreen.transform, "WinScoreText", "Score: 1,000", 32, new Vector2(0, 80));
            CreateText(winScreen.transform, "WinTimeBonusText", "Time Bonus: +500", 28, new Vector2(0, 40));
            CreateText(winScreen.transform, "WinPerfectBonusText", "Perfect: +1,000", 28, new Vector2(0, 0));
            CreateText(winScreen.transform, "WinTotalText", "Total: 2,500", 40, new Vector2(0, -60), true);

            // Stars
            GameObject starContainer = CreatePanel(winScreen.transform, "StarContainer",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -140), new Vector2(300, 80));

            for (int i = 0; i < 3; i++)
            {
                GameObject star = new GameObject($"Star{i + 1}");
                star.transform.SetParent(starContainer.transform);
                RectTransform starRect = star.AddComponent<RectTransform>();
                starRect.anchoredPosition = new Vector2(-80 + i * 80, 0);
                starRect.sizeDelta = new Vector2(60, 60);
                Image starImg = star.AddComponent<Image>();
                starImg.color = Color.yellow;
            }

            // Buttons
            CreateButton(winScreen.transform, "NextLevelButton", "NEXT LEVEL", new Vector2(0, -250), new Vector2(300, 70), "OnNextLevelButton");
            CreateButton(winScreen.transform, "ReplayButton", "REPLAY", new Vector2(0, -340), new Vector2(300, 70), "OnReplayButton");

            // ═══════════════════════════════════════════════════════
            // DEFINITION PANEL
            // ═══════════════════════════════════════════════════════
            GameObject defPanel = CreatePanel(winScreen.transform, "DefinitionPanel",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(500, 0), new Vector2(400, 500));
            defPanel.GetComponent<Image>().color = new Color(0.1f, 0.1f, 0.15f, 0.95f);

            CreateText(defPanel.transform, "DefWordText", "FLUENT", 36, new Vector2(0, 200), true);
            CreateText(defPanel.transform, "DefCategoryText", "Language Skills", 20, new Vector2(0, 160));
            CreateText(defPanel.transform, "DefPronunciationText", "/ˈfluːənt/", 18, new Vector2(0, 130));
            
            GameObject defText = CreateText(defPanel.transform, "DefinitionText", 
                "Able to express oneself easily and articulately.", 22, new Vector2(0, 20));
            defText.GetComponent<Text>().alignment = TextAnchor.UpperCenter;
            defText.GetComponent<RectTransform>().sizeDelta = new Vector2(350, 150);

            CreateText(defPanel.transform, "ExamplesText", "• She is fluent in three languages.", 18, new Vector2(0, -120));

            // ═══════════════════════════════════════════════════════
            // GAME OVER SCREEN
            // ═══════════════════════════════════════════════════════
            GameObject gameOverScreen = CreateScreen(canvas.transform, "GameOverScreen", new Color(0.1f, 0, 0, 0.95f));
            gameOverScreen.SetActive(false);

            CreateText(gameOverScreen.transform, "GameOverTitle", "TIME'S UP!", 72, new Vector2(0, 150), true);
            CreateText(gameOverScreen.transform, "GameOverScoreText", "Score: 0", 40, new Vector2(0, 50));
            CreateText(gameOverScreen.transform, "GameOverHighScoreText", "High Score: 0", 28, new Vector2(0, -10));
            
            CreateButton(gameOverScreen.transform, "TryAgainButton", "TRY AGAIN", new Vector2(0, -100), new Vector2(300, 70), "OnPlayButton");
            CreateButton(gameOverScreen.transform, "GameOverMenuButton", "MAIN MENU", new Vector2(0, -190), new Vector2(300, 70), "OnMainMenuButton");

            Debug.Log("Created all UI screens and elements");
        }

        // ═══════════════════════════════════════════════════════
        // HELPER METHODS
        // ═══════════════════════════════════════════════════════

        private static GameObject CreateScreen(Transform parent, string name, Color bgColor)
        {
            GameObject screen = new GameObject(name);
            screen.transform.SetParent(parent);
            
            RectTransform rect = screen.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Image bg = screen.AddComponent<Image>();
            bg.color = bgColor;
            bg.raycastTarget = true;

            screen.AddComponent<CanvasGroup>();

            return screen;
        }

        private static GameObject CreatePanel(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 position, Vector2 size)
        {
            GameObject panel = new GameObject(name);
            panel.transform.SetParent(parent);
            
            RectTransform rect = panel.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;

            Image bg = panel.AddComponent<Image>();
            bg.color = new Color(0, 0, 0, 0.5f);

            return panel;
        }

        private static GameObject CreateText(Transform parent, string name, string content, int fontSize, Vector2 position, bool bold = false)
        {
            GameObject textObj = new GameObject(name);
            textObj.transform.SetParent(parent);
            
            RectTransform rect = textObj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(600, fontSize + 20);

            Text text = textObj.AddComponent<Text>();
            text.text = content;
            text.fontSize = fontSize;
            text.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            return textObj;
        }

        private static GameObject CreateButton(Transform parent, string name, string label, Vector2 position, Vector2 size, string methodName)
        {
            GameObject buttonObj = new GameObject(name);
            buttonObj.transform.SetParent(parent);
            
            RectTransform rect = buttonObj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;

            Image bg = buttonObj.AddComponent<Image>();
            bg.color = primaryColor;

            Button button = buttonObj.AddComponent<Button>();
            ColorBlock colors = button.colors;
            colors.highlightedColor = new Color(0.3f, 0.7f, 1f);
            colors.pressedColor = new Color(0.1f, 0.4f, 0.7f);
            button.colors = colors;

            // Button text
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(buttonObj.transform);
            
            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            Text text = textObj.AddComponent<Text>();
            text.text = label;
            text.fontSize = 28;
            text.fontStyle = FontStyle.Bold;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            return buttonObj;
        }

        private static GameObject CreateSlider(Transform parent, string name, Vector2 position, Vector2 size)
        {
            GameObject sliderObj = new GameObject(name);
            sliderObj.transform.SetParent(parent);
            
            RectTransform rect = sliderObj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;

            Slider slider = sliderObj.AddComponent<Slider>();
            slider.interactable = false;
            slider.minValue = 0;
            slider.maxValue = 1;
            slider.value = 0;

            // Background
            GameObject bgObj = new GameObject("Background");
            bgObj.transform.SetParent(sliderObj.transform);
            RectTransform bgRect = bgObj.AddComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;
            Image bgImg = bgObj.AddComponent<Image>();
            bgImg.color = new Color(0.2f, 0.2f, 0.2f);

            // Fill Area
            GameObject fillArea = new GameObject("Fill Area");
            fillArea.transform.SetParent(sliderObj.transform);
            RectTransform fillAreaRect = fillArea.AddComponent<RectTransform>();
            fillAreaRect.anchorMin = Vector2.zero;
            fillAreaRect.anchorMax = Vector2.one;
            fillAreaRect.offsetMin = Vector2.zero;
            fillAreaRect.offsetMax = Vector2.zero;

            // Fill
            GameObject fill = new GameObject("Fill");
            fill.transform.SetParent(fillArea.transform);
            RectTransform fillRect = fill.AddComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
            Image fillImg = fill.AddComponent<Image>();
            fillImg.color = primaryColor;

            slider.fillRect = fillRect;

            return sliderObj;
        }

        private void OnGUI()
        {
            GUILayout.Space(20);
            
            GUIStyle titleStyle = new GUIStyle(GUI.skin.label);
            titleStyle.fontSize = 24;
            titleStyle.fontStyle = FontStyle.Bold;
            titleStyle.alignment = TextAnchor.MiddleCenter;
            
            GUILayout.Label("🎮 Fluency Drive Setup", titleStyle);
            
            GUILayout.Space(20);
            
            EditorGUILayout.HelpBox(
                "Use these tools to quickly set up your Fluency Drive project.\n\n" +
                "1. Create Tile Prefab - Generates the tile prefab with all components\n" +
                "2. Setup Scene - Creates managers, canvas, and all UI elements", 
                MessageType.Info);

            GUILayout.Space(20);

            if (GUILayout.Button("Create Tile Prefab", GUILayout.Height(40)))
            {
                CreateTilePrefab();
            }

            GUILayout.Space(10);

            if (GUILayout.Button("Setup Scene", GUILayout.Height(40)))
            {
                SetupScene();
            }

            GUILayout.Space(20);

            EditorGUILayout.HelpBox(
                "After setup, don't forget to:\n" +
                "• Import DOTween from the Asset Store\n" +
                "• Assign component references in the Inspector\n" +
                "• Add audio clips to AudioManager", 
                MessageType.Warning);
        }
    }
}
#endif
