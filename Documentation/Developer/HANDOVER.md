# Solasta Unfinished Business — Handover Guide

## What This Project Does

A comprehensive community mod for **Solasta: Crown of the Magister** (Unity/C# game). Adds multiclass support, 95+ subclasses, custom classes (Inventor), 200+ feats, 100+ spells, races, fighting styles, backgrounds, crafting recipes, gameplay options, UI improvements, and D&D 5e rules (2014 & 2024 variants). Everything is optional — install once, enable what you want.

## Prerequisites

| Requirement | Details |
|-------------|---------|
| **Solasta: Crown of the Magister** | Steam/GOG installed copy |
| **Unity Mod Manager (UMM)** | v0.27.x (not newer than 0.27.10) from Nexus Mods |
| **Visual Studio 2022** | Community Edition or higher |
| **.NET 4.8.1 targeting pack** | Project targets `net481` |
| **.NET SDK 6.0.300+** | For build tooling |
| **Environment Variable** | `SolastaInstallDir` pointing to game root folder |

## Installation (End-User)

1. Download latest ZIP from [GitHub Releases](https://github.com/EnderWiggin/SolastaUnfinishedBusiness/releases)
2. Install Unity Mod Manager, select Solasta, click "Install"
3. Extract mod ZIP into `{SolastaInstallDir}/Mods/SolastaUnfinishedBusiness/`
4. Launch game — UMM loads the mod automatically

## Development Setup

### 1. Environment Variable

Create system environment variable:
```
SolastaInstallDir = C:\Program Files (x86)\Steam\steamapps\common\Solasta Crown of the Magister
```
(Adjust path to your install location)

### 2. Publicize Game Assembly

The mod needs access to internal game types. The `Publicise.MSBuild.Task` NuGet package handles this automatically on clean/build:
- Input: `{SolastaInstallDir}/Solasta_Data/Managed/Assembly-CSharp.dll`
- Output: `lib/Assembly-CSharp_public.dll` (all types/members made public)

### 3. Build Configurations

| Configuration | Purpose |
|---------------|---------|
| `Debug Install` | Debug build, output directly to game Mods folder |
| `Release Install` | Optimized build to game Mods folder + creates ZIP |
| `Release Workflow` | CI/CD build (creates translation data) |

### 4. First Build

```bash
# Open solution in Visual Studio 2022
# 1. Clean Solution (triggers publicize of Assembly-CSharp.dll)
# 2. Build with "Release Workflow" to generate translation data
# 3. Switch to "Debug Install" or "Release Install" for development
```

### 5. External Dependencies (lib/ folder)

| Library | Purpose |
|---------|---------|
| `0Harmony.dll` | HarmonyLib runtime patching (from UMM) |
| `UnityModManager.dll` | Mod loader framework (from UMM) |
| `UnityExplorer.STANDALONE.Mono.dll` | Debug UI |
| `UniverseLib.Mono.dll` | UnityExplorer dependency |
| `mcs.dll` | Mono C# compiler (runtime compilation) |
| `Mono.Cecil*.dll` | IL manipulation |
| `MonoMod.RuntimeDetour.dll` | Low-level detours |
| `MonoMod.Utils.dll` | MonoMod utilities |
| `Tomlet.dll` | TOML configuration parsing |
| `NAudio.dll` | Audio utilities |

Game assemblies referenced directly from `{SolastaInstallDir}/Solasta_Data/Managed/`:
- `Assembly-CSharp.dll` (publicized)
- `UnityEngine*.dll` (multiple modules)
- `Newtonsoft.Json.dll`
- `Unity.Addressables.dll`
- `I2.dll` (localization)
- `PhotonUnityNetworking.dll` / `PhotonRealtime.dll`
- Various Wwise audio DLLs

## Running the Project

### Development

1. Build with `Debug Install` — DLL deploys to `{SolastaInstallDir}/Mods/SolastaUnfinishedBusiness/`
2. Launch Solasta normally
3. UMM loads the mod on game start
4. Open UMM in-game (Ctrl+F10) to access mod settings

### Debugging

1. Replace game's `UnityPlayer.dll` with Unity 2019.4.37 development build version
2. Add to `Solasta_Data/boot.config`:
   ```
   wait-for-managed-debugger=1
   player-connection-debug=1
   ```
3. Launch game → it waits for debugger
4. Visual Studio → Debug → Attach Unity Debugger
5. Set breakpoints in mod code, continue execution

### Testing

No automated test suite. Testing is manual:
1. Build mod
2. Launch game with mod enabled
3. Verify features in-game (character creation, combat, spell casting, etc.)
4. Check `Player.log` for errors

## Configuration (In-Game)

Mod settings accessed via UMM interface (Ctrl+F10 in-game):

### Tab Structure
- **Gameplay** → General, Rules, Campaigns, Crafting & Items, Dungeon Maker, Roleplay
- **Character** → Backgrounds & Races, Classes, Proficiencies, Spells, Subclasses
- **Encounters** → General, Bestiary, Characters Pool
- **Credits & Diagnostics** → Credits, Blueprints, Effects, Party Editor, Services

### Settings Storage
- Settings serialized as XML via UMM's `ModSettings`
- Named presets saved to `{ModFolder}/Settings/*.xml`
- All settings toggleable at runtime (some require restart)

## Common Operations

### Adding a New Feature

1. Create file in appropriate folder (Feats/, Spells/, Subclasses/, etc.)
2. Use the Builder pattern to define the feature
3. Register in the corresponding Context (FeatsContext, SpellsContext, etc.)
4. Add localization keys to `Translations/en/` files
5. Add UI toggle in corresponding Display file if needed
6. Build and test in-game

### Dumping Blueprints

Enable "Dump Blueprints" in Diagnostics tab → generates JSON in `Diagnostics/UnfinishedBusinessBlueprints/`

### Adding Translations

1. Add key=value pairs to `Translations/{language_code}/` files
2. Keys follow pattern: `{Category}/&{DefinitionName}Title` and `{Category}/&{DefinitionName}Description`
3. On Release build, translations are zipped into `Resources/Translations.zip`

## Project Structure

```
SolastaUnfinishedBusiness/
├── Actions/               # Custom CharacterAction implementations
├── Api/                   # Game API wrappers, extensions, ModKit UI
│   ├── Diagnostics/       # Debug tools
│   ├── GameExtensions/    # Extension methods for game types
│   ├── Helpers/           # TranspileHelper, GuidHelper, ResourceLocator
│   ├── Infrastructure/    # Assert, SerializableDictionary, RandomContext
│   ├── LanguageExtensions/# C# collection/type utilities
│   └── ModKit/            # UMM UI framework
├── Backgrounds/           # Custom background definitions
├── Behaviors/             # Reusable game logic components
│   └── Specific/          # Complex mechanic implementations
├── Builders/              # Fluent builders for game definitions
│   └── Features/          # FeatureDefinition-specific builders
├── Classes/               # Custom class definitions (Inventor)
├── CustomUI/              # Unity UI extensions (portraits, reactions, panels)
├── DataMiner/             # Debug data verification
├── DataViewer/            # Debug data inspection
├── Definitions/           # Static definition helpers
├── Displays/              # UMM settings UI panels
├── Feats/                 # Feat definitions by category
├── FightingStyles/        # Fighting style definitions
├── Interfaces/            # 82 game-event hook interfaces (THE BACKBONE)
├── ItemCrafting/          # Crafting recipe data
├── Models/                # 75 Context classes (orchestration layer)
├── Patches/               # 273 HarmonyLib patches
│   ├── Activities/        # Activity-related patches
│   └── Considerations/    # AI consideration patches
├── Portraits/             # PNG portrait images
├── Races/                 # Custom race/subrace definitions
├── Resources/             # Embedded sprite PNGs
├── Settings/              # Named setting preset XMLs
├── Spells/                # Spell definitions by level
├── Subclasses/            # 95+ subclass definitions
│   └── Builders/          # Subclass-specific helpers
├── Translations/          # Localization files (10 languages)
├── UnofficialTranslations/# Community translations + CJK fonts
└── Validators/            # Predicate checks for features/powers

Diagnostics/
├── OfficialBlueprints/    # JSON dump of all vanilla game definitions
├── UnfinishedBusinessBlueprints/ # JSON dump of all mod definitions
└── Translations-en/       # English translation reference

Documentation/             # Monster stat documentation
lib/                       # External DLL dependencies
Media/                     # Screenshots and media
Scripts/                   # Build/utility scripts
SolastaCeBootstrap/        # Minimal bootstrapper project
SolastaFiles/              # Game file references
SolastaFontAtlas/          # Unity project for CJK font atlas generation
MacOS/                     # macOS-specific files
```

## Troubleshooting

| Issue | Solution |
|-------|----------|
| Build fails: "SolastaInstallDir not set" | Create the environment variable pointing to game folder |
| Build fails: missing Assembly-CSharp | Clean solution first to trigger publicize step |
| Mod not loading | Verify UMM installed for Solasta, mod DLL in correct folder |
| UMM version error | Mod requires UMM ≤ 0.27.10 |
| Features not appearing | Check mod settings — most content disabled by default |
| Crash on load | Check `Player.log` in `%AppData%/LocalLow/Tactical Adventures/Solasta/` |
| Translation keys showing | Ensure translation files have the key or build with Release config |

## Branching Strategy

- `master` — Stable releases
- `Dev` — Development branch (merge target for PRs)
- Feature branches off `Dev` → PR back to `Dev`
- Periodic tested merges from `Dev` → `master` for releases
