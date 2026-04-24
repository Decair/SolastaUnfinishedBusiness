# Solasta Unfinished Business — Design Document

## System Overview

SolastaUnfinishedBusiness is a **runtime content injection mod** for Solasta: Crown of the Magister. It extends the game's D&D 5e implementation by:
- Injecting new game definitions (classes, spells, feats, etc.) into the runtime database
- Intercepting game logic via HarmonyLib method patching
- Attaching custom behavioral interfaces to definitions for event-driven logic
- Decorating Unity's resource pipeline to serve custom assets

The mod achieves comprehensive game extension (multiclass, 95+ subclasses, 200+ feats, 100+ spells) **without modifying any game files on disk**.

## Architecture

```
┌─────────────────────────────────────────────────────────────────────┐
│                        UNITY MOD MANAGER                            │
│  (Loads DLL, provides settings UI, manages mod lifecycle)           │
└────────────────────────────────┬────────────────────────────────────┘
                                 │ Load()
                                 ▼
┌─────────────────────────────────────────────────────────────────────┐
│                           MAIN.CS                                   │
│  Entry point → ModManager<Core, Settings> → Harmony.PatchAll()      │
└────────────────────────────────┬────────────────────────────────────┘
                                 │
              ┌──────────────────┼──────────────────┐
              ▼                  ▼                  ▼
┌──────────────────┐  ┌──────────────────┐  ┌──────────────────────┐
│   PATCHES LAYER  │  │  RESOURCE LAYER  │  │   SETTINGS LAYER     │
│  273 Harmony     │  │  Sprites, Models │  │  XML serialization   │
│  patches on game │  │  Translations    │  │  200+ toggles        │
│  methods         │  │  Addressables    │  │  Named presets       │
└────────┬─────────┘  └────────┬─────────┘  └──────────┬───────────┘
         │                     │                       │
         ▼                     ▼                       ▼
┌─────────────────────────────────────────────────────────────────────┐
│                       MODELS LAYER (75 Contexts)                    │
│  BootContext orchestrates load order                                │
│  Each *Context wires: Builders + Interfaces + Behaviors + Settings  │
└──────────┬──────────────────────┬──────────────────────┬────────────┘
           │                      │                      │
           ▼                      ▼                      ▼
┌────────────────┐  ┌──────────────────────┐  ┌────────────────────┐
│ BUILDERS LAYER │  │   INTERFACES LAYER   │  │  BEHAVIORS LAYER   │
│ 60+ fluent     │  │   82 hook-point      │  │  70 reusable       │
│ definition     │  │   interfaces for     │  │  logic components  │
│ builders       │  │   game events        │  │  (markers, impls)  │
└───────┬────────┘  └──────────┬───────────┘  └────────┬───────────┘
        │                      │                       │
        ▼                      ▼                       ▼
┌─────────────────────────────────────────────────────────────────────┐
│                    GAME DATABASE (Runtime)                          │
│  BaseDefinition registry — all game definitions indexed by type     │
│  Sub-feature attachments — interface impls stored per definition    │
└─────────────────────────────────────────────────────────────────────┘
        │                                              ▲
        ▼                                              │
┌─────────────────────────────────────────────────────────────────────┐
│                    CONTENT LAYER                                    │
│  Classes/  Subclasses/  Feats/  Spells/  Races/  FightingStyles/    │
│  Backgrounds/  ItemCrafting/  Actions/                              │
│  (Each creates definitions using Builders + attaches Behaviors)     │
└─────────────────────────────────────────────────────────────────────┘
```

## Components

### Main (Entry Point)
- **Responsibility:** Bootstrap mod lifecycle, validate environment, trigger initialization
- **Inputs:** UMM Load callback
- **Outputs:** Fully initialized mod with all patches active and content registered
- **Dependencies:** UnityModManager, HarmonyLib

### Patches Layer
- **Responsibility:** Intercept game method execution to inject custom logic
- **Inputs:** Game method calls at runtime
- **Outputs:** Modified behavior (prefix abort, postfix modification, IL rewriting)
- **Dependencies:** HarmonyLib, game's Assembly-CSharp

