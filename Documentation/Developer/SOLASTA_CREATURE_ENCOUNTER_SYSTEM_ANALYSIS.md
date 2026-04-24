# Solasta CRPG - Creature, Encounter & AI System Analysis

## Document Purpose
Complete reverse-engineering of the Solasta blueprint system for monsters, NPCs, encounters, and combat AI. Based on analysis of actual exported JSON blueprints from the game's data files.

---

## 1. ARCHITECTURE OVERVIEW

Solasta uses a **Unity-serialized JSON blueprint system** where every game definition is a C# class serialized to JSON. All blueprints share a common base with:

```
{
  "$type": "TypeName, Assembly-CSharp",   // C# class type
  "guiPresentation": { ... },             // Display info (title, description, sprite)
  "contentCopyright": "...",              // Content licensing flag
  "guid": "32-char-hex",                 // Unique identifier
  "contentPack": "BaseGame|LostValley",  // DLC source
  "name": "InternalName"                 // String key for cross-referencing
}
```

### Cross-Reference Pattern
All inter-blueprint references use the format:
```
"Definition:Name:guid"
```
Example: `"Definition:Attack_Wolf_Bite:0931068676ce4db45a285c5a914ac38a"`

This is the universal linkage pattern across the entire engine. Every feature, attack, spell, condition, faction, size, AI package, etc. is referenced this way.

---

## 2. MONSTER DEFINITION (MonsterDefinition)

The core creature stat block. This is the largest and most complex blueprint type.

### 2.1 Complete Field Structure

```jsonc
{
  "$type": "MonsterDefinition, Assembly-CSharp",

  // --- TAXONOMY ---
  "characterFamily": "Beast",              // Links to CharacterFamilyDefinition (see section 8)
  "creatureTags": [],                      // Additional classification tags
  "defaultFaction": "HostileMonsters",     // Faction name (string, not Definition: ref)
  "sizeDefinition": "Definition:Small:...", // Links to CharacterSizeDefinition
  "alignment": "Unaligned",               // D&D alignment string

  // --- CORE COMBAT STATS (D&D 5e mapping) ---
  "armorClass": 13,                        // AC (flat value, no formula breakdown)
  "armor": "",                             // Armor type reference (if worn)
  "hitDice": 2,                            // Number of hit dice
  "hitDiceType": "D8",                     // Hit die size (D6, D8, D10, D12)
  "hitPointsBonus": 2,                     // Additional flat HP (CON modifier * hitDice)
  "standardHitPoints": 11,                 // Pre-calculated average HP
  "challengeRating": 0.25,                 // CR (supports decimals: 0.125, 0.25, 0.5)

  // --- ABILITY SCORES ---
  // Array of 6 values: [STR, DEX, CON, INT, WIS, CHA]
  "abilityScores": [12, 15, 12, 3, 12, 6],

  // --- SAVING THROW PROFICIENCIES ---
  "savingThrowScores": [
    {
      "abilityScoreName": "Dexterity",
      "bonus": 8                           // Total save bonus (not just proficiency)
    }
  ],

  // --- SKILL PROFICIENCIES ---
  "skillScores": [
    {
      "skillName": "Stealth",
      "bonus": 4                           // Total skill bonus
    }
  ],

  // --- LEGENDARY CREATURE SUPPORT ---
  "legendaryCreature": false,
  "maxLegendaryResistances": 3,
  "maxLegendaryActionPoints": 3,
  "differentActionEachTurn": false,        // Forces variety in legendary actions
  "legendaryActionOptions": [              // See section 2.3
    {
      "cost": 1,                           // Legendary action point cost
      "subaction": "MonsterAttack|Power|Spell",
      "monsterAttackDefinition": "Definition:...",
      "featureDefinitionPower": "Definition:...",
      "spellDefinition": "Definition:...",
      "magicAttackBonus": 5,
      "saveDC": 13,
      "magicAbilityBonus": 3,
      "canMove": true,                     // Can move as part of this legendary action
      "moveMode": "Definition:MoveModeFly6:...",
      "noOpportunityAttack": true,         // Movement doesn't provoke
      "decisionPackage": "Definition:..."  // AI for when to use this
    }
  ],

  // --- AI SYSTEM REFERENCES ---
  "defaultBattleDecisionPackage": "Definition:WolfCombatDecisions:...",
  "threatEvaluatorDefinition": {
    "threatEvaluator": {
      "hateResponse": { /* AnimationCurve */ },
      "woundRatioResponse": { /* AnimationCurve */ },
      "maxHPResponse": { /* AnimationCurve */ },
      "attackersCountResponse": { /* AnimationCurve */ },
      "meleeVsRangedResponse": { /* AnimationCurve */ },
      "previousTargetRelatedMalus": 0.1
    },
    "name": "PrioritizeNooneButFocus"      // Named threat strategy
  },

  // --- FEATURES (abilities, immunities, senses, movement) ---
  // Array of Definition: references to FeatureDefinition subtypes
  "features": [
    "Definition:SenseNormalVision:...",
    "Definition:MoveModeMove8:...",
    "Definition:CombatAffinityPackTactics:...",
    "Definition:DamageAffinityPoisonImmunity:...",
    "Definition:ConditionAffinityProneImmunity:...",
    // ...
  ],

  // --- ATTACK ITERATIONS (Multiattack) ---
  "attackIterations": [
    {
      "number": 1,                         // How many times this attack is used per round
      "monsterAttackDefinition": "Definition:Attack_Wolf_Bite:..."
    }
  ],

  // --- LANGUAGES ---
  "languages": ["Language_Common", "Language_Elvish"],

  // --- MOVEMENT STANCES ---
  "sneakStance": "Stealth",
  "patrolStance": "Run",                  // Options: SlowWalk, Walk, Run, Stealth
  "interceptStance": "Run",

  // --- LOOT ---
  "droppedLootDefinition": "Definition:...",
  "stealableLootDefinition": null,
  "bestiaryLootOptions": [],
  "noExperienceGain": false,

  // --- PHYSICAL TRAITS ---
  "dualSex": true,
  "minimalAge": 20,
  "maximalAge": 100,
  "height": 4,
  "weight": 100,

  // --- FLAGS ---
  "isHusk": false,                         // Animated corpse / lifeless
  "isPet": false,                          // Player-controlled pet
  "forbidFastTravel": false,               // Prevents fast travel when present
  "forceHasComplexActions": false,          // Can use complex actions
  "forcePersistentBody": false,             // Body persists after death
  "isUnique": false,                       // Named unique creature
  "uniqueNameId": "",
  "groupAttacks": false,                   // Whether attacks are grouped in UI

  // --- ANIMATION FLAGS ---
  "forceCombatStartsAnimation": false,
  "hasLookAt": false,
  "forceNoFlyAnimation": false,
  "followFloorAngle": true,                // Creature follows terrain slope

  // --- BESTIARY ---
  "bestiaryEntry": "Full",                // "Full", "None", "Reference"
  "bestiarySpriteReference": { /* AssetRef */ },
  "bestiaryCameraOffset": { "x": 0.0, "y": 7.0, "z": -35.0 },

  // --- DUNGEON MAKER ---
  "dungeonMakerPresence": "Monster",       // "Monster", "NPC", "None"
  "overrideSpawnDecision": null,

  // --- PRESENTATION (inline or external) ---
  "monsterPresentation": { /* see section 4 */ },

  // --- AUDIO ---
  "audioSwitches": [ /* Wwise switches for voice, armor sounds */ ],
  "audioSwitchesOnHands": [ /* Wwise switches for weapon sounds */ ],
  "audioRaceRTPCValue": 11.0              // Audio parameter value
}
```

