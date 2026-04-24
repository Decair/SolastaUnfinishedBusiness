# Solasta: Crown of the Magister — Content & Data Design Document

> **Purpose**: Complete specification of all game content data: items, monsters, encounters, loot, feats, and reference/foundation types.
>
> **Companion Documents**: See `SOLASTA_DESIGN_DOC_CORE_SYSTEMS.md` (architecture, characters, features, spells, actions, AI) and `SOLASTA_DESIGN_DOC_WORLD_PRESENTATION.md` (world, dungeons, presentation).

---

## Table of Contents

1. [Item System](#1-item-system)
2. [Monster System](#2-monster-system)
3. [Encounter System](#3-encounter-system)
4. [Loot & Economy](#4-loot--economy)
5. [Feats, Invocations & Metamagic](#5-feats-invocations--metamagic)
6. [Foundational Reference Types](#6-foundational-reference-types)
7. [Difficulty System](#7-difficulty-system)

---

## 1. Item System

### 1.1 Architecture: Flag-Based Polymorphism

Items use a single `ItemDefinition` type with boolean flags that determine which sub-description blocks are relevant. This is flat, flag-based polymorphism — not inheritance.

```json
{
  "$type": "ItemDefinition, Assembly-CSharp",
  "isWeapon": true,
  "isArmor": false,
  "isAmmunition": false,
  "isUsableDevice": false,
  "isTool": false,
  "isFood": false,
  "isLightSourceItem": false,
  "isFocusItem": false,
  "isStarterPack": false,
  "isContainerItem": false,
  "isWealthPile": false,
  "isSpellbook": false,
  "isDocument": false,
  "isFactionRelic": false,
  "isMusicalInstrument": false
}
```

### 1.2 Universal Item Fields

```json
{
  "inDungeonEditor": true,
  "merchantCategory": "Weapon|Armor|MagicDevice|Adventuring|Crafting|Document|Ingredient",
  "weight": 3.0,
  "costs": [0, 15, 0, 0, 0],
  "itemTags": ["Standard", "Metal"],
  "activeTags": [],
  "inactiveTags": [],
  "magical": false,
  "requiresAttunement": false,
  "requiresIdentification": false,
  "requiredAttunementClasses": [],
  "itemRarity": "Common|Uncommon|Rare|VeryRare|Legendary",
  "canBeStacked": false,
  "stackSize": 10,
  "defaultStackCount": -1,
  "staticProperties": [],
  "slotTypes": ["MainHandSlot", "OffHandSlot", "ContainerSlot"],
  "slotsWhereActive": ["MainHandSlot", "OffHandSlot"]
}
```

**Cost array**: `[copper, gold, ?, ?, ?]` — first two elements are copper and gold.

**Item Rarity**: `Common` (mundane), `Uncommon` (minor magic), `Rare` (significant magic), `VeryRare` (powerful), `Legendary` (artifact)

**Item Tags**: Material tags (`Metal`, `Wood`, `Glass`, `Leather`), category tags (`Standard`, `Ingredient`)

### 1.3 Type Definitions (Archetypes)

Items reference type definitions that serve as archetypes.

#### WeaponTypeDefinition

```json
{
  "weaponCategory": "MartialWeaponCategory|SimpleWeaponCategory",
  "weaponProximity": "Melee|Range",
  "isBow": false,
  "isCrossbow": false,
  "animationTag": "GreatSword|HandCrossbow|...",
  "isAttachedToBone": "Prop1"
}
```

**All weapon types**: BattleaxeType, ClubType, DaggerType, DartType, GreataxeType, GreatswordType, HandaxeType, HeavyCrossbowType, LightCrossbowType, LongswordType, MaceType, MaulType, MorningstarType, QuarterstaffType, RapierType, ScimitarType, ShortswordType, SpearType, WarhammerType, UnarmedStrikeType, plus special types

#### ArmorTypeDefinition

```json
{
  "armorCategory": "LightArmorCategory|MediumArmorCategory|HeavyArmorCategory",
  "requiresProficiency": true
}
```

**All armor types**: LeatherType, StuddedLeatherType, HideArmorType, ChainShirtType, ScaleMailType, BreastPlateType, HalfPlateType, ChainMailType, SplintType, PlateType, ClothesType, PaddedType, ShieldType

#### AmmunitionTypeDefinition

Minimal — just a named type with GUI sprite. Types: `ArrowType`, `BoltType`, `BulletType`, `NeedleType`

### 1.4 Weapon Items

```json
{
  "isWeapon": true,
  "weaponDefinition": {
    "weaponType": "LongswordType",
    "reachRange": 1,
    "closeRange": 5,
    "maxRange": 5,
    "weaponTags": ["Versatile"],
    "effectDescription": {
      "effectForms": [{
        "formType": "Damage",
        "damageForm": {
          "diceNumber": 1,
          "dieType": "D8",
          "bonusDamage": 0,
          "damageType": "DamageSlashing",
          "versatile": true,
          "versatileDieType": "D10"
        }
      }]
    }
  }
}
```

**Weapon Tags** (D&D 5e properties): `Versatile`, `Finesse`, `Light`, `Thrown`, `TwoHanded`, `Heavy`, `Reach`, `Loading`, `Ammunition`

Weapon damage uses the same EffectDescription/EffectForm system as spells.

### 1.5 Armor Items

```json
{
  "isArmor": true,
  "armorDefinition": {
    "armorType": "ChainMailType",
    "armorClassValue": 16,
    "isBaseArmorClass": true,
    "maxDexterityBonus": 0,
    "requiresMinimalStrength": true,
    "minimalStrength": 13
  }
}
```

**AC logic:**
- `isBaseArmorClass: true` — Replaces base AC (body armor: Chain Mail AC 16 = flat 16)
- `isBaseArmorClass: false` — Adds to AC (Shield AC 2 is additive)
- `maxDexterityBonus: -1` = no cap (light), `0` = no DEX bonus (heavy), `2` = medium armor cap

**Stealth disadvantage** is NOT an armor field — it's a `staticProperty` referencing `AbilityCheckAffinityStealthDisadvantage`, making it modular and removable.

### 1.6 Enchantment System (Static Properties)

Magic items stack `staticProperties` that reference external FeatureDefinition objects:

```json
{
  "staticProperties": [
    {
      "appliesOnItemOnly": true,
      "type": "Feature",
      "featureDefinition": "Definition:AttackModifierWeapon+1:guid",
      "knowledgeAffinity": "ActiveAndVisible"
    },
    {
      "appliesOnItemOnly": false,
      "type": "Feature",
      "featureDefinition": "Definition:AttributeModifierArmor+1:guid",
      "knowledgeAffinity": "ActiveAndVisible"
    }
  ]
}
```

`appliesOnItemOnly: true` = affects weapon stats; `false` = affects wielder globally.

**Key insight**: Magic items don't change their base damage dice. The +1 to attack/damage is handled entirely through feature references. This makes enchantments modular and reusable across items.

### 1.7 Three-Stage Crafting Pipeline

```
Standard Weapon (15 gp, tags: ["Standard", "Metal"])
    ↓ craft with materials
Primed Weapon (115 gp, tags: ["Metal", "Ingredient"])
    ↓ enchant with magical ingredients
Enchanted Weapon (4500 gp, magical: true, rarity: "Rare")
```

The `"Ingredient"` tag on Primed items is the bridge — it flags them as valid crafting inputs for enchanting recipes.

### 1.8 Usable Devices (Potions, Scrolls, Wondrous Items)

```json
{
  "isUsableDevice": true,
  "usableDeviceDescription": {
    "usage": "Single|ByCharges",
    "chargesCapitalNumber": 1,
    "rechargeRate": "Dawn|ShortRest|LongRest|Never",
    "outOfChargesConsequence": "Persist|Destroy",
    "magicAttackBonus": 0,
    "saveDC": 10,
    "deviceFunctions": [
      {
        "type": "Power|Spell",
        "featureDefinitionPower": "Definition:PowerFunctionPotionOfHealing:guid",
        "spellDefinition": null,
        "useAffinity": "AtWill",
        "useAmount": 1
      }
    ],
    "usableDeviceTags": ["Potion"]
  }
}
```

- **Power functions** — Potions, wondrous item activations
- **Spell functions** — Scrolls cast actual spells

### 1.9 Identification System

- `requiresIdentification: true` on items
- `unidentifiedTitle` / `unidentifiedDescription` in itemPresentation for placeholder text
- Merchant `canIdentify` / `identifyCostGp` for identification service
- `autoIdentify: false` on loot pack entries

### 1.10 Equipment Slots (17 total)

| Slot | Type | Stacks | Renders on Model | Locked in Battle |
|---|---|---|---|---|
| MainHandSlot | Body | No | No | No |
| OffHandSlot | Body | No | No | No |
| HeadSlot | Body | No | Yes (sort 4) | No |
| TorsoSlot | Body | No | Yes (sort 0) | No |
| FeetSlot | Body | No | Yes (sort 2) | No |
| GlovesSlot | Body | No | Yes (sort 3) | No |
| NeckSlot | Body | No | No | No |
| FingerSlot (x2) | Body | No | No | No |
| ShouldersSlot | Body | No | Yes (sort 1) | No |
| WristsSlot | Body | No | No | No |
| BeltSlot | Body | No | No | No |
| BackSlot | Body | No | No | **Yes** |
| TabardSlot | Body | No | Yes (sort 5) | No |
| AmmunitionSlot (x2) | Body | Yes | No | No |
| ContainerSlot | Container | Yes | No | No |
| UtilitySlot | Container | Yes | No | No |

---

## 2. Monster System

### 2.1 MonsterDefinition

The core creature stat block — the largest and most complex blueprint type.

```json
{
  "$type": "MonsterDefinition, Assembly-CSharp",

  // Taxonomy
  "characterFamily": "Beast|Humanoid|Undead|Elemental|Fiend|...",
  "sizeDefinition": "Definition:Medium:guid",
  "alignment": "Unaligned",
  "defaultFaction": "HostileMonsters",

  // Core D&D 5e stats
  "armorClass": 13,
  "hitDice": 2,
  "hitDiceType": "D8",
  "hitPointsBonus": 2,
  "standardHitPoints": 11,
  "challengeRating": 0.25,

  // Ability scores: [STR, DEX, CON, INT, WIS, CHA]
  "abilityScores": [12, 15, 12, 3, 12, 6],

  // Proficiency bonuses
  "savingThrowScores": [
    { "abilityScoreName": "Dexterity", "bonus": 8 }
  ],
  "skillScores": [
    { "skillName": "Stealth", "bonus": 4 }
  ],

  // Multiattack
  "attackIterations": [
    {
      "number": 1,
      "monsterAttackDefinition": "Definition:Attack_Wolf_Bite:guid"
    }
  ],

  // Features (immunities, senses, movement, abilities)
  "features": [
    "Definition:SenseNormalVision:guid",
    "Definition:MoveModeMove8:guid",
    "Definition:CombatAffinityPackTactics:guid"
  ],

  // AI
  "defaultBattleDecisionPackage": "Definition:WolfCombatDecisions:guid",
  "threatEvaluatorDefinition": { /* see Core Systems doc section 7 */ },

  // Legendary creature support
  "legendaryCreature": false,
  "maxLegendaryResistances": 3,
  "maxLegendaryActionPoints": 3,
  "legendaryActionOptions": [
    {
      "cost": 1,
      "subaction": "MonsterAttack|Power|Spell",
      "monsterAttackDefinition": "Definition:...",
      "canMove": true,
      "moveMode": "Definition:MoveModeFly6:guid",
      "noOpportunityAttack": true,
      "decisionPackage": "Definition:..."
    }
  ],

  // Languages
  "languages": ["Language_Common", "Language_Elvish"],

  // Movement stances
  "sneakStance": "Stealth",
  "patrolStance": "Run",
  "interceptStance": "Run",

  // Loot
  "droppedLootDefinition": "Definition:LootPackName:guid",
  "noExperienceGain": false,

  // Dungeon Maker
  "dungeonMakerPresence": "Monster|NPC|None",
  "bestiaryEntry": "Full|None|Reference"
}
```

### 2.2 D&D 5e Stat Block Mapping

| D&D 5e Concept | Solasta Field |
|---|---|
| Creature Type | `characterFamily` |
| Size | `sizeDefinition` |
| AC | `armorClass` |
| Hit Points | `standardHitPoints` |
| Hit Dice | `hitDice` + `hitDiceType` |
| Ability Scores | `abilityScores[0-5]` = [STR,DEX,CON,INT,WIS,CHA] |
| Saving Throws | `savingThrowScores[]` |
| Skills | `skillScores[]` |
| Challenge Rating | `challengeRating` (supports 0.125, 0.25, 0.5) |
| Damage Immunities | Features with "DamageAffinity...Immunity" |
| Condition Immunities | Features with "ConditionAffinity...Immunity" |
| Senses | Features with "Sense..." |
| Movement | Features with "MoveMode..." |
| Multiattack | `attackIterations[]` with `number > 1` |

### 2.3 MonsterAttackDefinition

Defines a single attack action with the same EffectDescription engine as spells.

```json
{
  "$type": "MonsterAttackDefinition, Assembly-CSharp",
  "actionType": "Main",
  "toHitBonus": 8,
  "proximity": "Melee|Range",
  "reachRange": 1,
  "closeRange": 30,
  "maxRange": 120,
  "magical": false,
  "limitedUse": false,
  "maxUses": 1,

  "effectDescription": {
    "effectForms": [
      {
        "formType": "Damage",
        "damageForm": {
          "diceNumber": 2, "dieType": "D8", "bonusDamage": 5,
          "damageType": "DamageBludgeoning"
        }
      },
      {
        "formType": "Damage",
        "damageForm": {
          "diceNumber": 3, "dieType": "D6",
          "damageType": "DamageFire"
        }
      },
      {
        "formType": "Condition",
        "hasSavingThrow": true,
        "savingThrowAffinity": "Negates",
        "conditionForm": {
          "conditionDefinition": "Definition:ConditionGrappled:guid",
          "operation": "Add"
        }
      }
    ]
  }
}
```

A single attack can deal **multiple damage types AND apply conditions**, all gated by the same attack roll, with individual saving throws per effect form. Example: Ancient Remorhaz Bite = 6d10+7 piercing + 3d6 fire + DEX DC 17 grapple.

### 2.4 Sample Monsters

**Wolf (CR 0.25)** — Simple beast:
- STR 12, DEX 15, CON 12, INT 3, WIS 12, CHA 6
- AC 13, HP 11 (2d8+2), Size: Small
- 1x Bite, Features: Pack Tactics, Keen Hearing
- AI: WolfCombatDecisions

**Air Elemental (CR 5)** — Complex creature:
- STR 14, DEX 20, CON 14, INT 6, WIS 10, CHA 6
- AC 15, HP 90 (12d10+24), Size: Large
- 2x Slam per round, Fly 12, Walk 10
- 9 condition immunities, 3 damage resistances (B/P/S), poison immunity

**Aksha (CR 7)** — Boss with legendary actions:
- STR 18, DEX 18, CON 18, INT 18, WIS 12, CHA 18
- AC 16, HP 85 (10d8+40), 3 legendary resistances, 3 action points
- 4 Legendary Actions: Bite+Move, Darkness, Slow, Life Leech
- Superior Darkvision, Tremorsense, Fly 8, Walk 6

### 2.5 Character Family (Creature Types)

14 creature families mapping to D&D 5e: `Aberration`, `Beast`, `Celestial`, `Construct`, `Dragon`, `Elemental`, `Fey`, `Fiend`, `Giant`, `Humanoid`, `Monstrosity`, `Ooze`, `Plant`, `Undead`

Each can have inherent features via `features[]` array (most are empty — traits defined per-monster).

### 2.6 Character Size

| Size | Grid Extent | Bestiary Scale |
|---|---|---|
| Tiny | 1x1 | — |
| Small | 1x1 | 0.9 |
| Medium | 1x1 | 0.7 |
| Large | 2x2 | 0.5 |
| Huge | 3x3 | — |
| Gargantuan | 4x4 | — |

The `maxExtent` field determines additional cells beyond origin: Large = (1,1,1) = 2x2x2.

---

## 3. Encounter System

### 3.1 EncounterDefinition

Defines a specific combat encounter with creature composition.

```json
{
  "$type": "EncounterDefinition, Assembly-CSharp",
  "type": "Battle",
  "challengeRating": 10,
  "locationOverride": "Definition:LocationName:guid",
  "locationBlacklist": ["Definition:Bridge_LocationDB:guid"],

  "monsterOccurences": [
    {
      "monsterDefinition": "Definition:Giant_Ape:guid",
      "number": 1,
      "encounterPlacementDecision": "Definition:EncounterSpawn_PackSmall_PenalizeAltitude:guid",
      "creatureSex": "Male",
      "randomHumanoidPresentation": false
    },
    {
      "monsterDefinition": "Definition:Badlands_Ape:guid",
      "number": 4,
      "encounterPlacementDecision": "Definition:EncounterSpawn_PackSmall_PenalizeAltitude:guid"
    }
  ]
}
```

**Naming convention**: `CR[rating]_[description]_x[counts]`
Example: `CR3_GoblinWolfBand_x1_x1_x2_x1` = CR3, 1+1+2+1 creatures

**Placement strategies**:
- `EncounterSpawn_Spread` — Distribute across arena
- `EncounterSpawn_PackSmall_PenalizeAltitude` — Group together, prefer flat ground

### 3.2 EncounterTableDefinition

Random encounter tables that select from weighted pools.

```json
{
  "encounterOccurences": [
    { "weight": 1, "encounterDefinition": "Definition:CR2_Goblins:guid" },
    { "weight": 1, "encounterDefinition": "Definition:CR3_Spiders:guid" },
    { "weight": 1, "encounterDefinition": "Definition:CR5_Elementals:guid" }
  ]
}
```

All entries have `weight: 1` (equal probability). The table is NOT CR-filtered — the game engine filters by party level at runtime.

**Biome-specific tables**: `AridBadlandsEncounter`, `DLC1_JungleEncounterTable`, `DLC1_SwampEncounterTable`, `DLC1_MarchesEncounterTable`, `DLC1_CityEncounterTable`

---

## 4. Loot & Economy

### 4.1 LootPackDefinition

```json
{
  "lootSpawnMode": "ItemList",
  "lootMagnitudeMode": "Individual",
  "lootChallengeMode": "ByPartyLevel",

  "itemOccurencesList": [
    {
      "diceNumber": 1,
      "diceType": "D1",
      "additiveModifier": 0,
      "itemMode": "Explicit|TreasureTable",
      "itemDefinition": "Definition:ItemName:guid",
      "treasureTableDefinition": "Definition:RandomTreasureTableA_Gem:guid",
      "autoIdentify": false
    }
  ]
}
```

**Item modes**:
- `Explicit` — Drops the specific item. Quantity = dice roll + modifier
- `TreasureTable` — Rolls on the referenced treasure table for random items

### 4.2 TreasureTableDefinition

85 treasure tables organized in a tiered system:

```json
{
  "treasureOptions": [
    { "odds": 20, "itemDefinition": "Definition:Amethyst:guid", "amount": 1 },
    { "odds": 15, "itemDefinition": "Definition:Topaz:guid", "amount": 1 },
    { "odds": 5, "itemDefinition": "Definition:Diamond:guid", "amount": 1 }
  ]
}
```

**Treasure tiers** (A-N):

| Tier | Contents |
|---|---|
| A | Gems (20GP Amethyst to 1000GP Diamond) |
| B | Consumables / Scrolls |
| C | Magic Weapons (tier 1) |
| D | Wondrous Items / Weapons+Armors (tier 2) |
| E | Ingredients (mundane + magical) |
| F | Mundane Gear |
| G/H | Art items (25GP / 50GP) |
| I | Poisons + Poisoned Ammo |
| J | Scrolls |
| K | Recipe Manuals |
| L | Legendary items |
| M | Primed items |
| N | Weapons+Armors (tier 3) |

**Monster-specific tables**: `Random_Goblin_Table`, `Random_Skeleton_Table`, `Random_Orc_Grunt/Archer/Chieftain_Table`, `Random_Troll_Table`, etc.

**Regional ingredient tables**: Keyed to world areas for foraging results.

### 4.3 RecipeDefinition (Crafting)

```json
{
  "craftedItem": "Definition:ItemName:guid",
  "stackCount": 20,
  "craftingHours": 2,
  "craftingDC": 10,
  "toolTypeDefinition": "Definition:ArtisanToolSmithToolsType:guid",
  "ingredients": [
    { "amount": 1, "itemDefinition": "Definition:Ingredient_X:guid" }
  ],
  "spellDefinition": null
}
```

**Tool types gate recipes**:
- Smith Tools — Weapons, ammunition, metal items
- Herbalism Kit — Potions, salves
- Enchanting Tool — Magical enchantments
- Abyssal Agitator — DLC demonic items

**Difficulty scaling**: Basic DC 10, advanced alchemy DC 14, demonic DC 22

**Crafting Manual items** exist in loot tables — recipes must be *found* before they can be crafted.

### 4.4 MerchantDefinition

```json
{
  "overchargePercent": 5,
  "buyBackPercent": 25,
  "factionAffinity": "Principality",
  "canDetectMagic": false,
  "detectMagicCostGp": 100,
  "canIdentify": false,
  "identifyCostGp": 100,

  "stockUnitDescriptions": [
    {
      "itemDefinition": "Definition:Food_Ration:guid",
      "stackCount": 1,
      "initialAmount": 99,
      "minAmount": 0,
      "maxAmount": 99,
      "reassortAmount": 10,
      "reassortRateType": "Hour|Day",
      "reassortRateValue": 1,
      "requiredFaction": "",
      "factionStatus": "Alliance|Brotherhood"
    }
  ]
}
```

**Economy design patterns**:

| Category | Initial | Max | Restock | Rate | Example |
|---|---|---|---|---|---|
| Consumables | 99 | 99 | 10/hour | Rapid | Food Ration |
| Ammunition | 99 | 10 | 10/2hrs | Fast | Arrows |
| Basic Weapons | 2 | 2 | 1/day | Slow | Longsword |
| Common Potions | 2 | 8 | 1/2 days | Very slow | Potion of Healing |
| Greater Potions | 1 | 4 | 1/4 days | Rare | Greater Healing |

**Faction gating**: Some items require `factionStatus: "Brotherhood"` (higher rep).

### 4.5 CharacterToLootPackMapDefinition

Maps class to loot — when a chest uses this, loot adjusts to party composition.

```json
{
  "mappingMode": "Class",
  "characterClassToLootPackMappings": [
    { "className": "Fighter", "lootPack": "Definition:FighterLoot:guid" },
    { "className": "Wizard", "lootPack": "Definition:WizardLoot:guid" }
  ]
}
```

All 12 D&D classes mapped.

---

## 5. Feats, Invocations & Metamagic

### 5.1 FeatDefinition (47 feats)

```json
{
  "$type": "FeatDefinition, Assembly-CSharp",

  // Prerequisites
  "compatibleClassesPrerequisite": [],
  "mustCastSpellsPrerequisite": false,
  "compatibleRacesPrerequisite": [],
  "minimalAbilityScorePrerequisite": false,
  "minimalAbilityScoreValue": 13,
  "minimalAbilityScoreName": "Constitution",
  "armorProficiencyPrerequisite": false,
  "knownFeatsPrerequisite": [],
  "hasFamilyTag": false,
  "familyTag": "",

  // Granted features
  "features": [
    "Definition:AttributeModifierFeatBadlands:guid",
    "Definition:DamageAffinityPoisonResistance:guid"
  ]
}
```

**Feat categories**:
- **Combat**: Ambidextrous, ArmorMaster, DauntingPush, FollowUpStrike, MightyBlow, RaiseShield, TripAttack, TwinBlade
- **Magic**: ArcaneAppraiser, BurningTouch, FlawlessConcentration, PowerfulCantrip, ToxicTouch
- **Crafting**: InitiateAlchemist, InitiateEnchanter, MasterAlchemist, MasterEnchanter
- **Survival**: BadlandsMarauder, ForestRunner, HardToKill, Hauler, Robust
- **Creed**: Creed_Of_Arun, Creed_Of_Einar, Creed_Of_Maraike, Creed_Of_Misaye, Creed_Of_Pakri

### 5.2 InvocationDefinition (24 Warlock invocations)

```json
{
  "$type": "InvocationDefinition, Assembly-CSharp",
  "requiredKnownSpell": "Definition:EldritchBlast:guid",
  "requiredLevel": 1,
  "requiredPact": "Definition:FeatureSetPactBlade:guid",
  "grantedFeature": "Definition:AdditionalDamageInvocation:guid",
  "grantedSpell": null,
  "consumesSpellSlot": false,
  "longRestRecharge": false,
  "overrideMaterialComponent": false
}
```

**Invocation types**:

| Type | grantedFeature | grantedSpell | consumesSlot | Example |
|---|---|---|---|---|
| Passive | Set | null | — | AgonizingBlast, ThirstingBlade |
| At-Will Spell | null | Set | false | ArmorOfShadows, AscendantStep |
| 1/Day Spell | null | Set | true | BewitchingWhispers, ChainsCarceri |

**Prerequisites**: `requiredKnownSpell` (Eldritch Blast for blast mods), `requiredLevel` (1-15), `requiredPact` (Blade, Tome, Chain).

### 5.3 MetamagicOptionDefinition (8 options)

```json
{
  "$type": "MetamagicOptionDefinition, Assembly-CSharp",
  "costMethod": "FixedValue|SpellLevel",
  "sorceryPointsCost": 1,
  "metamagicType": "string",
  "parameterMethod": "None|CharismaModifier|BoundFeature",
  "parameterValue": 1,
  "boundFeature": null
}
```

| Metamagic | Cost | SP | Parameter | Effect |
|---|---|---|---|---|
| Careful Spell | Fixed | 1 | CharismaModifier | CHA mod allies auto-succeed saves |
| Distant Spell | Fixed | 1 | None | Double range |
| Empowered Spell | Fixed | 1 | BoundFeature | Reroll CHA mod damage dice |
| Extended Spell | Fixed | 1 | None | Double duration |
| Heightened Spell | Fixed | 3 | None | Target has disadvantage on save |
| Quickened Spell | Fixed | 2 | None | Cast as bonus action |
| Subtle Spell | Fixed | 1 | None | No V/S components |
| Twinned Spell | SpellLevel | 1 | None | Cost = spell level, target 2nd creature |

### 5.4 FightingStyleDefinition (6 styles)

```json
{
  "features": ["Definition:AttackModifierArchery:guid"],
  "condition": "RangedWeaponAttack"
}
```

| Style | Condition | Effect |
|---|---|---|
| Archery | RangedWeaponAttack | +2 ranged attacks |
| Defense | (armor equipped) | +1 AC in armor |
| Dueling | (one-handed melee) | +2 damage |
| Great Weapon | TwoHandedMeleeWeapon | Reroll 1s and 2s on damage |
| Protection | ShieldEquiped | Impose disadvantage on attacks |
| Two-Weapon | (dual wield) | Add ability to off-hand damage |

---

## 6. Foundational Reference Types

### 6.1 SkillDefinition (18 skills)

| Skill | Ability Score |
|---|---|
| Athletics | Strength |
| Acrobatics | Dexterity |
| SleightOfHand | Dexterity |
| Stealth | Dexterity |
| Arcana | Intelligence |
| History | Intelligence |
| Investigation | Intelligence |
| Nature | Intelligence |
| Religion | Intelligence |
| AnimalHandling | Wisdom |
| Insight | Wisdom |
| Medecine | Wisdom |
| Perception | Wisdom |
| Survival | Wisdom |
| Deception | Charisma |
| Intimidation | Charisma |
| Performance | Charisma |
| Persuasion | Charisma |

Note: "Medecine" is the internal spelling (preserved from French).

### 6.2 DamageDefinition (13 types)

| Name | Category | Symbol |
|---|---|---|
| DamageAcid | Elemental | 25F0 |
| DamageBludgeoning | Physical | 25F1 |
| DamageCold | Elemental | 25F2 |
| DamageFire | Elemental | 25F3 |
| DamageForce | Magical | 25F4 |
| DamageLightning | Elemental | 25F5 |
| DamageNecrotic | Magical | 25F6 |
| DamagePiercing | Physical | 25F7 |
| DamagePoison | Elemental | 25F8 |
| DamagePsychic | Magical | 25F9 |
| DamageRadiant | Magical | 25FA |
| DamageSlashing | Physical | 25FB |
| DamageThunder | Elemental | 25FC |

### 6.3 DieTypeDefinition (6 dice)

`DieTypeD4`, `DieTypeD6`, `DieTypeD8`, `DieTypeD10`, `DieTypeD12`, `DieTypeD20`

Each has a `rollingMeshReference` for 3D dice animation and `scaleFactor` for visual size.

### 6.4 LanguageDefinition (15 languages)

**Common languages**: Language_Common, Language_Dwarvish, Language_Elvish, Language_Giant, Language_Gnomish, Language_Goblin, Language_Halfling, Language_Orc

**Exotic languages**: Language_Abyssal, Language_Draconic, Language_Infernal, Language_Druidic, Language_Terran

**Solasta-specific**: Language_Tirmarian, Language_SpyCode

### 6.5 DeityDefinition (5 deities)

| Deity | Alignment | Divine Domains |
|---|---|---|
| Arun | Neutral | Fire, Cold, Lightning, Sun |
| Einar | LawfulGood | Battle, Law |
| Maraike | NeutralGood | Life, Oblivion |
| Pakri | LawfulNeutral | Law, Insight |
| Misaye | ChaoticNeutral | Mischief, Battle |

Each deity lists valid `CharacterSubclassDefinition` names (cleric domains).

### 6.6 AlignmentDefinition (10 alignments)

Two-axis system: `lawAxis` (-1 Chaotic, 0 Neutral, 1 Lawful) × `goodnessAxis` (-1 Evil, 0 Neutral, 1 Good)

| Alignment | Law | Good | Default Personality |
|---|---|---|---|
| LawfulGood | 1 | 1 | Authority, Friendliness |
| LawfulNeutral | 1 | 0 | Authority, Pragmatism |
| LawfulEvil | 1 | -1 | Authority, Greed |
| NeutralGood | 0 | 1 | Friendliness, Helpfulness |
| Neutral | 0 | 0 | Self-Preservation, Pragmatism |
| NeutralEvil | 0 | -1 | Selfishness, Greed |
| ChaoticGood | -1 | 1 | Friendliness, Violence |
| ChaoticNeutral | -1 | 0 | Selfishness, Pragmatism |
| ChaoticEvil | -1 | -1 | Cynicism, Violence |
| Unaligned | 0 | 0 | (hidden, for creatures) |

### 6.7 PersonalityFlagDefinition (57 flags)

Three categories:

**Alignment Type** (visible on personality chart): Authority, Cynicism, Formal, Friendliness, Greed, Helpfulness, Lawfulness, Normal, Pragmatism, Self-Preservation, Selfishness, Slang, Violence, Whisperer

**Background Type** (hidden): Bg_Aescetic, Bg_Artist, Bg_Highclass, Bg_Judgmental, Bg_Logical, Bg_Lukewarm, Bg_Mean, Bg_Nosy, Bg_Occultist, Bg_Preachy, Bg_Wild, Bg_Wisecracker

**Class Type** (hidden): ClassBarbarianFlag, ClassBardFlag, ClassClericFlag, ClassDruidFlag, ClassFighterFlag, ClassMonkFlag, ClassPaladinFlag, ClassRangerFlag, ClassRogueFlag, ClassSorcererFlag, ClassWarlockFlag, ClassWizardFlag

**Special**: Religion flags (5), Race flags (7), Gameplay tags (GpCombat, GpExplorer, GpSpellcaster, GpStealth), Story flags

### 6.8 CurrencyDefinition (5 types)

`CurrencyPlatinum`, `CurrencyGold`, `CurrencySilver`, `CurrencyElectrum`, `CurrencyCopper`

Standard D&D 5e currency. Conversion rates in game logic, not blueprints.

### 6.9 SmartAttributeDefinition (38 attributes)

**Six Ability Scores**: Strength, Dexterity (→ modifies ArmorClass, Initiative), Constitution, Intelligence, Wisdom, Charisma

**Derived**: ArmorClass, AttacksNumber, CriticalThreshold, HitPoints, HitPointBonusPerLevel, Initiative, ProficiencyBonus, Speed

**Class Resources**: BardicInspirationDie/Number, ChannelDivinityNumber, HealingPool, IndomitableResistances, KiPoints, RagePoints, RageDamage, SorceryPoints, BrutalCriticalDice

**Item Properties**: ItemCharges, ItemSpellbookPages, ItemStackCount

The `modifiedAttributes` array creates a dependency graph — Dexterity changes trigger ArmorClass and Initiative recalculation.

### 6.10 FactionDefinition

```json
{
  "builtIn": true,
  "minRelationCap": -100,
  "maxRelationCap": 100,
  "stealingPenalty": 0,
  "attackingPenalty": 0,
  "killingPenalty": 0,
  "failsQuestOnLowRelation": false,
  "questFailThreshold": -70,
  "prominentMembers": ["Definition:NPC_Name:guid"]
}
```

**System factions**: `HostileMonsters` (capped at -100/-100, permanently hostile), `Party` (capped at 100/100, permanently friendly)

**Story factions**: `Antiquarians`, `Arcaneum`, `ChurchOfEinar`, `CircleOfDanantar` (full -100 to 100 range, quest failure at -70)

### 6.11 FactionStatusDefinition (7 tiers)

| Status | Ceiling | Combat Side | Merchant Rebate |
|---|---|---|---|
| Hatred | -75 | Enemy | 0% |
| Animosity | -25 | Enemy | -10% |
| Indifference | 10 | Neutral | 0% |
| Sympathy | 30 | Ally | 0% |
| Alliance | 50 | Ally | +10% |
| Brotherhood | 80 | Ally | +20% |
| LivingLegend | 100 | Ally | +30% |

### 6.12 KnowledgeLevelDefinition (5 levels)

The bestiary/monster knowledge progressive unlock system.

| Level | Name | Access Flags | Bonus Damage |
|---|---|---|---|
| 0 | Unknown | 0 (none) | 0 |
| 1 | Observed | 14 (bits 1-3) | +1 |
| 2 | Studied | 1022 (bits 1-9) | +2 |
| 3 | Known | 65534 (bits 1-15) | +3 |
| 4 | Mastered | -1 (all bits) | +4 |

Each bit in `accessFlags` corresponds to a UI panel (AC, HP, resistances, etc.).

### 6.13 TerrainTypeDefinition (7 types)

`Arctic`, `Coast`, `Desert`, `Forest`, `Grassland`, `Mountain`, `Swamp`

Used for Ranger Favored Terrain and regional ingredient gathering.

### 6.14 ArmorCategoryDefinition (5 categories)

| Category | Proficiency | Physical | Noise (Prof/NotProf) | Forbids -DEX |
|---|---|---|---|---|
| NoArmorCategory | No | No | 2/2 | No |
| LightArmorCategory | Yes | Yes | 2/4 | No |
| MediumArmorCategory | Yes | Yes | 4/5 | No |
| HeavyArmorCategory | Yes | Yes | 7/7 | Yes |
| ShieldCategory | Yes | No | 0/0 | No |

`noiseRange` values create the stealth detection system — heavier armor = louder.

### 6.15 ItemFlagDefinition (22 flags)

- **Quality**: ItemFlagPrimed
- **Quest**: ItemFlagQuest
- **Elemental**: ItemFlag_Flaming, ItemFlag_Corrosive, ItemFlag_Flash
- **Ingredients**: ItemFlagIngredient_Component, ItemFlagIngredient_Enchant
- **Poisons**: ItemFlagPoison_1D4/1D6/1D8/2D4/2D8/3D6/3D8 + Blinding/Paralyzing/Restrained
- **Evidence**: ItemFlag_Clue_Exonerating, ItemFlag_Clue_Incriminating

### 6.16 EffectProxyDefinition (42 proxies)

Persistent spell effects placed in the world. Key types:

- **Zones**: ProxyDarkness, ProxyFogCloud, ProxySilence, ProxyEntangle, ProxyGrease
- **Walls**: ProxyWallOfFire_Line/Ring, ProxyWallOfForce, ProxyBladeBarrier
- **Summoned Objects**: ProxyFlamingSphere (canMove, canAttack), ProxySpiritualWeapon
- **Damage Clouds**: ProxyCloudKill, ProxyIncendiaryCloud, ProxyInsectPlague
- **Light Sources**: ProxyDancingLights, ProxyDaylight
- **Symbols**: ProxySymbolOfDeath/Fear/Hopelessness/Sleep/Stun

Each has `damageType`, `damageDie`, `canMove`, `canAttack`, `constrainedToSpellArea` fields.

---

## 7. Difficulty System

### 7.1 DifficultyPresetDefinition (5 presets)

| Setting | Story | Explorer | Authentic (Default) | Scavenger | Cataclysm |
|---|---|---|---|---|---|
| **Damage Taken** | ×0.2 | ×0.5 | ×1.0 | ×1.25 | ×1.5 |
| **Enemy HP** | ×1.0 | ×1.0 | ×1.0 | ×1.5 | ×2.0 |
| **Ally Roll Modifier** | +3 | +1 | 0 | 0 | 0 |
| **Enemy Roll Modifier** | -3 | -1 | 0 | +1 | +3 |
| **Enemy Crits** | Disabled | Disabled | Enabled | Enabled | Enabled |
| **AI Uses Powers** | No | No | No | Yes | Yes |
| **AI Targets Helpless** | No | No | No | No | Yes |
| **Auto-Revive** | Yes | Yes | No | No | No |
| **Max HP on Level Up** | Yes | Yes | No | No | No |
| **No Food Needed** | Yes | Yes | No | No | No |
| **Concentration Loss** | Never | Normal | Normal | Normal | Normal |
| **Any Class Scrolls** | Yes | No | No | No | No |
| **Verbal/Somatic** | Disabled | Full | Full | Full | Full |
| **Material** | Disabled | Basic | Basic | Full | Full |
| **Auto-Detect Traps** | Yes | No | No | No | No |
| **Force Craft Success** | Yes | No | No | No | No |
| **Forced Crits** | Yes | No | No | No | No |
| **Retry Gadgets** | Yes | Yes | No | No | No |
| **Encumbrance** | Default | Default | Variant | Variant | Variant |

### 7.2 Difficulty Schema

```json
{
  "$type": "DifficultyPresetDefinition, Assembly-CSharp",
  "isDefaultPreset": false,

  "disableEnemyCrits": false,
  "aiUsesPowerfulMovesMoreOften": false,
  "aiTargetsHelplessCharacters": false,
  "damageTakenAllyMultiplier": 1.0,
  "enemyHpMultiplier": 1.0,
  "savingThrowAllyModifier": 0,
  "savingThrowEnemyModifier": 0,
  "attackRollAllyModifier": 0,
  "attackRollEnemyModifier": 0,

  "autorevive": false,
  "maxHpOnLevelUp": false,
  "maxHpOnHitDice": false,
  "noFoodNeeded": false,
  "neverLoseConcentrationOnSpells": false,
  "scrollsCanBeUsedByAnyCharacter": false,
  "verbalComponent": "Full|Disabled",
  "somaticComponent": "Full|Disabled",
  "materialComponent": "Full|Basic|Disabled",

  "disableRandomEncounters": false,
  "autoDetectTraps": false,
  "authorizeRetryOnGadgets": false,
  "forceCraftingRollSuccess": false,
  "encumbranceRuleType": "Default|Variant"
}
```
