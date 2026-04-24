# Solasta: Crown of the Magister — World & Presentation Design Document

> **Purpose**: Complete specification of all world building, dungeon authoring, visual/audio presentation, narrative systems, and travel/rest mechanics.
>
> **Companion Documents**: See `SOLASTA_DESIGN_DOC_CORE_SYSTEMS.md` (architecture, characters, features, spells, actions, AI) and `SOLASTA_DESIGN_DOC_CONTENT_DATA.md` (items, monsters, encounters, loot, feats, reference types).

---

## Table of Contents

1. [Campaign & World Map](#1-campaign--world-map)
2. [Location System](#2-location-system)
3. [Dungeon Building](#3-dungeon-building)
4. [Gadget Logic System](#4-gadget-logic-system)
5. [Investigation & Clue System](#5-investigation--clue-system)
6. [Quest & Narrative System](#6-quest--narrative-system)
7. [Character Presentation](#7-character-presentation)
8. [Audio & Atmosphere](#8-audio--atmosphere)
9. [Rest & Travel System](#9-rest--travel-system)
10. [Calendar & Time](#10-calendar--time)
11. [System Interconnection Map](#11-system-interconnection-map)

---

## 1. Campaign & World Map

### 1.1 CampaignDefinition

Defines a full game campaign — the top-level container for all content.

```json
{
  "$type": "CampaignDefinition, Assembly-CSharp",
  "registeredNodes": [
    {
      "nodeName": "TownMasgarth",
      "locationType": "Town",
      "positionOnMap": { "x": 0.45, "y": 0.62 },
      "locationDefinition": "Definition:TownMasgarth_LocationDB:guid",
      "connectedToNodes": ["CatacombsBridge", "RuinsOfIlthismar"]
    }
  ],
  "startingNode": "TavernIntro",
  "campaignVariables": [],
  "isUserCampaign": false,
  "maxLevelCap": 12,
  "partySize": 4,
  "overrideMinMaxLevel": false,
  "autoPreselectCampaignSpells": true,
  "technoIntegration": false,
  "calendarDefinition": "Definition:CalendarSolasta:guid"
}
```

### 1.2 World Map Node System

The world map is a **graph of named nodes** with pixel-based positioning. Each node links to a `LocationDefinition` and lists its neighbors for pathfinding.

**Travel between nodes** triggers the travel system (section 9) which includes random encounters, narrative events, and resource consumption.

### 1.3 Biome System

Travel routes are tagged with biomes that determine:
- Which random encounter table to roll
- Which ingredients can be foraged
- Background ambience and visual mood
- Travel narrative event pool

**Biome types**: `AridBadlands`, `AridMesa`, `Forest`, `Grassland`, `Jungle`, `Marsh`, `Mountain`, `Snowscape`, `Swamp`, `Underground`

### 1.4 TravelEventDefinition

Events that trigger during overworld travel.

```json
{
  "travelEventType": "NarrativeEncounter|Challenge|Discovery|Rest",
  "chancePerHour": 0.15,
  "biomeRestrictions": ["Forest"],
  "minimumPartyLevel": 1,
  "maximumPartyLevel": 20,
  "choices": [
    {
      "description": "Travel/&EventChoiceHelp",
      "skillCheck": "Survival",
      "difficultyClass": 15,
      "successOutcome": "Definition:LootPackSmallReward:guid",
      "failureOutcome": "Definition:ConditionExhausted:guid"
    }
  ]
}
```

---

## 2. Location System

### 2.1 LocationDefinition

The primary container for all explorable areas. Defines the physical space, visual rendering, and gameplay variables.

```json
{
  "$type": "LocationDefinition, Assembly-CSharp",
  "locationType": "Town|Dungeon|Exterior|Interior|Underground",
  "locationSize": "Small|Medium|Large|Huge",
  "explorationMusic": "Definition:MusicalState_Exploration:guid",
  "combatMusic": "Definition:MusicalState_Combat:guid",
  "restAllowed": true,
  "longRestAllowed": true,
  "shortRestAllowed": true,

  // Scene setup
  "startingArea": "AreaName",
  "initialMoodName": "MoodName",

  // Variable system (world state tracking)
  "registeredVariables": [
    {
      "name": "GateOpened",
      "type": "Int",
      "value": 0
    },
    {
      "name": "BossDefeated",
      "type": "Bool",
      "value": "false"
    }
  ],

  // Encounter data
  "encounterTableDefinition": "Definition:DungeonEncounterTable:guid",
  "encounterChancePerHour": 0.25,

  // Environmental
  "defaultWeather": "Clear",
  "allowWeatherChanges": false,
  "overrideDayCycle": false,
  "fixedTimeOfDay": 0.5,

  // Linked gadgets and exit points
  "exitPoints": [
    {
      "name": "ExitToOverworld",
      "destinationNode": "TownMasgarth",
      "destinationEntryPoint": "FromDungeon"
    }
  ]
}
```

### 2.2 Variable Registration System

Locations register typed variables that persist across visits and can be read/written by gadgets and quest logic. This is the core state machine for world progression.

**Variable types**: `Int`, `Bool`, `String`

**Usage patterns**:
- `GateOpened: Int = 0` → Set to 1 by lever gadget → Gate checks value
- `BossDefeated: Bool = false` → Set by death trigger → NPC dialogue branches
- `PrisonersFreed: Int = 0` → Incremented per cell opened → Quest tracks total

### 2.3 Visual Mood System

Complete render pipeline control for atmospheric effects.

#### VisualMoodDefinition

```json
{
  "$type": "TA.VisualMoodDefinition, Assembly-CSharp",

  // Ambient lighting
  "ambientColor": { "r": 0.1, "g": 0.12, "b": 0.15, "a": 1.0 },
  "ambientIntensity": 0.4,
  "ambientEquatorColor": { "r": 0.08, "g": 0.1, "b": 0.12, "a": 1.0 },
  "ambientGroundColor": { "r": 0.05, "g": 0.05, "b": 0.05, "a": 1.0 },

  // Directional light (sun/moon)
  "sunColor": { "r": 1.0, "g": 0.95, "b": 0.85, "a": 1.0 },
  "sunIntensity": 1.2,
  "sunAngle": { "x": 50, "y": 170 },
  "sunShadows": true,
  "sunShadowColor": { "r": 0.15, "g": 0.12, "b": 0.2, "a": 1.0 },

  // Fog
  "fogEnabled": true,
  "fogColor": { "r": 0.3, "g": 0.35, "b": 0.4, "a": 1.0 },
  "fogDensity": 0.02,
  "fogStartDistance": 20.0,
  "fogEndDistance": 100.0,
  "fogHeightFalloff": 0.5,
  "heightFogBaseHeight": 0.0,

  // Volumetric
  "volumetricFogEnabled": false,
  "volumetricFogScattering": 0.1,
  "volumetricFogExtinction": 0.05,

  // Post-processing
  "bloomEnabled": true,
  "bloomIntensity": 0.3,
  "bloomThreshold": 1.0,
  "vignetteEnabled": false,
  "vignetteIntensity": 0.3,
  "colorGradingEnabled": true,
  "colorGradingTemperature": -10.0,
  "colorGradingTint": 5.0,
  "colorGradingSaturation": -10.0,
  "colorGradingContrast": 15.0,

  // Sky
  "skyboxEnabled": true,
  "skyboxTintColor": { "r": 0.5, "g": 0.6, "b": 0.8, "a": 1.0 },
  "skyboxExposure": 1.0,
  "reflectionIntensity": 0.5,

  // Day/night cycle
  "dayCycleEnabled": false,
  "dayCycleSunrise": 0.25,
  "dayCycleSunset": 0.75,
  "dayCycleNightAmbientMultiplier": 0.2
}
```

**Mood categories** (observed naming patterns):
- `Mood_Interior_Dungeon_*` — Underground areas (dark, high contrast)
- `Mood_Exterior_Day_*` — Daytime outdoor (bright, warm)
- `Mood_Exterior_Night_*` — Nighttime outdoor (blue shift, low ambient)
- `Mood_Exterior_Rain_*` — Rain (desaturated, high fog)
- `Mood_Exterior_Snow_*` — Snow (cold tones, strong fog)
- `Mood_Boss_*` — Boss arena (dramatic, red-shifted)
- `Mood_Lava_*` — Volcanic (orange/red, high bloom)
- `Mood_Underdark_*` — Deep underground (purple/blue, bioluminescent)

### 2.4 Day/Night Cycle

When `dayCycleEnabled: true`:
- Time mapped to 0.0-1.0 float (0 = midnight, 0.5 = noon)
- Sun color/intensity interpolated between day and night values
- Ambient light multiplied by `dayCycleNightAmbientMultiplier`
- Point lights gain importance at night
- Creatures may have different encounter rates (undead at night)

---

## 3. Dungeon Building

### 3.1 RoomBlueprint

The fundamental building block — a grid-based room definition.

```json
{
  "$type": "TA.RoomBlueprint, Assembly-CSharp",

  "cellGrid": {
    "gridWidth": 20,
    "gridHeight": 20,
    "cells": [
      2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2,
      2, 1, 1, 1, 1, 1, 1, 1, 3, 2, 2, 1, 1, 1, 1, 1, 1, 1, 1, 2,
      2, 1, 1, 1, 1, 1, 1, 1, 1, 2, 2, 1, 1, 1, 1, 1, 1, 1, 1, 2,
      2, 1, 1, 1, 1, 1, 1, 1, 1, 2, 2, 1, 1, 1, 1, 1, 1, 1, 1, 2,
      2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2
    ]
  },

  "wallAndOpeningSets": [
    {
      "wallSkinName": "StoneDungeon",
      "openingSkinName": "StoneDungeonArch"
    }
  ],

  "groundSkinName": "StoneTiles",
  "ceilingPresence": "Present|Absent",
  "ceilingHeight": 4,

  "registeredGadgets": [
    {
      "gadgetBlueprint": "Definition:Torch_Dungeon:guid",
      "position": { "x": 5, "y": 0, "z": 2 },
      "orientation": "North|South|East|West"
    }
  ],

  "registeredProps": [
    {
      "propBlueprint": "Definition:Table_Prop:guid",
      "position": { "x": 8, "y": 0, "z": 5 },
      "orientation": "North",
      "propApparition": "Always|OnCondition"
    }
  ]
}
```

### 3.2 Cell Grid System

Each cell in the grid is an integer:
- **0** = Empty (void)
- **1** = Floor (walkable)
- **2** = Wall (solid, blocks LOS/movement)
- **3** = Opening (doorway/archway connection point)

**Grid conventions**:
- X axis = horizontal (columns)
- Z axis = vertical (rows)
- Y axis = height (multi-level via `groundLevel` and `ceilingHeight`)
- Origin (0,0) = top-left of grid
- Rooms snap to grid → openings between rooms must align

### 3.3 Multi-Floor Rooms

Rooms support vertical construction:
- `groundLevel: 0` — Basement
- `groundLevel: 1` — Ground floor
- `ceilingHeight: 4` — 4-cell (20ft) ceilings
- Stairs/ramps are prop placements bridging levels
- Height affects ranged attacks, spell LOS, and cover calculations

### 3.4 Wall/Floor Skinning

Visual appearance is data-driven through skin references:

**Wall skins**: `StoneDungeon`, `BrickDungeon`, `CaveDungeon`, `WoodDungeon`, `MetalDungeon`, `MarbleDungeon`, `IceDungeon`, `LavaDungeon`

**Floor skins**: `StoneTiles`, `DirtFloor`, `WoodFloor`, `MarbleFloor`, `CaveFloor`, `IceFloor`, `LavaFloor`, `GrassFloor`

**Opening skins**: Match wall skins, determine archway/door frame appearance.

### 3.5 PropBlueprint

Static environmental objects placed in rooms.

```json
{
  "$type": "TA.PropBlueprint, Assembly-CSharp",
  "guiPresentation": { /* name + sprite */ },
  "propType": "Furniture|Vegetation|Decoration|Light|Structure",
  "cellSize": { "x": 1, "y": 1, "z": 1 },
  "canBeRotated": true,
  "canBeMirrored": false,
  "placementMode": "Floor|Wall|Ceiling|Corner",
  "placementConstraints": {
    "requiresFloor": true,
    "requiresWall": false,
    "minimumClearance": 1
  },
  "prefabReference": "Assets/Prefabs/Props/Table_01.prefab",
  "navMeshObstacle": true,
  "blockLineOfSight": false,
  "provides": {
    "lightSource": false,
    "cover": "None|Half|ThreeQuarters|Full"
  }
}
```

**Prop categories**:
- **Furniture**: Tables, chairs, beds, shelves, altars
- **Vegetation**: Trees, bushes, mushrooms, vines
- **Decoration**: Banners, paintings, rubble, bones, crates
- **Light**: Torches, braziers, chandeliers, magical orbs
- **Structure**: Columns, staircases, bridges, platforms

### 3.6 GadgetBlueprint

Interactive objects with scripted behavior — the core of dungeon interactivity.

```json
{
  "$type": "TA.GadgetBlueprint, Assembly-CSharp",
  "gadgetType": "Door|Chest|Lever|Button|Trap|Activator|Exit|Teleporter|NPC|SpawnPoint|EncounterSpawn",

  "parameters": [
    {
      "name": "Locked",
      "type": "Bool",
      "defaultValue": "true"
    },
    {
      "name": "LockDC",
      "type": "Int",
      "defaultValue": "15"
    },
    {
      "name": "TrapType",
      "type": "String",
      "defaultValue": "FireTrap"
    },
    {
      "name": "LootPack",
      "type": "BlueprintReference",
      "defaultValue": "Definition:LootPackName:guid"
    }
  ],

  "gadgetOutputs": [
    { "name": "OnOpened", "type": "Signal" },
    { "name": "OnClosed", "type": "Signal" },
    { "name": "OnTriggered", "type": "Signal" },
    { "name": "OnDestroyed", "type": "Signal" }
  ],

  "gadgetInputs": [
    { "name": "Open", "type": "Signal" },
    { "name": "Close", "type": "Signal" },
    { "name": "Enable", "type": "Signal" },
    { "name": "Disable", "type": "Signal" }
  ]
}
```

---

## 4. Gadget Logic System

### 4.1 Activators — The Node Graph Engine

Activators are invisible gadgets that implement puzzle logic. They wire gadget outputs to inputs, creating event-driven chains.

**Architecture**:
```
[Lever] → OnActivated → [Activator: AND Gate] → OnSuccess → [Door].Open
[Button] → OnPressed → [Activator: AND Gate]
```

The activator checks its conditions and fires `OnSuccess` or `OnFailure`.

### 4.2 Activator Types

| Type | Logic | Example Use |
|---|---|---|
| AND Gate | All inputs must fire | Multi-lever puzzle |
| OR Gate | Any input fires | Alternative solution |
| Timer | Fires after delay | Timed sequence |
| Counter | Fires after N inputs | Collect N items |
| Toggle | Alternates output | On/off switch |
| Sequence | Inputs must fire in order | Simon-says puzzle |
| Variable Check | Tests location variable | State-dependent logic |
| Random | Random success/failure | Gambling mechanic |
| Skill Check | Party skill test | Investigation point |

### 4.3 Gadget-Specific Logic

#### Doors

```
Parameters: Locked(bool), LockDC(int), KeyItem(ref), DestroyDC(int)
Inputs: Open, Close, Lock, Unlock
Outputs: OnOpened, OnClosed, OnDestroyed, OnLockpicked
```

Door states: Open, Closed, Locked, Destroyed. Lockpicking uses Thieves' Tools + DEX check vs LockDC.

#### Chests / Containers

```
Parameters: Locked(bool), LockDC(int), Trapped(bool), TrapType(string),
            TrapDC(int), LootPack(ref), LootTable(ref)
Inputs: Open, Close
Outputs: OnOpened, OnTrapped, OnLooted
```

#### Traps

```
Parameters: TrapDC(int), DamageType(string), DamageDice(string),
            TriggerArea(cells), Rearmable(bool), DetectionDC(int)
Inputs: Arm, Disarm, Trigger
Outputs: OnTriggered, OnDisarmed, OnDetected
```

**Trap detection**: Passive Perception vs DetectionDC while in trigger area. Active Investigation vs same DC.

**Trap types**: PitTrap (fall damage), FireTrap (fire), PoisonTrap (poison + condition), ArrowTrap (piercing), AcidTrap (acid), LightningTrap (lightning), BladeTrap (slashing)

#### Teleporters

```
Parameters: DestinationGadget(ref), TwoWay(bool), Visual(string)
Inputs: Activate, Deactivate
Outputs: OnTeleported
```

#### Exit Points

```
Parameters: DestinationLocation(ref), DestinationEntry(string),
            RequiresKey(bool), KeyItem(ref)
Inputs: Activate
Outputs: OnUsed
```

### 4.4 Encounter Spawn Points

```
Parameters: EncounterDefinition(ref), TriggerMode(string),
            TriggerDistance(int), OneShot(bool)
Inputs: Spawn, Despawn
Outputs: OnSpawned, OnAllDefeated
```

**Trigger modes**: `Proximity` (distance-based), `Signal` (wired from another gadget), `Immediate` (on room enter), `Variable` (check location variable)

### 4.5 NPC Gadgets

```
Parameters: CharacterTemplate(ref), DialogueTree(ref),
            FactionOverride(ref), MerchantDefinition(ref),
            QuestGiver(bool), QuestDefinition(ref)
Inputs: Enable, Disable, MakeHostile, MakeFriendly
Outputs: OnInteracted, OnQuestAccepted, OnQuestCompleted
```

---

## 5. Investigation & Clue System

### 5.1 InvestigationDefinition

A murder-mystery / detective investigation framework.

```json
{
  "$type": "InvestigationDefinition, Assembly-CSharp",
  "suspects": [
    {
      "npcDefinition": "Definition:NPC_Suspect_A:guid",
      "isTraitor": true,
      "interviewDC": 15,
      "evidenceItems": [
        "Definition:Clue_Letter:guid",
        "Definition:Clue_Weapon:guid"
      ]
    },
    {
      "npcDefinition": "Definition:NPC_Suspect_B:guid",
      "isTraitor": false,
      "interviewDC": 12,
      "evidenceItems": []
    }
  ],

  "clueLocations": [
    {
      "locationGadget": "Definition:SearchableDesk:guid",
      "clueItem": "Definition:Clue_Letter:guid",
      "investigationDC": 14,
      "requiredSkill": "Investigation"
    }
  ],

  "accusationDC": 18,
  "reputationReward": 20,
  "reputationPenaltyWrongAccusation": -10,

  "difficultyMatrix": {
    "easyThreshold": 2,
    "mediumThreshold": 4,
    "hardThreshold": 6
  }
}
```

### 5.2 Evidence System

Evidence items are regular `ItemDefinition` objects tagged with `ItemFlag_Clue_Incriminating` or `ItemFlag_Clue_Exonerating`. Finding evidence changes the accusation DC:

- Each **incriminating** clue found → lowers accusation DC
- Each **exonerating** clue found → affects suspect elimination

### 5.3 Interview / Interrogation

NPC interviews use the standard dialogue system with skill check branches:
- **Insight** checks reveal deception
- **Intimidation** forces confessions
- **Persuasion** unlocks voluntary information
- **Investigation** finds physical clues in dialogue context

---

## 6. Quest & Narrative System

### 6.1 QuestTreeDefinition

```json
{
  "$type": "QuestTreeDefinition, Assembly-CSharp",

  "questStartType": "Automatic|NPC|Item|Location",
  "questCompletionType": "TurnIn|Automatic",
  "repeatable": false,

  "steps": [
    {
      "stepName": "FindTheArtifact",
      "stepType": "Main|Optional|Hidden",
      "description": "Quest/&FindArtifactDescription",
      "objectives": [
        {
          "type": "ReachLocation|KillMonster|CollectItem|TalkToNPC|Variable",
          "targetDefinition": "Definition:AncientTemple_LocationDB:guid",
          "targetCount": 1,
          "variableName": "ArtifactFound",
          "variableValue": 1
        }
      ],
      "rewards": {
        "experience": 500,
        "gold": 200,
        "reputationChanges": [
          { "faction": "Antiquarians", "change": 10 }
        ],
        "items": ["Definition:MagicSword:guid"]
      },
      "onComplete": "NextStep|CompleteQuest|Branch",
      "nextStep": "ReturnToNPC",
      "branchConditions": []
    }
  ]
}
```

### 6.2 Objective Types

| Type | Trigger | Tracking |
|---|---|---|
| ReachLocation | Enter location | Binary |
| KillMonster | Defeat creature type | Counter |
| CollectItem | Pick up specific item | Counter |
| TalkToNPC | Complete dialogue | Binary |
| Variable | Location variable reaches value | Counter |
| UseItem | Use item at location | Binary |
| SurviveEncounter | Complete encounter | Binary |

### 6.3 CharacterInteractionDefinition

Defines NPC dialogue trees with branching logic.

```json
{
  "$type": "CharacterInteractionDefinition, Assembly-CSharp",

  "interactionType": "Dialogue|Shop|QuestGiver|Trainer",

  "dialogueTree": {
    "nodes": [
      {
        "nodeId": 0,
        "speakerType": "NPC|Player",
        "text": "CharacterInteraction/&DialogueText",
        "responses": [
          {
            "text": "CharacterInteraction/&ResponseText",
            "nextNode": 1,
            "skillCheck": null,
            "requiredPersonalityFlag": "Friendliness",
            "personalityFlagWeight": 5,
            "variableSet": null
          },
          {
            "text": "CharacterInteraction/&AggressiveResponse",
            "nextNode": 2,
            "skillCheck": {
              "skill": "Intimidation",
              "dc": 14,
              "successNode": 3,
              "failureNode": 4
            },
            "requiredPersonalityFlag": "Violence",
            "personalityFlagWeight": 5
          }
        ],
        "conditions": [
          {
            "type": "QuestState|Variable|HasItem|FactionReputation",
            "parameter": "QuestName",
            "value": "Completed"
          }
        ]
      }
    ]
  }
}
```

### 6.4 Personality in Dialogue

Dialogue responses are tagged with personality flags. Characters with matching personality flags prefer those responses in:
- AI-controlled party members (auto-dialogue)
- Personality-appropriate response highlighting
- Post-dialogue personality shift

The `personalityFlagWeight` determines how strongly choosing that response shifts the character's personality.

---

## 7. Character Presentation

### 7.1 MorphotypeElementDefinition

Defines visual customization options for character creation.

```json
{
  "$type": "MorphotypeElementDefinition, Assembly-CSharp",
  "category": "FaceShape|Skin|HairShape|BeardShape|BodyDecoration|HairColor|EyeColor|Age|Voice",
  "subClassFilterTag": "Default|Dragonborn",
  "originAllowed": "All|Human|Elf|Dwarf|Halfling|HalfElf|HalfOrc",

  "bodyMeshPath": "Assets/Models/Characters/Faces/Human_Male_Face_A",
  "bodyMaterialPath": "Assets/Materials/Characters/Skin_Human_01",
  "mainColor": { "r": 0.8, "g": 0.65, "b": 0.5, "a": 1.0 },
  "secondaryColor": { "r": 0.6, "g": 0.45, "b": 0.3, "a": 1.0 }
}
```

### 7.2 Morphotype Categories

| Category | Description | Race-Gated | Example Count |
|---|---|---|---|
| FaceShape | Head mesh | Yes (race-specific faces) | ~15/race |
| Skin | Skin color/material | Yes | ~8/race |
| HairShape | Hair mesh | Yes | ~12/race |
| BeardShape | Beard mesh | Males only | ~8 (Dwarf/Human) |
| BodyDecoration | Tattoos, scars | No | ~10 |
| HairColor | Hair color palette | No | ~12 |
| EyeColor | Iris color | No | ~8 |
| Age | Wrinkle/age texture | No | ~3 |
| Voice | Voice set | Per gender | ~4/gender |

### 7.3 MonsterPresentationDefinition

Separate from `MonsterDefinition` — handles all visual/audio aspects.

```json
{
  "$type": "MonsterPresentationDefinition, Assembly-CSharp",
  "prefabReference": "Assets/Prefabs/Monsters/Wolf_Prefab",
  "maleModelScale": 1.0,
  "femaleModelScale": 0.95,
  "monsterAnimationId": "Beast_Medium",
  "hasPhantomDistortion": false,
  "customShaderColor": null,

  "attachedParticlesReference": "Assets/VFX/Monster_Eyes_Glow",
  "bestiaryAttackAnimationId": "Bite",

  "materialVariantTypes": ["SkinColor"],
  "materialVariantCount": 3,

  "surpriseAnimationId": "AlertBeast",
  "idleAnimationId": "IdleBeast"
}
```

### 7.4 CharacterTemplateDefinition

Pre-built character templates for NPCs and pre-generated characters.

```json
{
  "$type": "CharacterTemplateDefinition, Assembly-CSharp",
  "characterClass": "Definition:Rogue:guid",
  "characterSubclass": "Definition:ThiefSubclass:guid",
  "characterRace": "Definition:Elf:guid",
  "characterSubRace": "Definition:ElfHigh:guid",
  "characterBackground": "Definition:Spy:guid",
  "characterLevel": 5,
  "abilityScores": [10, 18, 12, 14, 14, 10],
  "chosenSkills": ["Stealth", "Thieves_Tools", "Acrobatics"],
  "chosenFeats": ["Ambidextrous"],
  "chosenSpells": [],
  "equipment": ["Definition:StuddedLeather:guid", "Definition:Rapier:guid"],
  "personalityFlags": {
    "Pragmatism": 30,
    "Selfishness": 20,
    "Bg_Nosy": 15
  },
  "alignment": "ChaoticNeutral",
  "deity": null
}
```

---

## 8. Audio & Atmosphere

### 8.1 MusicalStateDefinition

Controls background music state machine.

```json
{
  "$type": "MusicalStateDefinition, Assembly-CSharp",
  "stateType": "Exploration|Combat|Boss|Cutscene|Camp|Title|Victory|Defeat",
  "layers": [
    {
      "audioClipPath": "Audio/Music/Exploration_Dungeon_Base",
      "volume": 0.7,
      "fadeInTime": 2.0,
      "fadeOutTime": 1.5,
      "looping": true
    },
    {
      "audioClipPath": "Audio/Music/Exploration_Dungeon_Strings",
      "volume": 0.4,
      "triggerCondition": "CombatProximity",
      "fadeInTime": 3.0
    }
  ],
  "transitionRules": {
    "toCombat": { "crossfadeTime": 1.0, "immediate": true },
    "fromCombat": { "crossfadeTime": 3.0, "delayAfterCombat": 5.0 }
  }
}
```

**Music state machine**:
```
Exploration ←→ Combat ←→ Boss
    ↕              ↕
   Camp         Victory/Defeat
    ↕
  Cutscene
```

### 8.2 BanterDefinition

Context-triggered party dialogue lines.

```json
{
  "$type": "BanterDefinition, Assembly-CSharp",
  "banterType": "Location|Combat|Rest|Discovery|LowHealth|LevelUp|ItemFound",
  "trigger": "OnLocationEnter|OnCombatStart|OnKill|OnRest|OnItemPickup",
  "cooldownMinutes": 15,
  "lines": [
    {
      "text": "PartyBanter/&Line1",
      "speakerType": "ClassFighter|RaceElf|BackgroundSpy|AnyPartyMember",
      "personalityRequirement": "Violence",
      "emotion": "Angry|Happy|Scared|Curious|Neutral"
    }
  ],
  "conditions": {
    "locationTag": "Underground",
    "minimumPartyLevel": 3,
    "requiredPartyComposition": ["Cleric"]
  }
}
```

### 8.3 CinematicEventDefinition

Scripted camera sequences for story moments.

```json
{
  "cameraShots": [
    {
      "shotType": "CloseUp|MediumShot|WideShot|OverTheShoulder|Panoramic",
      "targetType": "NPC|PartyLeader|Location|Gadget",
      "targetReference": "Definition:NPC_Name:guid",
      "duration": 3.0,
      "cameraMovement": "Static|Pan|Orbit|Dolly|Shake",
      "transitionIn": "Cut|Fade|CrossDissolve",
      "dialogueLine": "Cinematic/&LineName"
    }
  ]
}
```

---

## 9. Rest & Travel System

### 9.1 RestActivityDefinition

```json
{
  "$type": "RestActivityDefinition, Assembly-CSharp",
  "restStage": "BeforeSleep|AfterSleep",
  "restType": "ShortRest|LongRest",

  "activityType": "Pray|Meditate|IdentifyItems|CraftItems|Train|Forage|Guard|Entertain|Heal",

  "skillCheck": "Survival",
  "difficultyClass": 12,
  "successReward": "Definition:ForageReward:guid",
  "failureConsequence": null,

  "requiredProficiency": "HerbalismKitType",
  "requiredClass": null,
  "exclusiveWithActivities": ["Guard"]
}
```

**Short Rest (1 hour)**:
- Spend Hit Dice to heal
- Recover short-rest features (Second Wind, Action Surge)
- Warlock spell slots recharge

**Long Rest (8 hours)**:
- Full HP recovery
- Recover all spell slots
- Recover all features
- Activity assignments: Guard, Pray, Meditate, Craft, etc.
- Random encounter chance during rest

### 9.2 Travel System

```json
{
  "travelPace": "Slow|Normal|Fast",
  "travelEffects": {
    "slow": {
      "speedMultiplier": 0.67,
      "stealthBonus": 5,
      "perceptionPenalty": 0,
      "forageBonus": 2
    },
    "normal": {
      "speedMultiplier": 1.0,
      "stealthBonus": 0,
      "perceptionPenalty": 0,
      "forageBonus": 0
    },
    "fast": {
      "speedMultiplier": 1.33,
      "stealthBonus": 0,
      "perceptionPenalty": -5,
      "forageBonus": -2
    }
  }
}
```

### 9.3 TravelActivityDefinition

Activities characters can perform while traveling.

```json
{
  "activityType": "Scout|Forage|Navigate|Guard|Entertain|Sing|BrewPotions|Detect|Read",
  "skillUsed": "Survival|Perception|Stealth|Performance",
  "benefitType": "ReduceEncounterChance|FindIngredients|PreventSurprise|BoostMorale",
  "exclusiveSlot": true,
  "maxAssigned": 1
}
```

**Travel encounter flow**:
1. Random roll each travel hour vs encounter chance
2. If triggered: Roll on biome-specific EncounterTableDefinition
3. Scout activity can prevent surprise round
4. Navigate activity can reduce travel time
5. Guard activity can prevent ambush

### 9.4 Food / Ration System

- Party consumes 1 ration per character per long rest
- Forage activity (Survival check) can supplement rations
- `noFoodNeeded` difficulty flag disables this
- Running out of food → Exhaustion condition

---

## 10. Calendar & Time

### 10.1 CalendarDefinition

```json
{
  "$type": "CalendarDefinition, Assembly-CSharp",
  "monthsInYear": 12,
  "daysInMonth": 30,
  "hoursInDay": 24,
  "minutesInHour": 60,

  "months": [
    { "name": "Calendar/&Month1Name", "season": "Winter" },
    { "name": "Calendar/&Month2Name", "season": "Winter" },
    { "name": "Calendar/&Month3Name", "season": "Spring" },
    { "name": "Calendar/&Month4Name", "season": "Spring" },
    { "name": "Calendar/&Month5Name", "season": "Spring" },
    { "name": "Calendar/&Month6Name", "season": "Summer" },
    { "name": "Calendar/&Month7Name", "season": "Summer" },
    { "name": "Calendar/&Month8Name", "season": "Summer" },
    { "name": "Calendar/&Month9Name", "season": "Autumn" },
    { "name": "Calendar/&Month10Name", "season": "Autumn" },
    { "name": "Calendar/&Month11Name", "season": "Autumn" },
    { "name": "Calendar/&Month12Name", "season": "Winter" }
  ],

  "startYear": 963,
  "startMonth": 3,
  "startDay": 15,

  "dayNames": ["Calendar/&Day1", "Calendar/&Day2", "Calendar/&Day3",
               "Calendar/&Day4", "Calendar/&Day5", "Calendar/&Day6",
               "Calendar/&Day7"]
}
```

**360-day year** (12 months × 30 days). 7-day weeks.

### 10.2 Time-Driven Systems

| System | Time Granularity | Trigger |
|---|---|---|
| Day/night cycle | Continuous (0.0-1.0) | Real-time during exploration |
| Combat rounds | 6 seconds/round | Turn-based |
| Short rest | 1 hour | Manual |
| Long rest | 8 hours | Manual |
| Travel | Hours per segment | Automatic |
| Merchant restock | Hours/days | Background timer |
| Spell duration | Rounds/minutes/hours | Auto-tracked |
| Condition duration | Rounds | Auto-tracked |
| Food consumption | Per long rest | Auto-tracked |

---

## 11. System Interconnection Map

This section maps how all 144 blueprint types connect across the three documents.

### 11.1 Core → Content Connections

```
FeatureDefinition ──→ ItemDefinition.staticProperties[] (enchantments)
FeatureDefinition ──→ MonsterDefinition.features[] (creature abilities)
FeatureDefinition ──→ FeatDefinition.features[] (feat grants)
FeatureDefinition ──→ FightingStyleDefinition.features[]
FeatureDefinition ──→ ConditionDefinition.features[] (condition effects)

EffectDescription ──→ SpellDefinition (spell effects)
EffectDescription ──→ FeatureDefinitionPower (ability effects)
EffectDescription ──→ MonsterAttackDefinition (attack effects)
EffectDescription ──→ ItemDefinition.weaponDefinition (weapon damage)
EffectDescription ──→ ConditionDefinition.recurrentEffectForms[]

SpellDefinition ──→ SpellListDefinition (class spell access)
SpellDefinition ──→ ConditionDefinition (buffs/debuffs via EffectForms)
SpellDefinition ──→ EffectProxyDefinition (persistent world effects)

ConditionDefinition ──→ FeatureDefinition (mechanical effects)
ConditionDefinition ──→ MonsterDefinition (via features: immunities)
ConditionDefinition ──→ ActionDefinition (via addedConditionName)
```

### 11.2 Content → World Connections

```
MonsterDefinition ──→ EncounterDefinition (creature composition)
EncounterDefinition ──→ EncounterTableDefinition (random tables)
EncounterTableDefinition ──→ LocationDefinition (location encounters)
EncounterTableDefinition ──→ TravelEventDefinition (travel encounters)

LootPackDefinition ──→ MonsterDefinition.droppedLootDefinition
LootPackDefinition ──→ GadgetBlueprint (chest contents)
TreasureTableDefinition ──→ LootPackDefinition.treasureTable

ItemDefinition ──→ RecipeDefinition (crafting input/output)
ItemDefinition ──→ MerchantDefinition.stockUnitDescriptions[]
ItemDefinition ──→ GadgetBlueprint.parameters (key items, evidence)
```

### 11.3 World Interconnections

```
CampaignDefinition ──→ LocationDefinition (via registeredNodes)
LocationDefinition ──→ RoomBlueprint[] (physical layout)
RoomBlueprint ──→ PropBlueprint[] (decoration)
RoomBlueprint ──→ GadgetBlueprint[] (interactive objects)

GadgetBlueprint ──→ GadgetBlueprint (wired input/output signals)
GadgetBlueprint ──→ EncounterDefinition (spawn triggers)
GadgetBlueprint ──→ CharacterInteractionDefinition (NPC dialogue)
GadgetBlueprint ──→ QuestTreeDefinition (quest triggers)
GadgetBlueprint ──→ LocationDefinition.registeredVariables (state)

QuestTreeDefinition ──→ LocationDefinition (reach objectives)
QuestTreeDefinition ──→ FactionDefinition (reputation rewards)
QuestTreeDefinition ──→ ItemDefinition (item rewards)

InvestigationDefinition ──→ CharacterInteractionDefinition (interviews)
InvestigationDefinition ──→ ItemDefinition (evidence items)
InvestigationDefinition ──→ GadgetBlueprint (searchable objects)
```

### 11.4 Presentation Layer

```
VisualMoodDefinition ──→ LocationDefinition (atmosphere)
MusicalStateDefinition ──→ LocationDefinition (music)
BanterDefinition ──→ PersonalityFlagDefinition (speaker filter)
MonsterPresentationDefinition ──→ MonsterDefinition (visuals)
MorphotypeElementDefinition ──→ CharacterRaceDefinition (appearance)
CalendarDefinition ──→ CampaignDefinition (time tracking)
```

### 11.5 Complete Blueprint Type Catalog (144 types)

Organized by document section:

**Core Systems (Doc 1):**
CharacterClassDefinition, CharacterSubclassDefinition, CharacterRaceDefinition, CharacterBackgroundDefinition, FeatureDefinition (22+ subtypes), SpellDefinition, SpellListDefinition, ConditionDefinition, ActionDefinition, ReactionDefinition, DecisionPackageDefinition, DecisionDefinition

**Content & Data (Doc 2):**
ItemDefinition, WeaponTypeDefinition, ArmorTypeDefinition, AmmunitionTypeDefinition, MonsterDefinition, MonsterAttackDefinition, MonsterPresentationDefinition, EncounterDefinition, EncounterTableDefinition, LootPackDefinition, TreasureTableDefinition, RecipeDefinition, MerchantDefinition, CharacterToLootPackMapDefinition, FeatDefinition, InvocationDefinition, MetamagicOptionDefinition, FightingStyleDefinition, SkillDefinition, DamageDefinition, DieTypeDefinition, LanguageDefinition, DeityDefinition, AlignmentDefinition, PersonalityFlagDefinition, CurrencyDefinition, SmartAttributeDefinition, FactionDefinition, FactionStatusDefinition, KnowledgeLevelDefinition, TerrainTypeDefinition, ArmorCategoryDefinition, ItemFlagDefinition, EffectProxyDefinition, DifficultyPresetDefinition, CharacterFamilyDefinition, CharacterSizeDefinition, CharacterTemplateDefinition, FormationDefinition

**World & Presentation (Doc 3):**
CampaignDefinition, LocationDefinition, VisualMoodDefinition, RoomBlueprint, PropBlueprint, GadgetBlueprint, TravelEventDefinition, InvestigationDefinition, QuestTreeDefinition, CharacterInteractionDefinition, MorphotypeElementDefinition, MusicalStateDefinition, BanterDefinition, CinematicEventDefinition, RestActivityDefinition, TravelActivityDefinition, CalendarDefinition, BiomeDefinition, DatabaseIndex

**Foundation types providing enums/references:**
SchoolOfMagicDefinition, ToolTypeDefinition, EquipmentSlotDefinition, WeaponCategoryDefinition, HealingFormDescription, DamageFormDescription, ConditionFormDescription, SummonFormDescription, CounterFormDescription, MotionFormDescription, LightSourceFormDescription, AlterationFormDescription, SpellSlotsFormDescription, TopologyFormDescription, ShapeChangeFormDescription, KillFormDescription, ReviveFormDescription, TemporaryHitPointsFormDescription, FeatureUnlockByLevel, EquipmentOption, StockUnitDescription, TreasureOption, EncounterOccurence, WeightedDecision, Consideration, AnimationCurve

---

*End of World & Presentation Design Document*