### 2.2 D&D 5e Stat Block Mapping

| D&D 5e Concept | Solasta Field | Notes |
|---|---|---|
| Creature Type | `characterFamily` | "Beast", "Humanoid", "Undead", etc. |
| Size | `sizeDefinition` | Reference to CharacterSizeDefinition |
| Alignment | `alignment` | "LawfulGood", "ChaoticEvil", "Unaligned", etc. |
| Armor Class | `armorClass` | Flat integer |
| Hit Points | `standardHitPoints` | Average HP |
| Hit Dice | `hitDice` + `hitDiceType` | e.g., 2d8 |
| HP Bonus | `hitPointsBonus` | CON mod * hit dice |
| Ability Scores | `abilityScores[0-5]` | [STR, DEX, CON, INT, WIS, CHA] |
| Saving Throws | `savingThrowScores[]` | Total bonus per ability |
| Skills | `skillScores[]` | Total bonus per skill |
| Challenge Rating | `challengeRating` | Float (supports 0.125, 0.25, 0.5) |
| Damage Immunities | Features with "DamageAffinity...Immunity" | |
| Damage Resistances | Features with "DamageAffinity...Resistance" | |
| Condition Immunities | Features with "ConditionAffinity...Immunity" | |
| Senses | Features with "Sense..." | Darkvision, Tremorsense, etc. |
| Movement | Features with "MoveMode..." | Multiple modes possible (walk + fly) |
| Special Abilities | Features array | Pack Tactics, Keen Hearing, etc. |
| Multiattack | `attackIterations[]` with `number > 1` or multiple entries | |
| Languages | `languages[]` | String array |
| Legendary Actions | `legendaryActionOptions[]` | With cost, type, and AI package |

### 2.3 Sample Monsters by CR

**Wolf (CR 0.25)** - Simple beast:
- Stats: STR 12, DEX 15, CON 12, INT 3, WIS 12, CHA 6
- AC 13, HP 11 (2d8+2)
- 1 attack: Bite
- Features: Pack Tactics, Keen Hearing, No Vault/Climb
- AI: WolfCombatDecisions
- Family: Beast, Size: Small, Faction: HostileMonsters

**Air Elemental (CR 5)** - Complex creature with immunities:
- Stats: STR 14, DEX 20, CON 14, INT 6, WIS 10, CHA 6
- AC 15, HP 90 (12d10+24)
- 2x Slam attacks per round
- Features: Fly 12, Walk 10, Darkvision, 9 condition immunities, 3 damage resistances (B/P/S), lightning resistance, poison immunity
- AI: AirElementalCombatDecisions
- Family: Elemental, Size: Large

**Aksha (CR 7)** - Boss with legendary actions:
- Stats: STR 18, DEX 18, CON 18, INT 18, WIS 12, CHA 18
- AC 16, HP 85 (10d8+40), Legendary (3 resistances, 3 action points)
- Saves: DEX +8, WIS +5, CHA +8
- Skills: Perception +5, Intimidation +8
- 2 attacks: Dagger + Defiler Bite
- 4 Legendary Actions: Bite+Move, Darkness, Slow spell, Eat Friends (life leech)
- Features: Fly 8, Walk 6, Superior Darkvision, Tremorsense 16, Light Hypersensitivity, multiple resistances
- Languages: Common, Elvish, Tirmarian, Dwarvish, Goblin
- Family: Humanoid, Alignment: Lawful Evil