### Models Layer (Contexts)
- **Responsibility:** Orchestrate content creation, wire settings to features, manage load order
- **Inputs:** Game database (post-load), Settings state
- **Outputs:** Registered definitions, enabled/disabled feature sets
- **Dependencies:** Builders, Interfaces, Behaviors, Settings

### Builders Layer
- **Responsibility:** Construct game BaseDefinition objects via type-safe fluent APIs
- **Inputs:** Configuration parameters (names, properties, features)
- **Outputs:** Fully configured definition objects registered in game database
- **Dependencies:** Game type system, GUID generation

### Interfaces Layer
- **Responsibility:** Define contracts for game-event hooks
- **Inputs:** Game events dispatched by patches
- **Outputs:** Custom logic execution (damage modification, attack alteration, condition handling)
- **Dependencies:** None (pure contracts)

### Behaviors Layer
- **Responsibility:** Provide reusable logic implementations of interfaces
- **Inputs:** Game state, character state, combat context
- **Outputs:** Modified game values, triggered effects, state changes
- **Dependencies:** Interfaces, game API

### Content Layer
- **Responsibility:** Define all custom game content (spells, feats, subclasses, etc.)
- **Inputs:** Builder APIs, existing game definitions (as templates)
- **Outputs:** Complete definition objects with features, effects, and behaviors
- **Dependencies:** Builders, Behaviors, Validators

### Resource Layer
- **Responsibility:** Serve custom assets (sprites, prefabs, materials, translations) to game's Addressables system
- **Inputs:** Embedded PNG resources, AssetBundle files, translation text files
- **Outputs:** Unity Sprites, GameObjects, Materials, localization terms
- **Dependencies:** Unity Addressables, I2 Localization

### Settings Layer
- **Responsibility:** Persist user preferences, expose configuration UI
- **Inputs:** User interactions via UMM GUI
- **Outputs:** Serialized XML settings, runtime feature toggles
- **Dependencies:** UMM, ModKit UI framework

## Data Flow

### Mod Initialization Sequence

```
1. Game launches → UMM loaded → Main.Load() called
2. Harmony patches ALL [HarmonyPatch] classes in assembly (273 files)
3. Game continues loading → GameManager.BindPostDatabase fires
4. GameManagerPatcher intercepts → triggers BootContext.Startup()
5. BootContext calls each Context.Load() in sequence:
   a. TranslatorContext → loads translations into I2
   b. CeContentPackContext → registers mod content pack ID
   c. ResourceLocatorHelper → registers custom asset providers
   d. Feature Contexts → build + register definitions via builders
   e. Content Contexts → create classes, subclasses, feats, spells
6. Game fires RuntimeLoaded service event
7. BootContext calls each Context.LateLoad():
   a. Wire cross-references between definitions
   b. Apply settings-based filtering
   c. Register with GUI collections
8. Main.Enable() → mod fully operational
```

### Combat Event Flow (Interface Dispatch)

```
1. Player initiates attack → game calls CharacterActionAttack.Execute()
2. Harmony Postfix on game's attack method fires
3. Patch code calls:
   foreach (var handler in attacker.GetSubFeaturesByType<IPhysicalAttackFinishedByMe>())
       yield return handler.OnPhysicalAttackFinishedByMe(battleManager, action, ...)
4. GetSubFeaturesByType<T>() iterates:
   - All active FeatureDefinitions on the character
   - All active conditions on the character
   - For each: checks definition itself + attached sub-features for type T
5. Each matching handler executes its custom logic
6. Game continues with modified state
```

### Definition Creation Flow

```
1. Content code calls: SpellDefinitionBuilder.Create("MySpell")
2. Builder allocates ScriptableObject.CreateInstance<SpellDefinition>()
3. Builder generates GUID: GuidHelper.Create(CeNamespaceGuid, "MySpell")
4. Builder reflectively initializes all null collection fields to empty
5. Fluent setters configure the definition properties
6. .AddCustomSubFeatures(...) attaches interface implementations
7. .AddToDB() validates the definition and registers it in:
   - SpellDefinition database
   - BaseDefinition database
   - All intermediate type databases
8. Definition is now discoverable by game systems
```

