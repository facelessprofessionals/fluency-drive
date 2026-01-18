# 🎮 Fluency Drive

A vocabulary-building match puzzle game built with Unity. Match tiles to reveal letters, discover words, and learn their definitions!

## 🎯 Game Concept

Players match tiles of the same type to reveal hidden letters. Once all letters are revealed, the word assembles itself and displays its definition, helping players build vocabulary through engaging gameplay.

## 📁 Project Structure

```
FluencyDrive/
│
├─ Assets/
│   ├─ Scripts/
│   │   ├─ Tile.cs           # Individual tile behavior & animations
│   │   ├─ GridManager.cs    # Grid spawning & layout management
│   │   ├─ MatchManager.cs   # Match logic & validation
│   │   ├─ WordManager.cs    # Word/letter tracking & definitions
│   │   ├─ GameManager.cs    # Game state & level progression
│   │   └─ AudioManager.cs   # Sound effects & music
│   ├─ Prefabs/
│   │   ├─ TilePrefab.prefab
│   │   └─ UI_WinScreen.prefab
│   └─ Art/
│       └─ (placeholder tiles & colors)
│
├─ Scenes/
│   └─ Main.unity
│
└─ Resources/
    └─ Words.json            # Word database with definitions
```

## 🎮 Level Completion Flow

When all letters are revealed:

```
IF all_letters_revealed == true
    → Pause gameplay
    → Animate word assembly (letters fly into position)
    → Display word definition
    → Award bonuses (time, perfect match, completion)
    → Unlock next level
```

## ✨ Features

- **Match-3 Style Gameplay**: Match tiles of the same type to reveal letters
- **Progressive Difficulty**: Words increase in complexity as you advance
- **Vocabulary Building**: Learn definitions, examples, and synonyms
- **Combo System**: Chain matches quickly for bonus points
- **Smooth Animations**: DOTween-powered tile animations
- **Progress Saving**: Continue from where you left off

## 🔧 Setup Instructions

### Prerequisites
- Unity 2021.3 LTS or newer
- DOTween (Free) - [Asset Store Link](https://assetstore.unity.com/packages/tools/animation/dotween-hotween-v2-27676)

### Installation

1. Clone this repository
2. Open the project in Unity
3. Import DOTween from the Asset Store
4. Open `Scenes/Main.unity`
5. Create the required prefabs (see below)

### Creating the Tile Prefab

1. Create a new UI Canvas (if not exists)
2. Create a UI Panel as the tile base
3. Add the following components:
   - `Image` (for background)
   - `Image` (for icon, as child)
   - `Text` (for letter, as child)
   - `Tile.cs` script
4. Configure references in the Tile component
5. Save as `Prefabs/TilePrefab.prefab`

### Scene Setup

1. Create empty GameObjects for managers:
   - `GameManager` with `GameManager.cs`
   - `GridManager` with `GridManager.cs`
   - `MatchManager` with `MatchManager.cs`
   - `WordManager` with `WordManager.cs`
   - `AudioManager` with `AudioManager.cs`

2. Create UI elements:
   - Score Text
   - Level Text
   - Timer Text
   - Word Display Text
   - Progress Bar (Slider)
   - Definition Panel
   - Win Screen Panel

3. Wire up references in the Inspector

## 📊 Word Database

Words are stored in `Resources/Words.json` with the following structure:

```json
{
  "words": [
    {
      "word": "FLUENT",
      "definition": "Able to express oneself easily...",
      "difficulty": 1,
      "category": "Language Skills",
      "pronunciation": "/ˈfluːənt/",
      "examples": ["She is fluent in three languages."],
      "synonyms": ["articulate", "eloquent"]
    }
  ]
}
```

### Difficulty Levels
- **1**: Basic words (4-5 letters)
- **2**: Intermediate (5-6 letters)
- **3**: Advanced (6-7 letters)
- **4**: Expert (7-9 letters)
- **5**: Master (9+ letters)

## 🎨 Customization

### Adding New Words
Edit `Resources/Words.json` to add new vocabulary words with definitions.

### Adjusting Difficulty
Modify `GameManager.cs` settings:
- `levelTimeLimit`: Time per level
- `baseMatchScore`: Points per match
- `comboMultiplier`: Bonus for chains

### Grid Size
Adjust in `GridManager.cs`:
- `gridWidth` / `gridHeight`: Grid dimensions
- `tileSize` / `tileSpacing`: Visual layout

## 🎵 Audio

Add audio clips to `AudioManager.cs`:
- `TileSelect` - When selecting a tile
- `TileMatch` - Successful match
- `InvalidMatch` - Failed match
- `LevelComplete` - Level cleared
- `WordAssembled` - Word animation complete
- `ShowDefinition` - Definition appears
- `BonusAwarded` - Bonus points added

## 📜 License

MIT License - Feel free to use and modify for your projects!

## 🤝 Contributing

Contributions welcome! Please feel free to submit pull requests.

---

**Built with ❤️ for language learners**