---

## 3. MONSTER ATTACK DEFINITION (MonsterAttackDefinition)

Defines a single attack action. Each monster references one or more of these.

### 3.1 Complete Field Structure

```jsonc
{
  "$type": "MonsterAttackDefinition, Assembly-CSharp",

  // --- ATTACK PROPERTIES ---
  "actionType": "Main",                  // "Main" or "Bonus"
  "toHitBonus": 8,                       // Total attack roll modifier
  "proximity": "Melee",                  // "Melee" or "Range"
  "reachRange": 1,                       // Melee reach in cells
  "closeRange": 30,                      // Short range (for ranged)
  "maxRange": 120,                       // Long range (for ranged)
  "magical": false,                      // Counts as magical for resistance bypass
  "afterChargeOnly": false,              // Only available after charge
  "limitedUse": false,                   // Has use limit per encounter
  "maxUses": 1,                          // Max uses if limited

  // --- EFFECT DESCRIPTION (the core damage/condition payload) ---
  "effectDescription": {
    "durationType": "Instantaneous",      // or "Round", "Minute", etc.
    "durationParameter": 1,
    "hasSavingThrow": false,
    "savingThrowAbility": "Constitution",
    "fixedSavingThrowDifficultyClass": 12,
    "targetSide": "Enemy",

    // --- EFFECT FORMS (multiple damage types + conditions) ---
    "effectForms": [
      {
        "formType": "Damage",             // "Damage", "Condition", "Healing", etc.
        "damageForm": {
          "diceNumber": 2,
          "dieType": "D8",                // D4, D6, D8, D10, D12
          "bonusDamage": 5,               // Flat bonus
          "damageType": "DamageBludgeoning", // See damage type list
          "versatile": false,
          "healFromInflictedDamage": "Never", // "Never", "Half", "Full"
          "forceKillOnZeroHp": false,
          "ignoreCriticalDoubleDice": false
        }
      },
      {
        "formType": "Condition",          // Apply a condition
        "hasSavingThrow": true,
        "savingThrowAffinity": "Negates", // "None", "Negates", "HalfDamage"
        "conditionForm": {
          "conditionDefinition": "Definition:ConditionGrappledRestrainedRemorhaz:...",
          "operation": "Add"              // "Add" or "Remove"
        }
      }
    ],

    // --- PARTICLE EFFECTS ---
    "effectParticleParameters": {
      "casterParticleReference": { /* AssetRef */ },
      "impactParticleReference": { /* AssetRef */ }
      // ... many particle slots for different effect phases
    }
  }
}
```

### 3.2 Damage Types in the System

From analyzed attacks, the following damage type strings are used:
- `DamageBludgeoning` - Physical: clubs, slams, falls
- `DamagePiercing` - Physical: bites, arrows, stabs
- `DamageSlashing` - Physical: claws, swords
- `DamageFire` - Elemental
- `DamageCold` - Elemental
- `DamageLightning` - Elemental
- `DamageThunder` - Elemental
- `DamageAcid` - Elemental
- `DamagePoison` - Elemental
- `DamageNecrotic` - Magical
- `DamageRadiant` - Magical
- `DamageForce` - Magical
- `DamagePsychic` - Magical

### 3.3 Multi-Damage Attacks

The Ancient Remorhaz Bite demonstrates how complex attacks work:
- **toHitBonus**: +13
- **Effect Form 1**: 6d10+7 Piercing damage
- **Effect Form 2**: 3d6 Fire damage (secondary damage on same hit)
- **Effect Form 3**: Condition - ConditionGrappledRestrainedRemorhaz (DEX save DC 17, Negates)

This shows that a single attack can deal multiple damage types AND apply conditions, all gated by the same attack roll, with individual saving throws per effect form.

---

## 4. MONSTER PRESENTATION DEFINITION (MonsterPresentationDefinition)

Handles visual representation. There are two patterns:

### 4.1 Inline Presentation (embedded in MonsterDefinition)

```jsonc
"monsterPresentation": {
  "useHumanoidMonsterPresentationName": false,
  "humanoidMonsterPresentationDefinitions": [],  // List of variant appearances
  "malePrefabReference": { "m_AssetGUID": "..." },
  "maleModelScale": 0.655,
  "femalePrefabReference": { "m_AssetGUID": "..." },
  "femaleModelScale": 0.655,
  "wieldedItemsScale": 1.0,
  "hideWieldedItemsWhenPassive": false,
  
  // Shader effects
  "useCustomMaterials": false,
  "hasPhantomDistortion": true,            // Ghost-like visual effect
  "hasPhantomFadingFeet": true,
  "hasPhantomVertexAnimation": true,
  "overrideCharacterShaderColors": false,
  
  // Attached particles (aura effects)
  "attachedParticlesReference": { "m_AssetGUID": "..." },
  
  // Portrait camera setup
  "canGeneratePortrait": true,
  "hasMonsterPortraitBackground": true,
  "portraitCameraFollowOffset": { "x": 0.0, "y": -0.5, "z": -12.0 },
  "portraitCameraLookAtScreenOffset": { "x": 0.5, "y": 0.5 },
  "portraitCameraFOV": 20.0
}
```

### 4.2 External Presentation Definition