## Design Patterns

### 1. Builder Pattern (CRTP Fluent API)
**Why chosen:** Game definitions have 20-100+ properties. Constructors would be unwieldy. Fluent builders provide readable, discoverable, type-safe configuration.

**Implementation:**
```
DefinitionBuilder (abstract: verification, naming)
  └─ DefinitionBuilder<TDefinition> (generic: holds ScriptableObject instance)
      └─ DefinitionBuilder<TDefinition, TBuilder> (CRTP: returns `this` as TBuilder for fluent chaining)
```

The CRTP eliminates casting — each concrete builder (e.g., `SpellDefinitionBuilder`) returns itself from every setter method without the caller needing to cast.

### 2. Strategy Pattern (Interface-based Event Dispatch)
**Why chosen:** Game logic needs to vary based on which features a character has. Rather than massive switch statements, each feature carries its own behavior via interfaces. New features don't require modifying existing dispatch code.

**Implementation:** 82 interfaces define game-event contracts. Implementations are attached to definitions as sub-features. Patches iterate all active interfaces per event.

### 3. Sub-Feature Composition
**Why chosen:** A game definition's C# type is fixed by the game engine. The mod can't create new FeatureDefinition subtypes the game doesn't know. Sub-features allow attaching arbitrary behavior objects to ANY existing definition without altering its type.

**Implementation:**
```csharp
static Dictionary<BaseDefinition, List<object>> CustomSubFeatures;
definition.AddCustomSubFeatures(new MyBehavior());
// Later:
var behaviors = definition.GetAllSubFeaturesOfType<IMyInterface>();
```

### 4. Template Method (Builders)
**Why chosen:** All builders share initialization logic (GUID, collections, DB registration) but need type-specific setup.

**Implementation:** `DefinitionBuilder.Initialise()` is virtual. Concrete builders override to set type-specific defaults (e.g., `SpellDefinitionBuilder.Initialise()` sets default spell properties).

### 5. Registry Pattern (Database + Content Pack)
**Why chosen:** Game uses a central database of definitions indexed by type. Mod content needs to be distinguishable from vanilla content for filtering/toggling.

**Implementation:** All mod definitions get `ContentPack = 9999` (CeContentPack) and are registered via `AddToDB()` into the same databases the game uses.

### 6. Decorator Pattern (Resource Providers)
**Why chosen:** The game loads assets via Unity's Addressables system using GUIDs. Mod content needs custom sprites/prefabs served through the same pipeline without modifying game asset bundles.

**Implementation:** Custom `IResourceProvider` + `IResourceLocator` implementations are registered with Addressables. When the game requests an asset by GUID, custom providers intercept and serve mod-created assets.

### 7. Context/Facade Pattern (Models)
**Why chosen:** Each feature area (multiclass, spells, subclasses) requires coordinating multiple builders, patches, and settings. Contexts encapsulate this complexity behind simple `Load()`/`LateLoad()`/`Switch()` methods.

### 8. Observer Pattern (Event Listeners)
**Why chosen:** Multiple independent features may need to react to the same game event (e.g., "attack finished"). The sub-feature iteration is fundamentally pub/sub — all interested subscribers get notified.

### 9. Factory Method (Definition Cloning)
**Why chosen:** Many mod definitions are slight variations of existing game definitions. `Create(original, newName)` deep-clones an existing definition, allowing modification of the copy.

### 10. Marker Interface Pattern
**Why chosen:** Some behaviors are binary (enabled/disabled) with no parameters. Marker interfaces (`IPreventRemoveConcentrationOnPowerUse`, `IIgnoreInvisibilityInterruptionCheck`) signal their presence via `HasSubFeatureOfType<T>()` without needing method implementations.

## State Management

