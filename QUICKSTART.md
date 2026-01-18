# 🚀 Fluency Drive - Quick Start Guide

## Step 1: Open in Unity

1. Open **Unity Hub**
2. Click **Add** → **Add project from disk**
3. Navigate to this folder and select it
4. Open with **Unity 2021.3 LTS** or newer

---

## Step 2: Install DOTween

DOTween is required for all animations:

1. Open **Window** → **Asset Store** (or visit [Unity Asset Store](https://assetstore.unity.com/packages/tools/animation/dotween-hotween-v2-27676))
2. Search for **DOTween (HOTween v2)**
3. Click **Download** then **Import**
4. When prompted, click **Setup DOTween**
5. Check all options and click **Apply**

---

## Step 3: Run Auto-Setup

Use the built-in editor tool to set up your scene:

1. Go to **Tools** → **Fluency Drive** → **Setup Window**
2. Click **Create Tile Prefab** - Creates the tile with all components
3. Click **Setup Scene** - Creates managers, canvas, and all UI

---

## Step 4: Wire Up References

After auto-setup, some references need manual connection:

### GridManager
1. Select **GridManager** in Hierarchy
2. Drag **TilePrefab** from `Assets/Prefabs/` to **Tile Prefab** field
3. Drag **GridContainer** from Canvas to **Grid Container** field

### UIManager  
1. Select **UIManager** in Hierarchy
2. Expand each section and drag corresponding UI elements from Canvas

### Tile Prefab
1. Open `Assets/Prefabs/TilePrefab.prefab`
2. Assign:
   - **Tile Background** → root Image
   - **Tile Icon** → Icon child Image
   - **Letter Text** → LetterText child Text
   - **Match Particles** → MatchParticles child ParticleSystem

---

## Step 5: Add Audio (Optional)

1. Add sound files to `Assets/Audio/`
2. Select **AudioManager** in Hierarchy
3. Assign clips to the sound effect fields

Recommended sounds:
- `TileSelect.wav` - Click/tap sound
- `TileMatch.wav` - Success chime
- `InvalidMatch.wav` - Error buzz
- `LevelComplete.wav` - Fanfare
- `BackgroundMusic.mp3` - Looping music

---

## Step 6: Play!

1. Press **Play** in Unity Editor
2. Click **PLAY** on the main menu
3. Match tiles of the same color to reveal letters
4. Complete the word to win!

---

## 🎮 Controls

| Action | Input |
|--------|-------|
| Select Tile | Click/Tap |
| Deselect | Click selected tile |
| Pause | Click pause button |

---

## 📁 Project Structure

```
Assets/
├── Scripts/
│   ├── Tile.cs              # Tile behavior
│   ├── GridManager.cs       # Grid management  
│   ├── MatchManager.cs      # Match logic
│   ├── WordManager.cs       # Word/vocabulary
│   ├── GameManager.cs       # Game state
│   ├── UIManager.cs         # UI handling
│   ├── AudioManager.cs      # Sound
│   ├── GameSettings.cs      # Config ScriptableObject
│   └── Editor/
│       └── FluencyDriveSetup.cs  # Auto-setup tool
├── Prefabs/
│   └── TilePrefab.prefab
├── Art/
├── Audio/
├── Scenes/
│   └── Main.unity
└── Resources/
    └── Words.json           # Word database
```

---

## ⚙️ Customization

### Word Database System

Fluency Drive now includes an **expanded word database** with 15,000+ words across multiple categories:

| Category | Words | Description |
|----------|-------|-------------|
| `common` | ~800 | Everyday vocabulary |
| `kids` | ~600 | Simple words for children |
| `phonics` | ~2,500 | Phonics-based learning |
| `gre` | ~800 | GRE test preparation |
| `medical` | ~1,500 | Medical terminology |
| `nursing` | ~1,200 | Nursing vocabulary |
| `legal` | ~2,000 | Legal terminology |
| `tech` | ~500 | Technology terms |

#### Adding More Words
Word lists are stored in `Resources/WordLists/` as simple text files (one word per line).

To add a new category:
1. Create `Resources/WordLists/your_category_words.txt`
2. Add words (one per line, uppercase recommended)
3. Update `WordDatabaseService.cs` to load your new list

#### API Integration
The `WordDatabaseService` can fetch definitions from external APIs:
- **Free Dictionary API** - definitions, phonetics, examples
- **Datamuse API** - synonyms, rhymes, related words

Enable/disable in the Inspector on the `WordManager` component.

### Legacy Word System
Edit `Resources/Words.json` for curated words with full definitions:
```json
{
  "word": "EXAMPLE",
  "definition": "Your definition here",
  "difficulty": 2,
  "category": "Category Name",
  "pronunciation": "/ɪɡˈzɑːmpəl/",
  "examples": ["Example sentence one.", "Example sentence two."],
  "synonyms": ["sample", "instance"]
}
```

### Create Game Settings Asset
1. Right-click in Project window
2. **Create** → **Fluency Drive** → **Game Settings**
3. Adjust values in Inspector
4. Drag to GameManager's Settings field

### Change Grid Size
1. Select **GridManager**
2. Adjust **Grid Width** and **Grid Height**
3. Adjust **Tile Size** and **Tile Spacing**

---

## 🐛 Troubleshooting

### "DOTween not found" errors
→ Import DOTween and run Setup DOTween wizard

### Tiles not appearing
→ Check GridManager has TilePrefab and GridContainer assigned

### UI not responding
→ Ensure EventSystem exists in scene (auto-created by Setup)

### No sound
→ Check AudioManager has AudioSource components and clips assigned

---

## 📚 Resources

- [DOTween Documentation](http://dotween.demigiant.com/documentation.php)
- [Unity UI Tutorial](https://learn.unity.com/tutorial/working-with-ui-in-unity)
- [Unity 2D Game Dev](https://learn.unity.com/pathway/2d-game-development)

---

**Happy coding! 🎮**