For variant appearances (e.g., albino minotaurs):
```jsonc
{
  "$type": "MonsterPresentationDefinition, Assembly-CSharp",
  "prefabReference": { "m_AssetGUID": "..." },
  "sex": "Female",
  "modelScale": 0.5,
  "useCustomMaterials": true,
  "customMaterials": [
    { "m_AssetGUID": "..." },   // Material 1
    { "m_AssetGUID": "..." }    // Material 2
  ]
}
```

Humanoid monsters (like Aksha) use `humanoidMonsterPresentationDefinitions` to reference named NPC appearance definitions, allowing for equipment-wearing, race-based models.

---

## 5. ENCOUNTER DEFINITION (EncounterDefinition)

Defines a specific combat encounter with creature composition.

### 5.1 Complete Field Structure

```jsonc
{
  "$type": "EncounterDefinition, Assembly-CSharp",
  "dungeonMakerPresence": true,            // Available in dungeon maker
  "type": "Battle",                        // Encounter type
  "challengeRating": 10,                   // Overall encounter CR

  // --- LOCATION CONSTRAINTS ---
  "locationOverride": "Definition:...",    // Force specific battle map
  "locationBlacklist": [                   // Maps this encounter cannot use
    "Definition:..._Bridge_LocationDB:..."
  ],

  // --- MONSTER COMPOSITION ---
  "monsterOccurences": [
    {
      "monsterDefinition": "Definition:Giant_Ape:...",
      "number": 1,                         // Count of this monster type
      "encounterPlacementDecision": "Definition:EncounterSpawn_PackSmall_PenalizeAltitude:...",
      "creatureSex": "Male",
      "presentationDefinitionIndex": 0,    // Which visual variant to use
      "randomHumanoidPresentation": false,  // Randomize humanoid appearance
      "overrideFaction": false,            // Override default faction
      "factionName": ""                    // Custom faction if overridden
    },
    {
      "monsterDefinition": "Definition:Badlands_Ape_MonsterDefinition:...",
      "number": 4,
      "encounterPlacementDecision": "Definition:EncounterSpawn_PackSmall_PenalizeAltitude:...",
      // ...
    },
    {
      "monsterDefinition": "Definition:Ape_Range_MonsterDefinition:...",
      "number": 5,
      // ...
    }
  ]
}
```

### 5.2 Encounter Design Patterns

**Simple encounters** (CR10_CrimsonSpiders_x2):
- Single monster type, quantity 2
- Placement: `EncounterSpawn_Spread` (spread out across arena)

**Complex encounters** (CR10_DLC1_ApeKing):
- Mixed creature groups: 1 Giant Ape + 4 Badlands Apes + 5 Ranged Apes
- Location override to jungle road
- Location blacklist excluding bridges
- Placement: `EncounterSpawn_PackSmall_PenalizeAltitude` (grouped, avoid height differences)

**Naming convention**: `CR[rating]_[description]_x[counts]`
Example: `CR3_GoblinWolfBand_x1_x1_x2_x1` = CR3, Goblin Wolf Band, 1+1+2+1 creatures

### 5.3 Placement Decisions

Referenced by `encounterPlacementDecision`, these control spawn positioning:
- `EncounterSpawn_Spread` - Distribute across the battle area
- `EncounterSpawn_PackSmall_PenalizeAltitude` - Group together, prefer flat ground
- Others exist for flanking, ambush, etc.

---

## 6. ENCOUNTER TABLE DEFINITION (EncounterTableDefinition)

Random encounter tables that select from a pool of EncounterDefinitions.

### 6.1 Structure

```jsonc
{
  "$type": "EncounterTableDefinition, Assembly-CSharp",
  "dungeonMakerPresence": true,
  "encounterOccurences": [
    {
      "weight": 1,
      "encounterDefinition": "Definition:CR2_ApprenticeNecromancer_x1_x3:..."
    },
    {
      "weight": 1,
      "encounterDefinition": "Definition:CR2_GoblinScouts_x2_x1_x1:..."
    },
    // ... many more entries
  ]
}
```

### 6.2 Design Analysis

**AridBadlandsEncounter** table contains encounters ranging from CR2 to CR10+:
- All entries have `weight: 1` (equal probability)
- Encounters span multiple CRs within one table
- The table is NOT CR-filtered - the game engine must filter by party level at runtime

**Encounter variety within one table** (AridBadlands example):
- CR2: Necromancers, Goblins, Bandits, Ogres, Orcs, Wolves, Drake
- CR3: Spiders, Eagles, Goblins with wolves, Minotaur, Veterans
- CR4: Bandits, Berserkers, Dire Wolves, Crusaders, Slavers
- CR5: Berserkers, Cutthroats, Shamans, Fanatics
- CR6: Fanatics, Deep Spiders, Slavers, Elementals, Orc leaders
- CR7: Knights, Berserker Hunters, Orc Raiders, Young Dragons
- CR8: Assassins, Beast Lords, War Parties, Young Dragons
- CR9+: Lost Crusaders, higher threats

**Biome-specific tables**: Each region has its own table:
- `AridBadlandsEncounter` - Desert/wasteland
- `DLC1_JungleEncounterTable` - Tropical jungle
- `DLC1_SwampEncounterTable` - Swamp biome
- `DLC1_MarchesEncounterTable` - Border marshlands
- `DLC1_CityEncounterTable` - Urban encounters

---

## 7. AI DECISION SYSTEM

