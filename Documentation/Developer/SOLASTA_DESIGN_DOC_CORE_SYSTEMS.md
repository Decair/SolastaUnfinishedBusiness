# Solasta: Crown of the Magister — Core Systems Design Document

> **Purpose**: Complete reverse-engineered specification of Solasta's blueprint architecture and core game systems. Technology-agnostic — intended for recreation in any game engine.
>
> **Source**: Analysis of 144 blueprint categories from the official game data export (Unity-serialized JSON).
>
> **Companion Documents**: See also `SOLASTA_DESIGN_DOC_CONTENT_DATA.md` and `SOLASTA_DESIGN_DOC_WORLD_PRESENTATION.md`.

---

## Table of Contents

1. [Architecture Overview](#1-architecture-overview)
2. [Character System](#2-character-system)
3. [Feature Definition System](#3-feature-definition-system)
4. [Spell & Power System](#4-spell--power-system)
5. [Condition System](#5-condition-system)
6. [Action Economy](#6-action-economy)
7. [Combat AI System](#7-combat-ai-system)

---

## 1. Architecture Overview

### 1.1 Blueprint-Driven Design

Every game object in Solasta is a **definition blueprint** — a JSON-serialized C# class. The game loads these at startup into an in-memory database, indexed by type and GUID. All gameplay is data-driven: changing a blueprint changes the game without recompilation.

There are **144 distinct blueprint types** organized in a flat namespace. Each type maps to a C# class.

### 1.2 Universal Base Structure

Every blueprint shares this common envelope:

```json
{
  "$type": "TypeName, Assembly-CSharp",
  "guiPresentation": {
    "hidden": false,
    "title": "Category/&NameTitle",
    "description": "Category/&NameDescription",
    "spriteReference": {
      "m_AssetGUID": "hex-guid",
      "m_SubObjectName": "SpriteName",
      "m_SubObjectType": "UnityEngine.Sprite, UnityEngine.CoreModule"
    },
    "color": { "r": 1.0, "g": 1.0, "b": 1.0, "a": 1.0 },
    "symbolChar": "221E",
    "sortOrder": 0,
    "unusedInSolastaCOTM": false,
    "usedInValleyDLC": false
  },
  "contentCopyright": "OpenGameContent",
  "guid": "32-char-hex-string",
  "contentPack": "BaseGame",
  "name": "InternalName"
}
```

### 1.3 GUID Reference System

All cross-references between blueprints use a compound string format:

```
"Definition:InternalName:32charHexGUID"
```

Examples:
- `"Definition:ProficiencyFighterArmor:2509cbf7264d7094eb22ed9a2dd225fc"`
- `"Definition:SpellListWizard:ebbe572795f1e2e499bf55d00df9547f"`
- `"Definition:ConditionParalyzed:a775cb208dcfe8244bc724c2af40b4fa"`

This makes references both human-readable and collision-proof. The engine resolves them at load time into direct object pointers.

### 1.4 Localization System

No blueprint contains hardcoded player-visible text. All strings use localization keys:

```
"Category/&KeyNameTitle"
"Category/&KeyNameDescription"
```

**Observed categories**: `Class/&`, `Race/&`, `Subclass/&`, `Feature/&`, `Equipment/&`, `Spell/&`, `SchoolOfMagic/&`, `Travel/&`, `Map/&`, `NPC/&`, `Environment/&`, `Biome/&`, `Calendar/&`, `Investigation/&`, `CharacterInteraction/&`, `ContentPack/&`, `Campaign/&`, `Gadget/&`, `TutorialStep/&`, `PartyBanter/&`

### 1.5 Content Pack & Copyright System

Every blueprint is tagged with ownership information:

**Content Packs** (DLC gating):
- `BaseGame` — Core game content
- `LostValley` — DLC1: Valley of the Dead
- `PalaceOfIce` — DLC3: Palace of Ice
- `InnerStrength`, `PrimalCalling`, `SorcererUpdate`, `LoadedDice` — Smaller DLCs
- `BackerItems`, `DigitalBackerContent` — Backer exclusives

**Copyright Tiers** (modding/legal):
- `OpenGameContent` — Direct SRD/OGL content
- `OpenGameContentModified` — SRD-derived with modifications
- `TacticalAdventuresContent` — Original proprietary content
- `TacticalAdventuresContentHidden` — Hidden from mod tools

### 1.6 Database Index

A master `DatabaseIndex` maps every definition type to a database path:

| Database Path | Definition Type |
|---|---|
| `Database/Classes` | CharacterClassDefinition, CharacterSubclassDefinition |
| `Database/Races` | CharacterRaceDefinition |
| `Database/Spells` | SpellDefinition |
| `Database/SpellLists` | SpellListDefinition |
| `Database/Features` | FeatureDefinition (all subtypes) |
| `Database/Features/Power` | FeatureDefinitionPower |
| `Database/Monsters` | MonsterDefinition |
| `Database/MonsterAttacks` | MonsterAttackDefinition |
| `Database/Equipment` | ItemDefinition |
| `Database/Conditions` | ConditionDefinition |
| `Database/Actions` | ActionDefinition |
| `Database/Reactions` | ReactionDefinition |
| `Database/Feats` | FeatDefinition |
| `Database/FightingStyles` | FightingStyleDefinition |
| `Database/Locations` | LocationDefinition |
| `Database/AI/DecisionPackages` | DecisionPackageDefinition |
| `Database/AI/Decisions` | DecisionDefinition |
| `Database/Factions` | FactionDefinition |
| `Database/LootPacks` | LootPackDefinition |
| `Database/TreasureTable` | TreasureTableDefinition |
| `Database/Recipes` | RecipeDefinition |
| `Database/Quests` | QuestTreeDefinition |
| `Database/Blueprint/Rooms` | RoomBlueprint |
| `Database/Blueprint/Gadgets` | GadgetBlueprint |
| `Database/Blueprint/Props` | PropBlueprint |

### 1.7 Measurement Convention

**All distances are in cells**, not feet. 1 cell = 5 feet.

| Game Concept | Cells | Feet |
|---|---|---|
| Darkvision | 12 | 60 ft |
| Base walk speed | 6 | 30 ft |
| Dwarf walk speed | 5 | 25 ft |
| Fly speed (fast) | 12 | 60 ft |
| Fire Bolt range | 24 | 120 ft |
| Fireball range | 30 | 150 ft |
| Melee reach | 1 | 5 ft |

---

## 2. Character System

### 2.1 Character Composition

A player character is composed of:

```
Character = Race + Sub-Race + Class + Subclass + Background + Deity (optional)
          + Ability Scores + Skill Choices + Equipment + Personality Flags
```

Each component contributes **features** via the same mechanism: arrays of `FeatureDefinition` references.

### 2.2 CharacterClassDefinition

Defines a playable class (Fighter, Wizard, Cleric, Rogue, etc.).

**Complete field structure:**

```json
{
  "$type": "CharacterClassDefinition, Assembly-CSharp",

  // Core mechanics
  "hitDice": "D10",
  "requiresDeity": false,

  // Level progression — THE CORE SYSTEM
  "featureUnlocks": [
    {
      "$type": "FeatureUnlockByLevel, Assembly-CSharp",
      "featureDefinition": "Definition:FeatureName:guid",
      "level": 1
    }
  ],

  // Starting equipment choices
  "equipmentRows": [
    {
      "equipmentColumns": [
        {
          "equipmentOptions": [
            {
              "optionType": "Weapon|Armor|AmmoPack|StarterPack|Focus|Tool|GenericItem|WealthPile|WeaponMartialChoice|WeaponSimpleChoice|FocusArcaneChoice",
              "itemReference": "Definition:ItemName:guid",
              "defaultChoice": "Longsword",
              "number": 1
            }
          ]
        }
      ],
      "defaultColumn": 0
    }
  ],

  // AI ability score prioritization
  "abilityScoresPriority": ["Strength", "Constitution", "Dexterity", "Wisdom", "Charisma", "Intelligence"],

  // AI auto-level preferences
  "skillAutolearnPreference": ["Athletics", "Intimidation"],
  "toolAutolearnPreference": [],
  "expertiseAutolearnPreference": [],
  "featAutolearnPreference": ["Robust", "MightyBlow"],
  "invocationAutolearnPreference": [],
  "metamagicAutolearnPreference": [],

  // Personality / roleplaying
  "personalityFlagOccurences": [
    { "weight": 30, "personalityFlag": "ClassFighterFlag" },
    { "weight": 10, "personalityFlag": "GpCombat" }
  ],

  // AI combat behavior
  "defaultBattleDecisions": "Definition:CasterCombatDecisions:guid",

  // Presentation
  "classAnimationId": "Fighter",
  "vocalSpellSemeClass": "Arcana",
  "ingredientGatheringOdds": 2,

  // Bard-specific
  "isUsingMusicalInstrumentWhenCasting": false,
  // Monk-specific
  "isUsingRandomUnarmedStrikes": false,
  "randomUnarmedStrikesRangeMin": 0,
  "randomUnarmedStrikesRangeMax": 0
}
```

### 2.3 FeatureUnlockByLevel — The Progression Engine

This is the single most important data structure in the game. Every class, subclass, and race defines its gameplay through arrays of `(featureDefinition, level)` tuples.

**Feature types observed in class progressions:**

| Prefix Pattern | Purpose | Example |
|---|---|---|
| `Proficiency*SavingThrow` | Saving throw proficiencies | `ProficiencyFighterSavingThrow` |
| `Proficiency*Armor` | Armor proficiencies | `ProficiencyClericArmor` |
| `Proficiency*Weapon` | Weapon proficiencies | `ProficiencyRogueWeapon` |
| `Proficiency*Tools` | Tool proficiencies | `ProficiencyWizardTools` |
| `PointPool*SkillPoints` | Skill selection at creation | `PointPoolFighterSkillPoints` |
| `PointPool*Expertise` | Expertise choices | `PointPoolRogueExpertise` |
| `FightingStyle*` | Fighting style choice | `FightingStyleFighter` |
| `Power*` | Activated abilities | `PowerFighterSecondWind` |
| `AttributeModifier*` | Passive stat modifiers | `AttributeModifierFighterExtraAttack` |
| `CastSpell*` | Spellcasting capability | `CastSpellWizard` |
| `FeatureSet*` | Bundled feature groups | `FeatureSetAbilityScoreChoice` |
| `SubclassChoice*` | Subclass selection point | `SubclassChoiceFighterMartialArchetypes` |
| `AdditionalDamage*` | Bonus damage features | `AdditionalDamageRogueSneakAttack` |
| `ActionAffinity*` | Action economy modifiers | `ActionAffinityRogueCunningAction` |
| `SavingThrowAffinity*` | Save bonuses/evasion | `SavingThrowAffinityRogueEvasion` |
| `DieRollModifier*` | Dice roll modifications | `DieRollModifierRogueReliableTalent` |
| `Sense*` | Sensory abilities | `SenseRogueBlindsense` |
| `AutoPreparedSpells*` | Always-prepared spells | `AutoPreparedSpellsDomainLife` |

**Subclass choice levels by class:**

| Class | Subclass Choice Level |
|---|---|
| Cleric | 1 |
| Wizard | 2 |
| Fighter | 3 |
| Rogue | 3 |
| Barbarian | 3 |
| Paladin | 3 |
| Ranger | 3 |

### 2.4 Equipment Row/Column System

Starting equipment uses a row-column choice model:

- Each **row** = one equipment decision
- Each **column** within a row = one alternative (player picks one column per row)
- `defaultColumn` = which column AI selects
- When `optionType` is a "Choice" variant (e.g., `WeaponMartialChoice`), `itemReference` is null and `defaultChoice` names the default pick

### 2.5 CharacterSubclassDefinition

Subclasses are structurally simple — just a `featureUnlocks` list with GUI presentation.

```json
{
  "$type": "CharacterSubclassDefinition, Assembly-CSharp",
  "featureUnlocks": [],
  "personalityFlagOccurences": [],
  "morphotypeSubclassFilterTag": "Default"
}
```

**Key design points:**
- Subclass features use **absolute class levels**, not relative offsets
- The parent class's `SubclassChoice*` feature definition contains the list of valid subclasses
- The subclass blueprint itself does NOT contain a back-reference to its parent class
- `morphotypeSubclassFilterTag` can restrict character appearance options (always "Default" in base game)

**Subclass feature level examples:**

| Subclass | Feature Levels |
|---|---|
| MartialChampion (Fighter) | 3, 7, 10, 15 |
| DomainLife (Cleric) | 1, 1, 1, 2, 6, 8, 10 |
| TraditionShockArcanist (Wizard) | 2, 6, 10, 10, 14 |
| PathBerserker (Barbarian) | 3, 6, 10, 14 |

### 2.6 CharacterRaceDefinition

```json
{
  "$type": "CharacterRaceDefinition, Assembly-CSharp",
  "sizeDefinition": "Definition:Medium:guid",
  "defaultAlignement": "LawfulGood",
  "dualSex": true,
  "minimalAge": 50,
  "maximalAge": 350,
  "baseHeight": 0,
  "baseWeight": 0,

  "featureUnlocks": [],
  "subRaces": ["Definition:DwarfHill:guid", "Definition:DwarfSnow:guid"],

  "racePresentation": {
    "bodyAssetPrefix": "Dwarf",
    "armorAssetPrefix": "Dwarf",
    "faceShapeAssetPrefix": "Dwarf",
    "hairShapeAssetPrefix": "Dwarf",
    "beardShapeAssetPrefix": "Dwarf",
    "maleNameOptions": ["Race/&DwarfMaleName1Title"],
    "femaleNameOptions": ["Race/&DwarfFemaleName1Title"],
    "hasSurName": true,
    "surNameOptions": ["Race/&DwarfSurName1Title"],
    "availableMorphotypeCategories": ["FaceShape", "Skin", "HairShape", "BeardShape", "BodyDecoration", "HairColor", "EyeColor", "Age", "Voice"],
    "needBeard": true,
    "maleFaceShapeOptions": ["FaceShape_A", "FaceShape_B"],
    "femaleFaceShapeOptions": [],
    "maleHairShapeOptions": ["HairShape_A", "HairShape_None"],
    "maleBeardShapeOptions": ["BeardShape_A"],
    "showHelmet": true,
    "equipmentLayoutPath": "Gui/Prefabs/Equipment/MediumHeroEquipmentLayout",
    "raceAnimationTag": "Dwarf"
  },

  "inventoryDefinition": "Definition:HumanoidInventory:guid",
  "languageAutolearnPreference": ["Language_Halfling"],
  "audioRaceRTPCValue": 2.0
}
```

**All racial features are level 1.** Typical racial features:

| Feature Type | Example | Purpose |
|---|---|---|
| `MoveMode*` | `MoveModeMove5` (25ft) | Base movement speed |
| `AttributeModifier*AbilityScoreIncrease` | +2 CON for Dwarf | Racial ability score bonus |
| `DamageAffinity*` | Poison resistance | Damage resistance |
| `Proficiency*WeaponTraining` | Dwarf weapon proficiency | Weapon proficiency |
| `Sense*` | `SenseDarkvision` | Vision mode |
| `ConditionAffinity*` | `ConditionAffinityHalflingBrave` | Condition immunity |
| `DieRollModifier*` | `DieRollModifierHalfingLucky` | Dice reroll mechanic |
| `CastSpell*` | `CastSpellElfHigh` | Racial spellcasting |

### 2.7 Sub-Race System

Parent races define sub-races via a `subRaces` array. Sub-race blueprints:
- Are the same `CharacterRaceDefinition` type
- Have `subRaces: []` (no nesting)
- Provide **additional** features that stack with parent race features
- Fill in gaps the parent leaves empty (names, height/weight, darkvision)

### 2.8 CharacterBackgroundDefinition

```json
{
  "$type": "CharacterBackgroundDefinition, Assembly-CSharp",
  "requiresDeity": true,
  "hasSubtype": false,
  "banterList": "Formal",

  "features": [
    "Definition:ProficiencyAcademicSkills:guid",
    "Definition:FactionAffinityAntiquarians:guid",
    "Definition:PointPoolBackgroundLanguageChoice_two:guid"
  ],

  "equipmentRows": [],

  "staticPersonalityFlags": [
    { "weight": 30, "personalityFlag": "Bg_Nosy" }
  ],
  "optionalPersonalityFlags": [
    { "weight": 8, "personalityFlag": "Pragmatism" }
  ],
  "defaultOptionalPersonalityFlags": ["Pragmatism", "Selfishness"],
  "forbiddenAlignments": [],
  "hasBackgroundQuest": true
}
```

**Key difference from classes:** Backgrounds use a flat `features` array (no level gating) instead of `featureUnlocks`.

### 2.9 Entity Relationship Diagram

```
CharacterRaceDefinition
  ├── featureUnlocks[] (all level 1)
  ├── subRaces[] ──────────► CharacterRaceDefinition (sub-race)
  └── racePresentation (visual data)

CharacterClassDefinition
  ├── featureUnlocks[] (levels 1-20)
  │     └── includes SubclassChoice* at a specific level
  ├── equipmentRows[] (starting gear choices)
  └── AI preferences (skills, tools, feats)

CharacterSubclassDefinition
  ├── featureUnlocks[] (absolute class levels)
  └── morphotypeSubclassFilterTag

CharacterBackgroundDefinition
  ├── features[] (flat list, no levels)
  ├── equipmentRows[]
  ├── personality flags (static + optional)
  └── faction affinities

All ──► FeatureDefinition (by Definition:Name:GUID reference)
All ──► GuiPresentation (localization keys + sprite refs)
```

---

## 3. Feature Definition System

The Feature Definition system is the **heart of all game mechanics**. Every gameplay effect — from a +1 to hit to darkvision to spellcasting — is a FeatureDefinition. There are **22 subtypes**, each controlling a specific mechanic.

### 3.1 FeatureDefinitionProficiency

Grants proficiency in weapons, armor, skills, saving throws, tools, or languages.

```json
{
  "proficiencyType": "Armor|Weapon|SavingThrow|Skill|Tool|Language|FocusType",
  "proficiencies": ["LightArmorCategory", "MediumArmorCategory"],
  "forbiddenItemTags": []
}
```

**Proficiency values by type:**
- **Armor**: `LightArmorCategory`, `MediumArmorCategory`, `HeavyArmorCategory`, `ShieldCategory`
- **Weapon**: `SimpleWeaponCategory`, `MartialWeaponCategory`, or individual types (`LongswordType`, `HandaxeType`)
- **Saving Throw**: The 6 ability score names
- **Skill**: All 18 D&D 5e skills
- **Tool**: `ThievesToolsType`, `HerbalismKitType`, `EnchantingToolType`, etc.
- **Language**: `Language_Common`, `Language_Elvish`, `Language_Dwarvish`, etc.

### 3.2 FeatureDefinitionAttributeModifier

Modifies character attributes (ability scores, AC, HP, etc.).

```json
{
  "modifiedAttribute": "Constitution|ArmorClass|HitPointBonusPerLevel|...",
  "modifierOperation": "Additive|Set|ForceIfBetter|AddAbilityScoreBonus|...",
  "modifierValue": 19,
  "modifierAbilityScore": "Constitution",
  "situationalContext": "None|NotWearingArmorOrMageArmor|...",
  "minimum1": false
}
```

**modifiedAttribute values**: `Strength`, `Dexterity`, `Constitution`, `Intelligence`, `Wisdom`, `Charisma`, `ArmorClass`, `HitPointBonusPerLevel`, `Initiative`, `HealingPool`, `AbilityScoreIncrease`, `AttacksNumber`, `ProficiencyBonus`

**modifierOperation values**: `Additive`, `Multiplicative`, `Set`, `ForceIfBetter`, `SetWithDexPlusOtherAbilityScoreBonusIfBetter`, `AddAbilityScoreBonus`, `AddProficiencyBonus`, `AddHalfProficiencyBonus`, `Force`, `AddSurrogateAttribute`, `AddConditionAmount`

**situationalContext values**: `None`, `NotWearingArmor`, `NotWearingArmorOrMageArmor`, `NotWearingHeavyArmor`, `WearingShield`, `WieldingTwoHandedWeapon`, `ConsciousAllyNextToTarget`

**Examples:**
- Amulet of Health: `modifiedAttribute: "Constitution"`, `modifierOperation: "ForceIfBetter"`, `modifierValue: 19`
- Barbarian Unarmored Defense: `modifiedAttribute: "ArmorClass"`, `modifierOperation: "SetWithDexPlusOtherAbilityScoreBonusIfBetter"`, `modifierValue: 10`, `modifierAbilityScore: "Constitution"`, `situationalContext: "NotWearingArmorOrMageArmor"`
- Armor +1: `modifiedAttribute: "ArmorClass"`, `modifierOperation: "Additive"`, `modifierValue: 1`

### 3.3 FeatureDefinitionCombatAffinity

Combat-related advantages/disadvantages, AC effects, critical hit modifications.

```json
{
  "initiativeAffinity": "None|Advantage|Disadvantage",
  "attackOfOpportunityImmunity": false,
  "attackOfOpportunityOnMeAdvantageType": "None|Advantage|Disadvantage",
  "attackOnMeAdvantage": "None|Advantage|Disadvantage",
  "attackOnMeCountLimit": -1,
  "autoCritical": false,
  "criticalHitImmunity": false,
  "myAttackAdvantage": "None|Advantage|Disadvantage",
  "myAttackAffinityFilter": "Always|AllButSelf",
  "ignoreCover": false,
  "permanentCover": "None|Half|ThreeQuarters|Full",
  "myAttackModifierValueDetermination": "None|FlatValue|Die",
  "myAttackModifierSign": "Add|Substract",
  "myAttackModifierDiceNumber": 1,
  "myAttackModifierDieType": "D4",
  "myAttackDamageMultiplier": 1.0,
  "situationalContext": "None|ConsciousAllyNextToTarget|WearingShield",
  "requiredCondition": null
}
```

**Examples:**
- Pack Tactics: `myAttackAdvantage: "Advantage"`, `situationalContext: "ConsciousAllyNextToTarget"`
- Adamantine Plate: `criticalHitImmunity: true`
- Blessed: `myAttackModifierDiceNumber: 1`, `myAttackModifierDieType: "D4"`, `myAttackModifierSign: "Add"`

### 3.4 FeatureDefinitionDamageAffinity

Damage resistances, immunities, vulnerabilities, and special reactions.

```json
{
  "damageType": "DamageFire",
  "damageAffinityType": "Resistance|Immunity|Vulnerability|None",
  "savingThrowAdvantageType": "None|Advantage",
  "flatDamageReduction": 0,
  "tagsIgnoringAffinity": ["MagicalWeapon", "MagicalEffect"],
  "healsBack": false,
  "retaliateWhenHit": false,
  "retaliatePower": null,
  "knockOutAffinity": "None|ConstitutionCheck",
  "knockOutRequiredCondition": null,
  "knockOutDCAttribute": "RelentlessRageDC",
  "knockOutAddDC": 5,
  "instantDeathImmunity": false
}
```

`tagsIgnoringAffinity` allows "resistance to nonmagical bludgeoning" — resistance applies unless the damage source has the `MagicalWeapon` tag.

### 3.5 FeatureDefinitionSavingThrowAffinity

Saving throw advantages, bonuses, and special abilities.

```json
{
  "affinityGroups": [
    {
      "abilityScoreName": "Dexterity",
      "affinity": "Advantage",
      "savingThrowModifierType": "AddDice|FlatValue",
      "savingThrowModifierDiceNumber": 0,
      "savingThrowModifierDieType": "D1",
      "restrictedSchools": [],
      "restrictedSpells": []
    }
  ],
  "indomitableSavingThrows": 0,
  "canBorrowLuck": false,
  "canUseDiamondSoul": false
}
```

### 3.6 FeatureDefinitionAdditionalDamage

The most complex feature type (~40+ fields). Handles Sneak Attack, Smite, all extra damage.

```json
{
  "notificationTag": "SneakAttack",
  "limitedUsage": "None|OncePerTurn|OnceInMyTurn",
  "triggerCondition": "AlwaysActive|AdvantageOrNearbyAlly|SpellDamagesTarget|TargetHasCondition",
  "requiredProperty": "None|FinesseOrRangeWeapon|MeleeWeapon",
  "damageValueDetermination": "Die|FlatBonus|SpellLevel",
  "damageDieType": "D6",
  "damageDiceNumber": 1,
  "additionalDamageType": "Specific|SameAsBaseDamage|AncestryDamageType",
  "specificDamageType": "DamageRadiant",
  "damageAdvancement": "ClassLevel|SlotLevel|None",
  "diceByRankTable": [
    { "rank": 1, "diceNumber": 1 },
    { "rank": 3, "diceNumber": 2 }
  ],
  "hasSavingThrow": false,
  "savingThrowAbility": "Dexterity",
  "damageSaveAffinity": "None|HalfDamage|Negates",
  "conditionOperations": [
    {
      "operation": "Add",
      "conditionDefinition": "Definition:ConditionName:guid",
      "canSaveToCancel": false,
      "saveOccurence": "EndOfTurn"
    }
  ],
  "addLightSource": false
}
```

### 3.7 FeatureDefinitionAttackModifier

Modifiers to attack and damage rolls.

```json
{
  "attackRollModifierMethod": "FlatValue|None|SourceAbilityBonus",
  "attackRollModifier": 2,
  "damageRollModifierMethod": "None|FlatValue|SourceAbilityBonus",
  "damageRollModifier": 0,
  "canDualWieldNonLight": false,
  "canAddAbilityBonusToSecondary": false,
  "magicalWeapon": false,
  "followUpStrike": false
}
```

### 3.8 FeatureDefinitionAbilityCheckAffinity

Modifiers to ability checks / skill checks.

```json
{
  "affinityGroups": [
    {
      "abilityScoreName": "Strength",
      "proficiencyName": "",
      "affinity": "HalfProficiencyWhenNotProficient|Advantage|Disadvantage",
      "abilityCheckGroupOperation": "AddDie|FlatValueBonus",
      "abilityCheckModifierDiceNumber": 0,
      "abilityCheckModifierDieType": "D4",
      "lightingContext": "Irrelevant|DimLight|Darkness"
    }
  ]
}
```

### 3.9 FeatureDefinitionActionAffinity

Controls available actions — grants new actions, forbids others.

```json
{
  "allowedActionTypes": [true, true, true, true, true, true],
  "maxAttacksNumber": -1,
  "forbiddenActions": [],
  "authorizedActions": ["RecklessAttack"],
  "restrictedActions": [],
  "rechargeReactionsAtEveryTurn": false
}
```

### 3.10 FeatureDefinitionMagicAffinity

Spellcasting modifiers — concentration, spell DC, ritual casting, spell scribing.

```json
{
  "concentrationAffinity": "None|Advantage",
  "saveDCModifier": 0,
  "spellAttackModifier": 0,
  "somaticWithWeaponOrShield": false,
  "ritualCasting": "None|Prepared|Spellbook",
  "scribeAdvantageType": "None|Advantage",
  "scribeDurationMultiplier": 1.0,
  "scribeCostMultiplier": 1.0,
  "additionalScribedSpells": 0,
  "extendedSpellList": null,
  "spellImmunities": [],
  "forceHalfDamageOnCantrips": false,
  "rangeSpellNoProximityPenalty": false
}
```

### 3.11 FeatureDefinitionConditionAffinity

Immunity or advantage against specific conditions.

```json
{
  "conditionType": "ConditionCharmed|ConditionFrightened|ConditionPoisoned|...",
  "conditionAffinityType": "Immunity|Advantage|None",
  "savingThrowAdvantageType": "None|Advantage",
  "rerollSaveWhenGained": false
}
```

**All condition types**: `ConditionCharmed`, `ConditionFrightened`, `ConditionPoisoned`, `ConditionParalyzed`, `ConditionProne`, `ConditionRestrained`, `ConditionBlinded`, `ConditionDeafened`, `ConditionStunned`, `ConditionPetrified`, `ConditionExhausted`, `ConditionDiseased`, `ConditionIncapacitated`

### 3.12 FeatureDefinitionMoveMode

Grants a movement type at a specific speed.

```json
{
  "moveMode": "Walk|Fly|Swim|Climb|Burrow",
  "speed": 6
}
```

### 3.13 FeatureDefinitionMovementAffinity

Movement speed modifiers and special movement abilities.

```json
{
  "appliesToAllModes": true,
  "baseSpeedAdditiveModifier": 2,
  "heavyArmorImmunity": false,
  "immuneDifficultTerrain": false,
  "canMoveOnWalls": false,
  "fastClimber": false,
  "enhancedJump": false,
  "encumbranceImmunity": false,
  "situationalContext": "None|NotWearingHeavyArmor"
}
```

### 3.14 FeatureDefinitionSense

Vision and perception types.

```json
{
  "senseType": "NormalVision|Darkvision|SuperiorDarkvision|Blindsight|Tremorsense|Truesight|DetectInvisibility",
  "senseRange": 12,
  "stealthBreakerRange": 6
}
```

### 3.15 FeatureDefinitionPointPool

Grants selection points for ability scores, skills, spells, etc.

```json
{
  "poolType": "AbilityScore|Skill|Expertise|Cantrip|Spell|Language|Tool|FightingStyle|Feat",
  "poolAmount": 2,
  "restrictedChoices": ["Athletics", "Insight"],
  "uniqueChoices": false
}
```

### 3.16 FeatureDefinitionFeatureSet

Groups multiple features with Union (all) or Exclusion (choose one) mode.

```json
{
  "featureSet": [
    "Definition:PointPoolAbilityScoreImprovement:guid",
    "Definition:PointPoolBonusFeat:guid"
  ],
  "mode": "Union|Exclusion",
  "defaultSelection": 0,
  "uniqueChoices": false
}
```

The ASI/Feat choice at levels 4, 8, 12, 16 uses `mode: "Exclusion"`.

### 3.17 FeatureDefinitionCastSpell

The core spellcasting definition — defines everything about a class's spellcasting.

```json
{
  "spellCastingOrigin": "Class|Race|Subclass",
  "spellcastingAbility": "Intelligence|Wisdom|Charisma",
  "spellListDefinition": "Definition:SpellListWizard:guid",
  "spellKnowledge": "Spellbook|WholeList|Selection|FixedList",
  "spellReadyness": "Prepared|AllKnown",
  "spellPreparationCount": "AbilityBonusPlusLevel|AbilityBonusPlusHalfLevel|FixedNumber",
  "slotsRecharge": "LongRest|ShortRest",
  "focusType": "Arcane|Druidic|None",

  "knownCantrips": [3, 3, 3, 4, 4, 4, 4, 4, 4, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5],
  "knownSpells": [0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0],
  "scribedSpells": [6, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2],

  "slotsPerLevels": [
    { "level": 1, "slots": [2, 0, 0, 0, 0, 0, 0, 0, 0] },
    { "level": 2, "slots": [3, 0, 0, 0, 0, 0, 0, 0, 0] },
    { "level": 3, "slots": [4, 2, 0, 0, 0, 0, 0, 0, 0] }
  ]
}
```

Arrays indexed by class level (index 0 = level 1). Slot array is `[1st, 2nd, 3rd, 4th, 5th, 6th, 7th, 8th, 9th]`.

### 3.18 FeatureDefinitionAdditionalAction

Grants extra actions in a turn (Haste, Action Surge).

```json
{
  "actionType": "Main|Bonus|Move|Reaction",
  "restrictedActions": ["AttackMain", "DashMain", "DisengageMain", "HideMain", "UseItemMain"],
  "maxAttacksNumber": 1
}
```

### 3.19 FeatureDefinitionHealingModifier

Modifies healing received or administered.

```json
{
  "healingBonusDiceNumber": 0,
  "healingBonusDiceType": "D1",
  "addLevel": "None|EffectLevel|CharacterLevel",
  "healsSelfWhenCastingHealingSpell": false,
  "maximizeReceivedHealing": false,
  "cannotGainHitPoints": false,
  "advantageOnHitDieSpending": false
}
```

### 3.20 FeatureDefinitionRegeneration

Automatic HP regeneration.

```json
{
  "diceNumber": 2,
  "dieType": "D1",
  "bonus": 0,
  "tickType": "Round|Minute|Hour",
  "tickNumber": 1,
  "preventingDamages": [],
  "isActiveWhenDown": false
}
```

### 3.21 FeatureDefinitionLightSource

Grants light-producing abilities.

```json
{
  "lightSourceForm": {
    "lightSourceType": "Basic|Sun",
    "brightRange": 4,
    "dimAdditionalRange": 4,
    "color": { "r": 1.0, "g": 1.0, "b": 1.0, "a": 1.0 }
  }
}
```

### 3.22 FeatureDefinitionFightingStyleChoice

Grants a selection of fighting styles.

```json
{
  "fightingStyles": [
    "Definition:FightingStyleArchery:guid",
    "Definition:FightingStyleDefense:guid",
    "Definition:FightingStyleDueling:guid",
    "Definition:FightingStyleGreatWeapon:guid",
    "Definition:FightingStyleProtection:guid",
    "Definition:FightingStyleTwoWeapon:guid"
  ]
}
```

### 3.23 Other Feature Types (Simpler)

| Type | Purpose | Key Fields |
|---|---|---|
| `FeatureDefinitionAutoPreparedSpells` | Always-prepared spells (domain spells) | `autoPreparedSpellsGroups[]` with spell refs per level |
| `FeatureDefinitionBonusCantrips` | Grants bonus cantrips | `bonusCantrips[]` |
| `FeatureDefinitionCampAffinity` | Rest-related traits (Elf Trance) | Camp/rest behavior flags |
| `FeatureDefinitionCraftingAffinity` | Crafting bonuses | Tool proficiency/bonus |
| `FeatureDefinitionDieRollModifier` | Dice reroll mechanics (Lucky, Reliable Talent) | Reroll conditions and thresholds |
| `FeatureDefinitionEquipmentAffinity` | Equipment bonuses | Encumbrance, carry capacity |
| `FeatureDefinitionPerceptionAffinity` | Perception bonuses | Light/darkness context |
| `FeatureDefinitionSocialAffinity` | Social interaction modifiers | Faction/NPC interaction |
| `FeatureDefinitionSubclassChoice` | Subclass picker at a level | List of valid subclass refs |
| `FeatureDefinitionSummoningAffinity` | Summoning modifiers | Summoned creature bonuses |
| `FeatureDefinitionTerrainTypeAffinity` | Terrain familiarity (Ranger) | Terrain type refs |
| `FeatureDefinitionSchoolSavant` | Wizard school specialization | School + cost reduction |
| `FeatureDefinitionDeathSavingThrowAffinity` | Death save modifiers | Advantage/bonus on death saves |
| `FeatureDefinitionRestHealingModifier` | Rest healing modifiers | Hit dice healing bonus |
| `FeatureDefinitionCriticalCharacter` | Critical hit modifications | Extended crit range |
| `FeatureDefinitionMoveThroughEnemyModifier` | Move through enemies (Halfling Nimbleness) | Size-based movement rules |
| `FeatureDefinitionAncestry` | Ancestry damage types (Draconic) | Damage type selection |
| `FeatureDefinitionFactionAffinity` | Faction reputation bonuses | Faction + reputation bonus |
| `FeatureDefinitionFactionChange` | Changes faction allegiance | New faction assignment |
| `FeatureDefinitionCharacterPresentation` | Visual overrides | Animation/model changes |

---

## 4. Spell & Power System

### 4.1 SpellDefinition

```json
{
  "$type": "SpellDefinition, Assembly-CSharp",
  "schoolOfMagic": "SchoolEvocation",
  "spellLevel": 3,
  "ritual": false,
  "castingTime": "Action|Reaction|BonusAction",
  "requiresConcentration": false,
  "verboseComponent": true,
  "somaticComponent": true,
  "materialComponentType": "None|Mundane|Specific",
  "implemented": true,
  "uniqueInstance": false,

  "effectDescription": { /* see 4.3 */ }
}
```

**Spell levels**: 0 (cantrip) through 9.
**Schools**: `SchoolAbjuration`, `SchoolConjuration`, `SchoolDivination`, `SchoolEnchantment`, `SchoolEvocation`, `SchoolIllusion`, `SchoolNecromancy`, `SchoolTransmutation`

### 4.2 FeatureDefinitionPower (Class/Racial Abilities)

Powers share the same EffectDescription engine as spells but use charges instead of spell slots.

```json
{
  "$type": "FeatureDefinitionPower, Assembly-CSharp",
  "activationTime": "BonusAction|Action|NoCost",
  "rechargeRate": "AtWill|ShortRest|LongRest|ChannelDivinity|RagePoints|KiPoints|SpellSlot",
  "costPerUse": 1,
  "fixedUsesPerRecharge": 2,
  "usesDetermination": "Fixed|AbilityBonusPlusFixed|ProficiencyBonus",
  "usesAbilityScoreName": "Charisma",
  "overriddenPower": null,
  "delegatedToAction": false,

  "effectDescription": { /* identical to spell effects */ }
}
```

| Aspect | SpellDefinition | FeatureDefinitionPower |
|---|---|---|
| **Resource** | Spell slots | Charges via `rechargeRate` |
| **Scaling** | Upcast via higher slots | Usually no scaling |
| **Components** | V, S, M | None |
| **Concentration** | `requiresConcentration` | Via conditions |
| **Effect Engine** | **Identical** EffectDescription | **Identical** EffectDescription |

### 4.3 EffectDescription — The Core Effect Engine

This is the central structure shared by both Spells and Powers. It defines everything about what happens when an effect is used.

#### Targeting System

```json
{
  "rangeType": "Self|Touch|Distance|RangeHit",
  "rangeParameter": 30,
  "targetType": "Self|IndividualsUnique|Individuals|Sphere|Cube|Line|Cone|Cylinder",
  "targetParameter": 4,
  "targetParameter2": 2,
  "targetSide": "Enemy|Ally|All",
  "targetExcludeCaster": false,
  "targetFilteringMethod": "CharacterOnly|AllCharacterAndGadgets",
  "requiresVisibilityForPosition": true,
  "requiresTargetProximity": false,
  "targetProximityDistance": 6
}
```

| rangeType | Meaning | Example |
|---|---|---|
| `Self` | Affects only caster | Shield |
| `Touch` | Adjacent target | Cure Wounds |
| `Distance` | Point within range | Fireball (30 cells) |
| `RangeHit` | Ranged attack roll | Fire Bolt (24 cells) |

| targetType | Meaning | Example |
|---|---|---|
| `Self` | Caster only | Shield |
| `IndividualsUnique` | Single unique target | Fire Bolt |
| `Individuals` | Multiple individual targets | Magic Missile (3 darts) |
| `Sphere` | AoE sphere | Fireball (radius = targetParameter) |
| `Cube` | AoE cube | Thunderwave |
| `Line` | Line from caster | Lightning Bolt |
| `Cone` | Cone from caster | Burning Hands |
| `Cylinder` | Cylinder | Moonbeam |

#### Duration System

```json
{
  "durationType": "Instantaneous|Round|Minute|Hour|UntilAnyRest|UntilLongRest|Permanent|UntilTargetOrCasterDead",
  "durationParameter": 1,
  "endOfEffect": "EndOfTurn|StartOfTurn",
  "recurrentEffect": "No|OnActivation|OnTurnStart|OnTurnEnd"
}
```

#### Saving Throw System

```json
{
  "hasSavingThrow": true,
  "savingThrowAbility": "Dexterity",
  "difficultyClassComputation": "SpellCastingFeature|FixedValue|AbilityPlusFixed",
  "fixedSavingThrowDifficultyClass": 15,
  "restrictedCreatureFamilies": ["Humanoid"],
  "immuneCreatureFamilies": ["Construct", "Undead"]
}
```

### 4.4 EffectForms — The Payloads

The `effectForms` array contains the actual mechanical effects. Each form has a `formType` discriminator.

#### Damage Form

```json
{
  "formType": "Damage",
  "damageForm": {
    "diceNumber": 8,
    "dieType": "D6",
    "bonusDamage": 0,
    "damageType": "DamageFire",
    "versatile": false,
    "versatileDieType": "D10",
    "healFromInflictedDamage": "Never|Half|Full",
    "forceKillOnZeroHp": false,
    "ignoreCriticalDoubleDice": false
  },
  "savingThrowAffinity": "HalfDamage|Negates|None",
  "canSaveToCancel": false,
  "addBonusMode": "None|AbilityBonus"
}
```

**All damage types**: `DamageFire`, `DamageForce`, `DamageLightning`, `DamageAcid`, `DamageRadiant`, `DamageNecrotic`, `DamageThunder`, `DamageCold`, `DamagePoison`, `DamagePiercing`, `DamageBludgeoning`, `DamageSlashing`, `DamagePsychic`

#### Healing Form

```json
{
  "formType": "Healing",
  "healingForm": {
    "healingComputation": "Dice|Pool",
    "diceNumber": 1,
    "dieType": "D8",
    "bonusHealing": 0,
    "variablePool": false,
    "healingCap": "MaximumHitPoints"
  }
}
```

#### Condition Form

```json
{
  "formType": "Condition",
  "conditionForm": {
    "conditionDefinition": "Definition:ConditionParalyzed:guid",
    "operation": "Add|Remove",
    "applyToSelf": false
  }
}
```

#### Other Form Types

| formType | Purpose |
|---|---|
| `TemporaryHitPoints` | Grants temp HP (dice + bonus) |
| `Summon` | Summons a creature (MonsterDefinition ref, count, duration) |
| `Counter` | Counterspell / dispel mechanics |
| `Motion` | Push / pull / teleport (type, distance, direction) |
| `LightSource` | Creates light (brightness, color, range) |
| `Alteration` | Ability score changes |
| `SpellSlots` | Add / remove spell slots |
| `Topology` | Walls, barriers |
| `ShapeChange` | Polymorph / wild shape |
| `Kill` | Direct kill effects (Power Word Kill) |
| `Revive` | Resurrection mechanics |

### 4.5 EffectAdvancement — Spell Scaling

```json
{
  "effectIncrementMethod": "None|PerAdditionalSlotLevel|CasterLevelTable",
  "incrementMultiplier": 1,
  "additionalDicePerIncrement": 1,
  "additionalTargetsPerIncrement": 0,
  "additionalSummonsPerIncrement": 0,
  "additionalHPPerIncrement": 0,
  "additionalTempHPPerIncrement": 0,
  "additionalTargetCellsPerIncrement": 0
}
```

| Method | Meaning | Example |
|---|---|---|
| `None` | No scaling | Shield |
| `PerAdditionalSlotLevel` | Upcasting in higher slots | Fireball: +1d6/slot above 3rd |
| `CasterLevelTable` | Cantrip scaling by character level | Fire Bolt: +1d10 at levels 5, 11, 17 |

**How Fireball scales**: Base 8d6 at level 3. `PerAdditionalSlotLevel` + `additionalDicePerIncrement: 1` = 9d6 at 4th, 10d6 at 5th, etc.

**How Fire Bolt scales**: Base 1d10. `CasterLevelTable` + `incrementMultiplier: 5` + `additionalDicePerIncrement: 1` = 2d10 at level 5, 3d10 at level 11, 4d10 at level 17.

**How Hold Person scales**: Base 1 target. `additionalTargetsPerIncrement: 1` = +1 target per slot above 2nd.

### 4.6 SpellListDefinition

Maps spells to classes and levels.

```json
{
  "hasCantrips": true,
  "maxSpellLevel": 9,
  "spellsByLevel": [
    {
      "level": 0,
      "spells": [
        "Definition:FireBolt:guid",
        "Definition:AcidSplash:guid"
      ]
    },
    {
      "level": 1,
      "spells": [
        "Definition:MagicMissile:guid",
        "Definition:Shield:guid"
      ]
    }
  ]
}
```

**Observed spell lists**: `SpellListWizard`, `SpellListCleric`, `SpellListBard`, `SpellListDruid`, `SpellListPaladin`, `SpellListRanger`, plus NPC-specific lists.

The same spell can appear on multiple lists (same GUID). A spell's own `spellLevel` determines slot cost; the SpellList determines which class can learn it.

### 4.7 Effect Visual Pipeline

Every EffectDescription contains particle parameters for the full VFX pipeline:

| Stage | Purpose |
|---|---|
| `casterParticleReference` | VFX on caster during casting |
| `effectParticleReference` | Main projectile/beam VFX |
| `impactParticleReference` | VFX on hit/impact |
| `zoneParticleReference` | AoE zone VFX |
| `conditionStartParticleReference` | VFX when condition applied |
| `conditionParticleReference` | Ongoing condition VFX |
| `conditionEndParticleReference` | VFX when condition ends |

Speed fields: `speedType` (`Instant`, `CellsPerSeconds`, `Fixed`), `speedParameter` (projectile speed).

---

## 5. Condition System

### 5.1 ConditionDefinition

Conditions are the state machine of the combat system. Spells and powers apply them; they carry the mechanical effects via feature references.

```json
{
  "$type": "ConditionDefinition, Assembly-CSharp",
  "conditionType": "Beneficial|Detrimental",
  "parentCondition": "Definition:ConditionIncapacitated:guid",
  "conditionTags": ["Buff", "Control"],
  "allowMultipleInstances": false,

  "features": [
    "Definition:CombatAffinityBlessed:guid",
    "Definition:SavingThrowAffinityConditionBlessed:guid"
  ],

  "durationType": "Hour|Round|Minute|UntilAnyRest|Permanent",
  "durationParameter": 1,
  "turnOccurence": "EndOfTurn|StartOfTurn",

  "specialInterruptions": [],
  "interruptionRequiresSavingThrow": false,
  "cancellingConditions": [],

  "recurrentEffectForms": [],
  "effectFormsOnRemoved": [],
  "subsequentOnRemoval": null,

  "amountOrigin": "None|SourceDamage|SourceAbilityBonus|FixedAmount",
  "baseAmount": 0,

  "forceBehavior": false,
  "battlePackage": null,
  "fearSource": false
}
```

### 5.2 Conditions as Feature Containers

The `features` array holds references to FeatureDefinition objects that grant mechanical effects while the condition is active. Conditions don't directly modify stats — they delegate to features.

**ConditionBlessed** features:
- `CombatAffinityBlessed` — +1d4 to attack rolls
- `SavingThrowAffinityConditionBlessed` — +1d4 to saving throws

**ConditionParalyzed** features (inherits from ConditionIncapacitated):
- `SavingThrowAffinityConditionParalyzed` — auto-fail STR/DEX saves
- `CombatAffinityParalyzedAdvantage` — attackers have advantage
- `CombatAffinityParalyzedAutoCrit` — melee hits are auto-crits

**ConditionShielded** features:
- `AttributeModifierShielded` — AC +5
- `MagicAffinityConditionShielded` — Magic Missile immunity

### 5.3 Condition Inheritance

The `parentCondition` field creates an inheritance chain. `ConditionParalyzed` inherits from `ConditionIncapacitated`, gaining its features (can't take actions) plus adding its own (auto-fail saves, auto-crits).

### 5.4 Reactive Effects

Conditions can trigger effects when hit:

```json
{
  "additionalDamageWhenHit": true,
  "additionalDamageType": "DamageFire",
  "additionalDamageDieType": "D8",
  "additionalDamageDieNumber": 2,
  "additionalConditionWhenHit": true,
  "additionalCondition": "Definition:ConditionName:guid"
}
```

### 5.5 Recurring and Removal Effects

- `recurrentEffectForms[]` — EffectForms that repeat each round (damage over time)
- `effectFormsOnRemoved[]` — EffectForms applied when condition ends
- `subsequentOnRemoval` — Another condition applied when this one ends

---

## 6. Action Economy

### 6.1 Action Types (6 categories)

| Type | D&D 5e Equivalent | Color Code |
|---|---|---|
| `Main` | Action | Blue #889EDF |
| `Bonus` | Bonus Action | Yellow #D1D27C |
| `Reaction` | Reaction | Pink #D496C8 |
| `Move` | Movement | Green #76D8A3 |
| `FreeOnce` | Object interaction | Cyan #8AC9CE |
| `NoCost` | Free ability | Cyan #8AC9CE |

### 6.2 ActionDefinition

120+ actions defined. Each specifies its economy cost and behavior.

```json
{
  "$type": "ActionDefinition, Assembly-CSharp",
  "id": "DashMain",
  "actionType": "Main",
  "actionScope": "All|Battle",
  "pairedActionId": "DashBonus",
  "usesPerTurn": -1,
  "requiresAuthorization": false,
  "stealthBreakerBehavior": "None|RollIfTargets",
  "canTriggerBattle": false,
  "iterativeTargeting": false,
  "formType": "Large|Small|Invisible",
  "addedConditionName": "",
  "removedConditionName": "",
  "activatedPower": null
}
```

### 6.3 Key Action Design Patterns

**Paired Actions**: Many actions have Main/Bonus/Free variants linked via `pairedActionId`:
- `DashMain` ↔ `DashBonus` (Rogue Cunning Action)
- `AttackMain` ↔ `AttackOff` (dual-wielding)
- `CastMain` ↔ `CastBonus` ↔ `CastReaction` ↔ `CastNoCost`
- `ShoveMain` ↔ `ShoveBonus` ↔ `ShoveFree`

**Authorization Gating**: `requiresAuthorization: true` hides the action until a class feature grants it. This enables Cunning Action (unlock DashBonus), Reckless Attack, etc.

**Form Types**: Control UI:
- `Large` — Full targeting UI (attacks, spells)
- `Small` — Confirmation dialog (Dash, Dodge)
- `Invisible` — Auto-resolves without UI (reactions)

### 6.4 ReactionDefinition

46 reactions defined. Lightweight UI prompt definitions.

```json
{
  "$type": "ReactionDefinition, Assembly-CSharp",
  "reactTitle": "string",
  "reactDescription": "string",
  "validationDismissesSimilarReactions": false
}
```

**Categories**:
- **Attack-triggered**: OpportunityAttack, GiantKiller, Retaliation, SwirlingDance
- **Defense-triggered**: BlockAttack, UncannyDodge, LeafScales
- **Spell-triggered**: CounterSpell, BardicInspiration, BorrowLuck
- **Class-specific**: DeflectMissile, DiamondSoul, SlowFall, IndomitableResistance
- **Readied**: ReadiedAction, ReactionShot

### 6.5 Complete Action Catalog (Selected)

| Action | Type | Scope | Authorization | Purpose |
|---|---|---|---|---|
| AttackMain | Main | Battle | No | Standard attack |
| AttackOff | Bonus | Battle | No | Off-hand attack (dual wield) |
| AttackOpportunity | Reaction | Battle | No | Opportunity attack |
| CastMain | Main | All | No | Cast spell (action) |
| CastBonus | Bonus | All | No | Cast spell (bonus action) |
| CastReaction | Reaction | All | No | Cast spell (reaction) |
| CastRitual | Main | All | Yes | Ritual casting |
| DashMain | Main | Battle | No | Double movement |
| DashBonus | Bonus | Battle | Yes | Rogue Cunning Action dash |
| Dodge | Main | Battle | No | Impose disadvantage on attackers |
| DisengageMain | Main | Battle | No | Avoid opportunity attacks |
| HideMain | Main | Battle | No | Attempt to hide |
| Ready | Main | Battle | No | Ready an action |
| Shove | Main | Battle | No | Push or knock prone |
| FlurryOfBlows | Bonus | Battle | Yes | Monk bonus attacks |
| RageStart | Bonus | Battle | Yes | Barbarian rage |
| RecklessAttack | NoCost | Battle | Yes | Advantage on attacks (risk) |
| UncannyDodge | Reaction | Battle | Yes | Halve attack damage |
| WildShape | Main | Battle | Yes | Druid transform |
| StandUp | Move | Battle | No | Stand from prone |

---

## 7. Combat AI System

### 7.1 Architecture: Weighted Utility AI

The AI uses a **utility-based scoring system**, not behavior trees.

```
Each Turn:
  1. Evaluate ALL weightedDecisions in creature's DecisionPackage
  2. For each decision:
     a. Score = product of all consideration outputs × decision weight
     b. Skip if on cooldown
  3. Select highest-scoring decision
  4. Execute the decision's activity
```

### 7.2 DecisionPackageDefinition

Complete AI behavior for a creature, composed of weighted decision references.

```json
{
  "$type": "TA.AI.DecisionPackageDefinition, Assembly-CSharp",
  "package": {
    "weightedDecisions": [
      {
        "decision": "Definition:DecisionName:guid",
        "weight": 5.0,
        "cooldown": 0,
        "dynamicCooldown": false
      }
    ]
  }
}
```

### 7.3 DecisionDefinition

A single possible action the AI could take.

```json
{
  "decision": {
    "description": "Human-readable description",
    "scorer": {
      "scorer": {
        "considerations": [
          {
            "consideration": {
              "considerationType": "CanAttack",
              "curve": { /* AnimationCurve */ },
              "floatParameter": 0.0,
              "intParameter": 0,
              "byteParameter": 1,
              "boolParameter": false
            },
            "weight": 1.0
          }
        ]
      }
    },
    "activityType": "Attack|CastMagic|BreakFree|Move",
    "stringParameter": "",
    "enumParameter": 1
  }
}
```

### 7.4 Consideration Types

| Type | Purpose |
|---|---|
| `CanAttack` | Can I attack this target? |
| `TargetThreat` | How threatening is the target? |
| `CanCastMagic` | Can I cast a specific spell? |
| `TargetHasCondition` | Does target have a condition? |
| `DistanceFromMe` | How far is the target? |
| `Random` | Random chance |
| `ActionTypeStatus` | What actions have been used? |
| `CanMoveToAnyEnemyInOneTurn` | Pathfinding check |

The `curve` (AnimationCurve) maps raw input values to 0.0-1.0 utility scores, enabling non-linear responses.

### 7.5 Threat Evaluator

Each monster has a named threat evaluator controlling target selection:

```json
{
  "threatEvaluator": {
    "hateResponse": { /* AnimationCurve */ },
    "woundRatioResponse": { /* AnimationCurve */ },
    "maxHPResponse": { /* AnimationCurve */ },
    "attackersCountResponse": { /* AnimationCurve */ },
    "meleeVsRangedResponse": { /* AnimationCurve */ },
    "previousTargetRelatedMalus": 0.1
  }
}
```

| Evaluator | Behavior |
|---|---|
| `PrioritizeNooneButFocus` | No priority, sticks to current target |
| `PrioritizeDamageDealers` | Targets highest DPS party members |
| `PrioritizeNoone` | No inherent target preference |

`previousTargetRelatedMalus` controls target switching — higher values discourage switching.

### 7.6 AI Package Examples

**Wolf (simple melee):**

| Weight | Decision | Purpose |
|---|---|---|
| 5.0 | LongRangePathToEnemy_Dash | Close distance |
| 3.0 | MeleeAttack_Default | Bite attack |
| 2.0 | Move_AggressiveSingleTarget | Position |

**Dragon (boss):**

| Weight | Decision | Cooldown | Purpose |
|---|---|---|---|
| 6.0 | CastMagic_DPS_AoE_DragonBreath | 2 (dynamic) | Breath weapon (highest priority, 2-turn CD) |
| 5.0 | Move_PrepareDragonBreath_Unsafe | 2 (dynamic) | Position for breath |
| 5.0 | CastMagic_FrightfulPresence_Dragon | 99 | Frighten (one-time use) |
| 3.0 | MeleeAttack_Default | 0 | Claw/bite |
| 3.0 | RangedAttack_Default | 0 | Tail attack |
| 1.5 | Move_Dragon_Aggressive | 0 | Reposition |
| 0.05 | Emote_Angry | 1 | Flavor roar |

**Bandit (versatile fighter):**

| Weight | Decision | Cooldown | Purpose |
|---|---|---|---|
| 5.0 | LongRangePathToEnemy_Dash | 0 | Close distance |
| 5.0 | CastMagic_Heal_SingleTarget | 0 | Heal allies (equal priority to engaging!) |
| 4.0 | ShoveDown_Default | 3 | Knock prone (3-turn CD prevents spam) |
| 3.0 | MeleeAttack_Default | 0 | Melee attack |
| 2.5 | CastMagic_ActionSurge_Self | 0 | Action surge |
| 1.0 | RangedAttack_Default | 0 | Ranged fallback |

---

## Appendix A: Complete Enum Reference

### Ability Scores
`Strength`, `Dexterity`, `Constitution`, `Intelligence`, `Wisdom`, `Charisma`

### Damage Types
`DamageAcid`, `DamageBludgeoning`, `DamageCold`, `DamageFire`, `DamageForce`, `DamageLightning`, `DamageNecrotic`, `DamagePiercing`, `DamagePoison`, `DamagePsychic`, `DamageRadiant`, `DamageSlashing`, `DamageThunder`

### Die Types
`D1` (flat value), `D4`, `D6`, `D8`, `D10`, `D12`, `D20`

### Movement Modes
`Walk`, `Fly`, `Swim`, `Climb`, `Burrow`

### Sense Types
`NormalVision`, `Darkvision`, `SuperiorDarkvision`, `Blindsight`, `Tremorsense`, `Truesight`, `DetectInvisibility`

### Condition Types
`ConditionCharmed`, `ConditionFrightened`, `ConditionPoisoned`, `ConditionParalyzed`, `ConditionProne`, `ConditionRestrained`, `ConditionBlinded`, `ConditionDeafened`, `ConditionStunned`, `ConditionPetrified`, `ConditionExhausted`, `ConditionDiseased`, `ConditionIncapacitated`, `ConditionInvisible`

### Magic Schools
`SchoolAbjuration`, `SchoolConjuration`, `SchoolDivination`, `SchoolEnchantment`, `SchoolEvocation`, `SchoolIllusion`, `SchoolNecromancy`, `SchoolTransmutation`

### Content Packs
`BaseGame`, `LostValley`, `PalaceOfIce`, `InnerStrength`, `PrimalCalling`, `SorcererUpdate`, `LoadedDice`, `BackerItems`, `DigitalBackerContent`

### Content Copyright
`OpenGameContent`, `OpenGameContentModified`, `TacticalAdventuresContent`, `TacticalAdventuresContentHidden`