### No Persistent Mod State
The mod stores NO runtime state beyond the game's own state systems. All mod content is expressed through:
- Game definitions (registered in the game's databases)
- Game conditions (applied/removed via standard game mechanics)
- `UsedSpecialFeatures` dictionary on `RulesetCharacter` (game-provided per-turn tracking)

### Settings State
- Single `Settings` object serialized to XML
- Loaded on mod init, saved on change
- Controls which features are enabled/disabled
- No per-character or per-save mod state

### Sub-Feature Registry
- `Dictionary<BaseDefinition, List<object>>` — lives in static memory
- Populated during initialization, immutable after boot
- Never serialized — reconstructed every game launch

## Error Handling Strategy

### Harmony Patch Safety
- Patches wrapped in try/catch to prevent game crashes from mod errors
- `Main.Error(exception)` logs to both UMM log and Unity's `Debug.LogError`
- Failed patches don't prevent other patches from loading

### Builder Validation
- `DefinitionBuilder.Verify()` validates definitions before DB registration
- Duplicate name detection prevents registration conflicts
- `PreConditions.Assert*` methods validate builder state

### Feature Toggling
- Every piece of content can be individually disabled via settings
- Disabled content is removed from game databases or filtered from UI
- Settings changes take effect without restart (most features)

## Security Model

Not applicable (single-player/co-op game mod). No authentication, authorization, or data protection concerns. The mod runs with full trust in the game's process space.

## Performance Considerations

### Harmony Patch Cost
- 273 patches add constant overhead to patched methods
- Transpiler patches have zero runtime cost (IL rewritten at patch time)
- Prefix/Postfix patches add one method call per invocation

### Interface Dispatch Optimization
- `GetSubFeaturesByType<T>()` iterates ALL character features every time called
- This is called frequently in combat (every attack, every save)
- Mitigated by: features are typically < 50 per character, interface checks are type-test only

### Resource Loading
- Sprites loaded lazily and cached by GUID in static dictionary
- Translation terms loaded once at startup into I2's in-memory database
- Asset providers do GUID lookup (O(1) dictionary) before falling through to game's providers

### Definition Database Registration
- `AddToDB()` registers in ALL matching type databases (a SpellDefinition goes into SpellDefinition DB + BaseDefinition DB)
- One-time cost at startup, amortized across entire session

## Key Design Decisions

### 1. Interface-based Dispatch over Event System
**Decision:** Use C# interfaces attached to definitions rather than a central event bus.
**Trade-off:** More interface files to maintain, but behavior is always co-located with the feature that needs it. No event registration/unregistration lifecycle to manage.

### 2. Builders over Direct Construction
**Decision:** All definitions created via fluent builders, never directly.
**Trade-off:** ~60 builder classes to maintain. Gained: consistent initialization, GUID generation, null-safety, readable content code.

### 3. Static Architecture (no DI)
**Decision:** All Contexts, Helpers, and Infrastructure are static classes.
**Trade-off:** No testability via dependency injection. Gained: simpler code, no container overhead, matches game's own static patterns.

### 4. Content Pack Identification
**Decision:** All mod content tagged with `ContentPack = 9999`.
**Trade-off:** Limits to one content pack per mod. Gained: simple filtering of "is this mod content?" everywhere.

### 5. Translations as Key-Value Text Files
**Decision:** Simple `key=value` format rather than XLIFF/PO/JSON.
**Trade-off:** No plural forms or ICU message format. Gained: community contributors can edit with any text editor.

### 6. Blueprint JSON Dumps as Documentation
**Decision:** Diagnostics dumps definitions as JSON but never loads from them.
**Trade-off:** JSON files can drift if dump code changes. Gained: authoritative documentation of what definitions look like at runtime, useful for debugging and reference.

### 7. Everything Optional by Default
**Decision:** All mod content disabled by default, user must opt-in.
**Trade-off:** New users must configure before seeing value. Gained: maximum compatibility, no unwanted surprises, easy troubleshooting (disable suspicious features one by one).