The AI system uses a **weighted utility-based decision tree** with two layers:

### 7.1 Layer 1: DecisionDefinition (Individual Decision Nodes)

Each decision node represents ONE possible action the AI could take.

```jsonc
{
  "$type": "TA.AI.DecisionDefinition, Assembly-CSharp",
  "decision": {
    "description": "Human-readable description of behavior",
    
    // --- SCORER (utility function) ---
    "scorer": {
      "scorer": {
        "considerations": [
          {
            "consideration": {
              "considerationType": "CanAttack",    // Evaluation function name
              "curve": { /* AnimationCurve */ },   // Response curve
              "stringParameter": "",
              "floatParameter": 0.0,
              "intParameter": 0,
              "byteParameter": 1,                  // Sub-parameter
              "boolParameter": false,
              "boolSecParameter": false,
              "boolTerParameter": false
            },
            "name": "CanAttack_PreferAdvantage",
            "weight": 1.0                          // Consideration weight
          },
          {
            "consideration": {
              "considerationType": "TargetThreat",
              // ...
            },
            "name": "TargetHasHighThreat",
            "weight": 1.0
          }
        ]
      },
      "name": "MeleeAttackScorer"
    },

    // --- ACTIVITY ---
    "activityType": "Attack",              // What the AI actually does
    "stringParameter": "",                 // Spell name, etc.
    "floatParameter": 0.0,
    "enumParameter": 1                     // Activity sub-type
  }
}
```

### 7.2 Consideration Types (Observed)

These are the evaluation functions the AI uses:

| ConsiderationType | Purpose | Parameters |
|---|---|---|
| `CanAttack` | Can I attack this target? | byteParameter: 1 = prefer advantage |
| `TargetThreat` | How threatening is the target? | Uses ThreatEvaluator curves |
| `CanCastMagic` | Can I cast a specific spell? | floatParameter: spell level filter |
| `TargetHasCondition` | Does target have a condition? | stringParameter: condition name, boolParameter: invert |
| `DistanceFromMe` | How far is the target? | floatParameter: ideal distance |
| `Random` | Random chance | For non-deterministic behavior |
| `ActionTypeStatus` | What actions have been used? | boolParameter/boolSecParameter: which actions to check |
| `CanMoveToAnyEnemyInOneTurn` | Pathfinding check | |

### 7.3 Activity Types (Observed)

| ActivityType | Description |
|---|---|
| `Attack` | Melee or ranged weapon attack |
| `CastMagic` | Cast a spell (stringParameter = spell name) |
| `BreakFree` | Escape restraints/grapple |
| `Move` | Movement (various strategies) |

### 7.4 Layer 2: DecisionPackageDefinition (AI Behavior Packages)

A **DecisionPackage** is the complete AI behavior for a creature, composed of weighted references to DecisionDefinitions.

```jsonc
{
  "$type": "TA.AI.DecisionPackageDefinition, Assembly-CSharp",
  "package": {
    "weightedDecisions": [
      {
        "decision": "Definition:DecisionName:guid",
        "weight": 5.0,       // Base priority weight
        "cooldown": 0,        // Turns before can be selected again
        "dynamicCooldown": false  // Cooldown adjusts based on circumstances
      }
    ]
  }
}
```

### 7.5 AI Package Examples

**AirElementalCombatDecisions** (simple melee):
| Weight | Decision | Cooldown |
|---|---|---|
| 5.0 | LongRangePathToEnemy_Dash | 0 |
| 3.0 | MeleeAttack_Default | 0 |
| 2.0 | Move_AggressiveSingleTargetAndSpread | 0 |

Behavior: Dash to reach enemies > attack in melee > position aggressively. Pure melee brute.

**BanditCombatDecisions** (versatile fighter):
| Weight | Decision | Cooldown |
|---|---|---|
| 5.0 | LongRangePathToEnemy_Dash | 0 |
| 5.0 | CastMagic_Heal_SingleTarget | 0 |
| 4.0 | ShoveDown_Default | 3 |
| 4.0 | ShoveBack_Default | 3 |
| 3.0 | MeleeAttack_Default | 0 |
| 2.5 | CastMagic_ActionSurge_Self | 0 |
| 2.0 | Move_AggressiveSingleTarget | 0 |
| 1.0 | RangedAttack_Default | 0 |

Behavior: Prioritizes reaching enemies and healing allies equally, shoves opportunistically (with 3-turn cooldown to prevent spam), melee over ranged.

**DragonCombatDecisions** (boss):
| Weight | Decision | Cooldown |
|---|---|---|
| 6.0 | CastMagic_DPS_AoE_DragonBreath | 2 (dynamic) |
| 5.0 | Move_PrepareDragonBreath_Unsafe | 2 (dynamic) |
| 5.0 | CastMagic_FrightfulPresence_Dragon | 99 (one-time) |
| 3.0 | MeleeAttack_Default | 0 |
| 3.0 | RangedAttack_Default | 0 |
| 1.5 | Move_Dragon_Aggressive | 0 |
| 0.1 | Move_Dragon_AggressiveLongRangeFallback | 0 |
| 0.05 | Emote_Angry | 1 |

Behavior: Breath weapon is highest priority (weight 6) but has a 2-turn dynamic cooldown. Will position for breath. Frightful Presence used once (cooldown 99). Falls back to melee/ranged. Tiny weight emote for flavor.

### 7.6 How the AI Scoring Works

The system is a **utility AI** architecture:

1. Each turn, ALL `weightedDecisions` in the creature's package are evaluated
2. For each decision, the scorer multiplies all consideration outputs together
3. The scorer output is multiplied by the decision's `weight`
4. Cooldowns eliminate decisions that were recently used
5. The highest-scoring decision is selected
6. Ties or near-ties may have randomization

The `consideration.curve` (AnimationCurve) maps raw input values to 0.0-1.0 utility scores. This allows designers to create non-linear responses (e.g., "strongly prefer targets below 50% HP" with a sharp curve).

### 7.7 Threat Evaluator

Each monster has a named `threatEvaluatorDefinition` controlling target selection:

| Evaluator Name | Behavior |
|---|---|
| `PrioritizeNooneButFocus` | No special priority, but sticks to current target |
| `PrioritizeDamageDealers` | Targets highest DPS party members |
| `PrioritizeNoone` | No inherent target preference |

The evaluator uses AnimationCurves for:
- **hateResponse**: How much aggro from being attacked
- **woundRatioResponse**: Priority based on target's wound level
- **maxHPResponse**: Priority based on target's max HP
- **attackersCountResponse**: How many allies already attacking this target
- **meleeVsRangedResponse**: Preference for melee vs ranged targets
- **previousTargetRelatedMalus**: Penalty for switching targets (0.1 = slight preference to stay on target)

---

## 8. CHARACTER FAMILY DEFINITION (CharacterFamilyDefinition)

Creature type classification, mapping directly to D&D 5e creature types.

### 8.1 Structure

```jsonc
{
  "$type": "CharacterFamilyDefinition, Assembly-CSharp",
  "features": [],              // Inherent features for ALL creatures of this family
  "extraplanar": false,        // Whether this type is from another plane
  "name": "Undead"
}
```

### 8.2 All Creature Families

From the directory listing:
- **Aberration** - features: 43 null slots (reserved for future use), not extraplanar
- **Beast** - no features, not extraplanar
- **Celestial** - not extraplanar (interesting design choice)
- **Construct** - not extraplanar
- **Dragon** - not extraplanar
- **Elemental** - not extraplanar
- **Fey** - not extraplanar
- **Fiend** - (likely extraplanar)
- **Giant** - not extraplanar
- **Humanoid** - not extraplanar
- **Monstrosity** - not extraplanar
- **Ooze** - not extraplanar
- **Plant** - not extraplanar
- **Undead** - no features, not extraplanar

The `features` array can hold family-wide traits (e.g., all Aberrations sharing certain properties), but most families have empty feature arrays, meaning traits are defined per-monster.

---

## 9. CHARACTER SIZE DEFINITION (CharacterSizeDefinition)

### 9.1 Structure

```jsonc
{
  "$type": "CharacterSizeDefinition, Assembly-CSharp",
  "wieldingSize": "Medium",      // What weapon size category can be wielded
  "carryingSize": "Medium",      // Carrying capacity category
  "minExtent": { "x": 0, "y": 0, "z": 0 },  // Grid extent minimum
  "maxExtent": { "x": 0, "y": 0, "z": 0 },  // Grid extent maximum
  "visionHeightFactor": 0.93,    // Eye height relative to model
  "bestiaryScaleFactor": 0.7,    // Scale in bestiary view
  "name": "Medium"
}
```

### 9.2 All Size Categories

| Size | Min Extent | Max Extent | Bestiary Scale | Notes |
|---|---|---|---|---|
| Small | (0,0,0) | (0,0,0) | 0.9 | 1x1 cell |
| Medium | (0,0,0) | (0,0,0) | 0.7 | 1x1 cell |
| Large | (0,0,0) | (1,1,1) | 0.5 | 2x2 cells |
| LargeFlat | (0,0,0) | (1,0,1) | - | 2x2 but flat (spiders) |
| Large_Tall | (0,0,0) | (1,1,1) | - | 2x2 tall |
| Huge | (0,0,0) | (2,2,2) | - | 3x3 cells |
| Gargantuan | (0,0,0) | (3,3,3) | - | 4x4 cells |
| DragonSize | - | - | - | Custom for dragons |
| Tiny | - | - | - | Smallest |

The extent system uses a 3D integer grid where maxExtent determines how many additional cells the creature occupies beyond its origin cell.

---

## 10. CHARACTER TEMPLATE DEFINITION (CharacterTemplateDefinition)

Pre-built character/NPC definitions with full character creation data.

### 10.1 Structure

```jsonc
{
  "$type": "CharacterTemplateDefinition, Assembly-CSharp",
  
  // --- IDENTITY ---
  "firstName": "Angela",
  "surName": "Cleric2",
  "sex": "Female",
  "pronoun": "Female",
  "age": 47,
  "voiceId": "FEM1",
  
  // --- CHARACTER BUILD ---
  "mainRace": "Definition:Dwarf:...",
  "subRace": "Definition:DwarfSnow:...",
  "mainClass": "Definition:Cleric:...",
  "subClass": "Definition:DomainBattle:...",
  "deity": "Definition:Einar:...",
  "background": "Definition:Lawkeeper:...",
  "characterLevel": 2,
  "alignment": "LawfulGood",
  
  // --- ABILITY SCORES ---
  "abilityScores": [13, 9, 13, 10, 18, 10],
  "automateAbilityScoreIncreases": true,
  
  // --- APPEARANCE ---
  "originMorphotype": "Origin_NonHuman",
  "skinMorphotype": "FaceAndSkinFair",
  "faceShapeMorphotype": "FaceShape_A",
  "hairShapeMorphotype": "HairShape_C",
  "hairColorMorphotype": "HairColorBlond",
  "eyeColorMorphotype": "EyeColorBlack",
  // ... many more appearance fields
  
  // --- PERSONALITY ---
  "backgroundPersonalityFlag1": "Authority",
  "backgroundPersonalityFlag2": "Violence",
  "alignmentPersonalityFlag1": "Authority",
  "alignmentPersonalityFlag2": "Lawfulness",
  
  // --- EQUIPMENT ---
  "equipment": [
    "Definition:Backpack:...",
    "Definition:Mace:...",
    "Definition:Shield:...",
    "Definition:LightCrossbow:...",
    "Definition:ScaleMail:...",
    // ...
  ],
  "startingMoney": [0, 98, 0, 0, 0],      // [PP, GP, EP, SP, CP]
  
  // --- WEAPON CONFIGURATIONS ---
  "wieldedItemsConfigurations": [
    { "mainHandItemDefinition": "Definition:Mace:...", "offHandItemDefinition": "Definition:Shield:..." },
    { "mainHandItemDefinition": "Definition:LightCrossbow:...", "offHandItemDefinition": null },
    { "mainHandItemDefinition": null, "offHandItemDefinition": "Definition:Torch:..." }
  ],
  
  // --- SKILLS & PROFICIENCIES ---
  "skillsOverride": ["Insight", "Religion", "Perception", "Persuasion"],
  "toolsOverride": ["GamingSetDiceType"],
  "featsOverride": [],
  "expertisesOverride": [],
  "languagesOverride": [],
  
  // --- SPELLS ---
  "knownClassCantrips": [
    "Definition:Light:...",
    "Definition:SpareTheDying:...",
    "Definition:SacredFlame:...",
    // ...
  ],
  "preparedClassSpells": [
    "Definition:Bane:...",
    "Definition:CureWounds:...",
    "Definition:GuidingBolt:...",
    // ...
  ],
  
  // --- FLAGS ---
  "editorOnly": true,
  "isPremade": false
}
```

### 10.2 Template Types Observed

From the directory listing:
- **Premade party members**: "AngelaClrc2" (Dwarf Cleric 2)
- **Class archetypes**: "Barbarian_Berserker_Level16", "Barbarian_Claw_Level13"
- **Named NPCs**: "August"
- **QA variants**: "AngelaClrc2_QA" (testing versions)

These are used for pre-generated characters, NPC companions, and testing.

---

## 11. BESTIARY STATS DEFINITION (BestiaryStatsDefinition)

A single massive file (15MB) containing all bestiary display data. This is the in-game monster manual.

Only one file exists: `BestiaryStats.json` - too large to analyze inline, but it aggregates monster stat summaries for the bestiary UI.

---

## 12. FACTION DEFINITION (FactionDefinition)

Controls allegiance, reputation, and inter-faction relationships.

### 12.1 Structure

```jsonc
{
  "$type": "FactionDefinition, Assembly-CSharp",
  "builtIn": true,                         // System faction vs. story faction
  
  // --- REPUTATION RANGE ---
  "minRelationCap": -100,                  // Minimum possible reputation
  "maxRelationCap": 100,                   // Maximum possible reputation
  
  // --- REPUTATION PENALTIES ---
  "stealingPenalty": 0,                    // Rep loss for stealing
  "attackingPenalty": 0,                   // Rep loss for attacking members
  "killingPenalty": 0,                     // Rep loss for killing members
  
  // --- QUEST INTEGRATION ---
  "failsQuestOnLowRelation": false,       // Can quests fail from low rep?
  "questFailThreshold": -70,              // Rep threshold for quest failure
  
  // --- MEMBERS ---
  "prominentMembers": [                    // Named NPCs in this faction
    "Definition:Hertha_Gormsdottir:...",
    "Definition:Halman_Summer:..."
  ]
}
```

### 12.2 Faction Categories

**System Factions (builtIn: true)**:

| Faction | minRelCap | maxRelCap | Purpose |
|---|---|---|---|
| `HostileMonsters` | -100 | -100 | Always hostile, cannot improve |
| `Party` | 100 | 100 | Always friendly (player party) |
| `CaerLem_Guards` | varies | varies | Town guards |

**Story Factions (builtIn: false)**:

| Faction | Notable Members | Quest Fail Threshold |
|---|---|---|
| `Antiquarians` | Hertha Gormsdottir, Halman Summer | -70 |
| `Arcaneum` | (mage guild) | -70 |
| `ChurchOfEinar` | (religious order) | -70 |
| `CircleOfDanantar` | (druid circle) | -70 |

**DLC Factions**: `DLC1_Faction_Ally`, `DLC1_Faction_Forge`, `DLC1_Faction_Forge_Spy`

### 12.3 Faction Relationship System

The key insight is that `HostileMonsters` has **both min and max capped at -100**, making it permanently hostile. The `Party` faction is capped at **100/100**, permanently friendly.

Story factions have a full -100 to 100 range with penalties for antisocial actions. The `questFailThreshold` at -70 means you can lose quests by becoming too hostile with a faction.

Monsters reference their faction by name string (e.g., `"defaultFaction": "HostileMonsters"`), not by Definition: reference. This is one of the few places where a plain string is used instead of the Definition:name:guid pattern.

---

## 13. FORMATION DEFINITION (FormationDefinition)

Grid-based positioning for groups of units.

### 13.1 Structure

```jsonc
{
  "$type": "FormationDefinition, Assembly-CSharp",
  "defaultFormation": true,
  "formationToFixStuckLocation": false,
  "formationPositions": [
    { "x": 0, "y": 0, "z": 0 },   // Position 1 (leader)
    { "x": 1, "y": 0, "z": 0 },   // Position 2 (side-by-side)
    { "x": 0, "y": 0, "z": -1 },  // Position 3 (row 2, left)
    { "x": 1, "y": 0, "z": -1 },  // Position 4 (row 2, right)
    { "x": 0, "y": 0, "z": -2 },  // Position 5 (row 3, left)
    { "x": 1, "y": 0, "z": -2 },  // Position 6 (row 3, right)
    { "x": 0, "y": 0, "z": -3 },  // Position 7 (row 4, left)
    { "x": 1, "y": 0, "z": -3 }   // Position 8 (row 4, right)
  ]
}
```

### 13.2 Formation Patterns

**Column2** (default formation): 2-wide column, 4 rows deep.
```
[1][2]
[3][4]
[5][6]
[7][8]
```

Formations are used for:
- **Party exploration** (Column2 = default marching order)
- **Cutscene positioning** (Dialog_Level_01_CaerLem_03)
- **NPC group staging** (DLC3 convoy formations)
- **Special encounters** (MagicMouth)

Positions use integer 3D coordinates on the game's grid system. The Y axis handles elevation.

---

## 14. SYSTEM INTERCONNECTION MAP

```
MonsterDefinition
  ├── characterFamily ──────> CharacterFamilyDefinition
  ├── sizeDefinition ───────> CharacterSizeDefinition
  ├── defaultFaction ───────> FactionDefinition (by name)
  ├── features[] ───────────> FeatureDefinition (senses, movement, immunities)
  ├── attackIterations[] ───> MonsterAttackDefinition
  │     └── effectDescription
  │           ├── damageForm ──> damage types, dice, conditions
  │           └── conditionForm > ConditionDefinition
  ├── defaultBattleDecisionPackage > DecisionPackageDefinition
  │     └── weightedDecisions[]
  │           └── decision ──> DecisionDefinition
  │                 └── scorer
  │                       └── considerations[] (utility AI evaluations)
  ├── legendaryActionOptions[]
  │     ├── monsterAttackDefinition
  │     ├── featureDefinitionPower
  │     ├── spellDefinition
  │     └── decisionPackage ──> DecisionPackageDefinition (per legendary action)
  ├── threatEvaluatorDefinition (inline, not external)
  ├── monsterPresentation ──> MonsterPresentationDefinition (inline or external)
  └── droppedLootDefinition > LootDefinition

EncounterDefinition
  ├── monsterOccurences[]
  │     ├── monsterDefinition ─> MonsterDefinition
  │     └── encounterPlacementDecision > placement strategy
  ├── locationOverride ──────> location DB
  └── locationBlacklist[] ──> location DBs

EncounterTableDefinition
  └── encounterOccurences[]
        └── encounterDefinition > EncounterDefinition (weighted)

CharacterTemplateDefinition
  ├── mainRace, subRace
  ├── mainClass, subClass
  ├── equipment[], spells[]
  └── appearance morphotypes

FormationDefinition
  └── formationPositions[] (int3 grid positions)
```

---

## 15. KEY DESIGN PATTERNS FOR RECREATION

### 15.1 Blueprint System Rules

1. **Every game object is a serialized C# class** with `$type`, `guid`, `name`, and `guiPresentation`
2. **Cross-references use** `"Definition:Name:guid"` format universally
3. **Features are composable** - immunities, senses, movement modes are all separate FeatureDefinition objects composed into a features array
4. **AI is a weighted utility system** - not a behavior tree, not finite state machines
5. **Encounters are pre-authored compositions** - not procedurally generated groups
6. **Random encounters use weighted tables** referencing pre-authored encounters
7. **Factions use a simple reputation integer** with caps and penalties

### 15.2 Monster Design Checklist

To recreate a monster, you need:
1. CharacterFamilyDefinition (or pick existing)
2. CharacterSizeDefinition (or pick existing)
3. MonsterAttackDefinition(s) for each attack
4. A DecisionPackageDefinition for AI behavior
5. Appropriate FeatureDefinitions for senses, movement, immunities, abilities
6. MonsterPresentationDefinition for visuals
7. The MonsterDefinition itself tying everything together

### 15.3 AI Design Guidelines

- **Melee brutes**: High weight on Dash + MeleeAttack, low/no magic
- **Ranged fighters**: Balanced melee/ranged weights with movement
- **Spellcasters**: High weight on CastMagic decisions with cooldowns to prevent spell spam
- **Bosses**: Multiple attack options, dynamic cooldowns, positioning decisions, one-time abilities (cooldown 99)
- **Pack creatures**: Include Move_AggressiveSingleTargetAndSpread for flanking
- **All creatures**: Should include a fallback movement option at low weight

### 15.4 Encounter Balance Pattern

- Encounters are tagged with a single `challengeRating` number
- Encounter tables aggregate many encounters of varying CRs
- Runtime filtering (not in the blueprints) selects appropriate encounters for party level
- Mixed-CR creature groups (boss + minions) are common at higher CRs
- Placement decisions control tactical positioning (spread, pack, altitude-aware)
