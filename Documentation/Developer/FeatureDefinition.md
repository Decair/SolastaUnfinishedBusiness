# Solasta Feature Definition System - Complete Blueprint Catalog

## Architecture Overview

Every FeatureDefinition in Solasta follows a **common base structure** with type-specific fields layered on top. The `$type` field identifies the C# class, and every definition shares these base fields:

### Universal Base Fields (All FeatureDefinitions)
```json
{
  "$type": "FeatureDefinition[SubType], Assembly-CSharp",
  "guiPresentation": {
    "hidden": false,
    "title": "Feature/&[Key]Title",
    "description": "Feature/&[Key]Description",
    "spriteReference": { "m_AssetGUID": "", ... },
    "color": { "r": 1.0, "g": 1.0, "b": 1.0, "a": 1.0 },
    "symbolChar": "221E",
    "sortOrder": 0,
    "unusedInSolastaCOTM": false,
    "usedInValleyDLC": false
  },
  "contentCopyright": "OpenGameContent|OpenGameContentModified|TacticalAdventuresContent|TacticalAdventuresContentHidden",
  "guid": "[32-char hex]",
  "contentPack": "BaseGame|CrownOfTheMagister|ValleyDLC",
  "name": "[UniqueDefinitionName]"
}
```

### Cross-Reference Pattern
Other definitions are referenced via the `"Definition:[Name]:[guid]"` string format:
```
"Definition:ConditionRaging:1567bb2d092a2bf4fbe78f6ea6afdf49"
"Definition:SpellListWizard:ebbe572795f1e2e499bf55d00df9547f"
```

### Common Restriction Fields (on many subtypes)
```json
"myselfFamilyRestrictions": [],      // creature family restrictions on self
"otherCharacterFamilyRestrictions": [] // creature family restrictions on target
```

---

## 1. FeatureDefinitionAbilityCheckAffinity

**Purpose:** Modifiers to ability checks (skill checks, ability checks).

**Key Fields:**
```json
{
  "$type": "FeatureDefinitionAbilityCheckAffinity, Assembly-CSharp",
  "useControllerAbilityChecks": false,
  "affinityGroups": [
    {
      "abilityScoreName": "Strength|Dexterity|Constitution|Intelligence|Wisdom|Charisma",
      "proficiencyName": "",           // specific skill name, or "" for all
      "affinity": "HalfProficiencyWhenNotProficient|Advantage|Disadvantage|None",
      "abilityCheckGroupOperation": "AddDie|FlatValueBonus",
      "abilityCheckModifierDiceNumber": 0,
      "abilityCheckModifierDieType": "D1|D4|D6|D8|D10|D12|D20",
      "abilityCheckContext": "None|...",
      "lightingContext": "Irrelevant|DimLight|Darkness"
    }
  ],
  "substractBardicDieRoll": false
}
```

**Enum: affinity** = `None`, `HalfProficiencyWhenNotProficient`, `Advantage`, `Disadvantage`
**Enum: abilityCheckGroupOperation** = `AddDie`, `FlatValueBonus`
**Enum: lightingContext** = `Irrelevant`, `DimLight`, `Darkness`

**Example:** Bard's Jack of All Trades - 6 groups (one per ability), each with `HalfProficiencyWhenNotProficient`.

---

## 2. FeatureDefinitionActionAffinity

**Purpose:** Controls what actions a character can/cannot perform; adds new action options.

**Key Fields:**
```json
{
  "$type": "FeatureDefinitionActionAffinity, Assembly-CSharp",
  "allowedActionTypes": [true, true, true, true, true, true],  // 6 booleans (Main/Bonus/Move/Reaction/Free/NoCost)
  "eitherMainOrBonus": false,
  "maxAttacksNumber": -1,         // -1 = unlimited
  "forbiddenActions": [],          // action IDs forbidden
  "authorizedActions": ["RecklessAttack"],  // action IDs granted
  "restrictedActions": [],         // action IDs restricted to specific context
  "actionExecutionModifiers": [],  // modifiers when executing actions
  "specialBehaviour": "None",
  "randomBehaviorDie": "D10",
  "randomBehaviourOptions": [],
  "rechargeReactionsAtEveryTurn": false
}
```

**Enum: specialBehaviour** = `None`, and others (for AI behavior)

**Known Action IDs:** `RecklessAttack`, `RitualCasting`, `BardicInspiration`, `Rage`, `HideMain`, `DashMain`, `DisengageMain`, `AttackMain`, `UseItemMain`

---

## 3. FeatureDefinitionAdditionalDamage

**Purpose:** Extra damage applied on attacks (Sneak Attack, Smite, magic weapon effects, etc.). The most complex feature type.

**Key Fields:**
```json
{
  "$type": "FeatureDefinitionAdditionalDamage, Assembly-CSharp",
  "notificationTag": "SneakAttack|BrandingSmite|...",
  "limitedUsage": "None|OncePerTurn|OnceInMyTurn",
  "firstTargetOnly": true,
  "targetSide": "Enemy|Ally|All",

  // TRIGGER CONDITIONS
  "triggerCondition": "AlwaysActive|AdvantageOrNearbyAlly|SpellDamagesTarget|...",
  "requiredProperty": "None|FinesseOrRangeWeapon|MeleeWeapon|...",
  "attackModeOnly": false,
  "attackOnly": false,
  "requiredTargetCondition": null,   // "Definition:ConditionXxx:guid" or null
  "requiredTargetSenseType": "Darkvision",
  "requiredTargetCreatureTag": "",
  "requiredCharacterFamily": null,
  "requiredSpecificSpell": null,

  // DAMAGE CONFIGURATION
  "damageValueDetermination": "Die|FlatBonus|SpellLevel",
  "flatBonus": 0,
  "damageDieType": "D6",
  "damageDiceNumber": 1,
  "additionalDamageType": "Specific|SameAsBaseDamage|AncestryDamageType",
  "specificDamageType": "DamageRadiant|DamageFire|...",

  // SCALING
  "damageAdvancement": "ClassLevel|SlotLevel|None",
  "diceByRankTable": [
    { "rank": 1, "diceNumber": 1 },
    { "rank": 3, "diceNumber": 2 },
    // ...per-level or per-slot-level scaling
  ],

  // CREATURE TYPE BONUS
  "familiesWithAdditionalDice": [],
  "familiesDiceNumber": 1,

  "ignoreCriticalDoubleDice": false,

  // SAVING THROW (optional)
  "hasSavingThrow": false,
  "savingThrowAbility": "Dexterity|Constitution|Wisdom|...",
  "dcComputation": "FixedValue|SpellCastingFeature|AbilityBonusPlusFixed",
  "savingThrowDC": 10,
  "savingThrowDCAbilityModifier": "Dexterity",
  "damageSaveAffinity": "None|HalfDamage|Negates",

  // CONDITION APPLICATION
  "conditionOperations": [
    {
      "hasSavingThrow": false,
      "operation": "Add|Remove",
      "conditionDefinition": "Definition:ConditionXxx:guid",
      "saveAffinity": "None",
      "canSaveToCancel": false,
      "saveOccurence": "EndOfTurn|StartOfTurn"
    }
  ],

  // LIGHT SOURCE (e.g., Branding Smite)
  "addLightSource": false,
  "lightSourceForm": {
    "lightSourceType": "Basic",
    "brightRange": 4,
    "dimAdditionalRange": 4,
    "color": { "r": 1.0, "g": 1.0, "b": 1.0, "a": 1.0 }
  }
}
```

**Enum: triggerCondition** = `AlwaysActive`, `AdvantageOrNearbyAlly`, `SpellDamagesTarget`, `TargetHasCondition`, `TargetIsWounded`, `TargetDoesNotHaveCondition`, `RagingAndDamageType`
**Enum: requiredProperty** = `None`, `FinesseOrRangeWeapon`, `MeleeWeapon`, `RangeWeapon`, `Melee2Handed`
**Enum: damageAdvancement** = `None`, `ClassLevel`, `SlotLevel`
**Enum: additionalDamageType** = `Specific`, `SameAsBaseDamage`, `AncestryDamageType`
**Enum: limitedUsage** = `None`, `OncePerTurn`, `OnceInMyTurn`
**Enum: dcComputation** = `FixedValue`, `SpellCastingFeature`, `AbilityBonusPlusFixed`

---

## 4. FeatureDefinitionAttackModifier

**Purpose:** Modifiers to attack rolls and damage rolls (Fighting Style: Archery, Dueling, magic weapon bonuses).

**Key Fields:**
```json
{
  "$type": "FeatureDefinitionAttackModifier, Assembly-CSharp",
  "triggerCondition": "AlwaysActive|...",
  "requiredProperty": "None|RangedWeapon|...",

  // ATTACK ROLL
  "attackRollModifierMethod": "FlatValue|None|SourceAbilityBonus",
  "attackRollUseCasterBonus": false,
  "attackRollModifier": 2,
  "attackRollAbilityScore": "",

  // DAMAGE ROLL
  "damageRollModifierMethod": "None|FlatValue|SourceAbilityBonus",
  "damageRollUseCasterBonus": false,
  "damageRollModifier": 0,
  "damageRollAbilityScore": "",

  "additionalDamageDice": 0,

  // DUAL WIELDING
  "canDualWieldNonLight": false,
  "canAddAbilityBonusToSecondary": false,

  // SPECIAL
  "magicalWeapon": false,
  "followUpStrike": false,
  "followUpDamageDie": "D4",
  "followUpAddAbilityBonus": true,

  // BONUS ATTACKS
  "additionalBonusAttackFromMain": false,
  "additionalBonusUnarmedStrikeAttacksFromMain": false,
  "additionalBonusUnarmedStrikeAttacksCount": 0,
  "additionalBonusUnarmedStrikeAttacksTag": "",
  "additionalReturnMissileReactionAttack": false,
  "additionalMonsterAttack": false,
  "additionalMonsterAttacksCount": 0,

  // ABILITY SCORE REPLACEMENT
  "abilityScoreReplacement": "None|...",

  // DAMAGE DIE REPLACEMENT
  "damageDieReplacement": "None|...",
  "dieTypeByRankTable": [],
  "replacedDieType": "D8"
}
```

**Enum: attackRollModifierMethod / damageRollModifierMethod** = `None`, `FlatValue`, `SourceAbilityBonus`, `ConditionAmount`
**Enum: abilityScoreReplacement** = `None`, and other values for features like Shillelagh

---

## 5. FeatureDefinitionAttributeModifier

**Purpose:** Modifies character attributes (ability scores, AC, HP, etc.). Very versatile.

**Key Fields:**
```json
{
  "$type": "FeatureDefinitionAttributeModifier, Assembly-CSharp",
  "modifiedAttribute": "Constitution|Strength|ArmorClass|HitPointBonusPerLevel|...",
  "modifierOperation": "Additive|Set|ForceIfBetter|SetWithDexPlusOtherAbilityScoreBonusIfBetter|...",
  "modifierValue": 19,
  "modifierAbilityScore": "Constitution|Wisdom|...",
  "situationalContext": "None|NotWearingArmorOrMageArmor|NotWearingHeavyArmor|...",
  "minimum1": false,
  "useBonusFromCaster": false
}
```

**Enum: modifiedAttribute** = `Strength`, `Dexterity`, `Constitution`, `Intelligence`, `Wisdom`, `Charisma`, `ArmorClass`, `HitPointBonusPerLevel`, `Initiative`, `HealingPool`, `AbilityScoreIncrease`, `TagsProperty`
**Enum: modifierOperation** = `Additive`, `Multiplicative`, `Set`, `ForceIfBetter`, `SetWithDexPlusOtherAbilityScoreBonusIfBetter`, `AddAbilityScoreBonus`, `AddProficiencyBonus`, `AddHalfProficiencyBonus`, `Force`, `AddSurrogateAttribute`, `AddConditionAmount`
**Enum: situationalContext** = `None`, `NotWearingArmor`, `NotWearingArmorOrMageArmor`, `NotWearingHeavyArmor`, `WearingShield`, `WieldingTwoHandedWeapon`, `ConsciousAllyNextToTarget`, etc.

**Examples:**
- Amulet of Health: `modifiedAttribute: "Constitution"`, `modifierOperation: "ForceIfBetter"`, `modifierValue: 19`
- Barbarian Unarmored Defense: `modifiedAttribute: "ArmorClass"`, `modifierOperation: "SetWithDexPlusOtherAbilityScoreBonusIfBetter"`, `modifierValue: 10`, `modifierAbilityScore: "Constitution"`, `situationalContext: "NotWearingArmorOrMageArmor"`
- Armor +1: `modifiedAttribute: "ArmorClass"`, `modifierOperation: "Additive"`, `modifierValue: 1`

---

## 6. FeatureDefinitionCombatAffinity

**Purpose:** Combat-related advantages/disadvantages, AC effects, critical hit modifications.

**Key Fields:**
```json
{
  "$type": "FeatureDefinitionCombatAffinity, Assembly-CSharp",
  // INITIATIVE
  "initiativeAffinity": "None|Advantage|Disadvantage",
  "canRageToOvercomeSurprise": false,

  // OPPORTUNITY ATTACKS
  "attackOfOpportunityImmunity": false,
  "attackOfOpportunityOnMeAdvantageType": "None|Advantage|Disadvantage",

  // ATTACKS ON ME
  "attackOnMeAdvantage": "None|Advantage|Disadvantage",
  "attackOnMeCountLimit": -1,    // -1 = always applies

  // CRITICAL HITS
  "autoCritical": false,
  "criticalHitImmunity": true,

  // MY ATTACKS
  "myAttackAffinityFilter": "Always|AllButSelf|...",
  "myAttackAdvantage": "None|Advantage|Disadvantage",
  "ignoreCover": false,
  "permanentCover": "None|Half|ThreeQuarters|Full",
  "ignoreRangeAdvantage": false,

  // ATTACK MODIFIER DICE
  "myAttackModifierValueDetermination": "None|FlatValue|Die|...",
  "myAttackModifierSign": "Add|Substract",
  "myAttackModifierDiceNumber": 1,
  "myAttackModifierDieType": "D4",
  "myAttackDamageMultiplier": 1.0,

  // DAMAGE REDUCTION
  "myDamageReductionValueDetermination": "None|...",
  "myDamageReductionDiceNumber": 1,
  "myDamageReductionDieType": "D4",

  // CONTEXT
  "situationalContext": "None|ConsciousAllyNextToTarget|WearingShield|...",
  "requiredCondition": null,     // "Definition:ConditionXxx:guid"
  "nullifiedBySenses": [],
  "nullifiedBySelfSenses": [],

  // MULTI-ATTACK DEFENSE
  "multiAttackAffinity": false,
  "multiAttackDefenseValue": 0,

  // READY ACTION
  "readyAttackAdvantage": "None|Advantage",
  "shoveOnReadyAttackHit": false
}
```

**Examples:**
- Adamantine Plate: `criticalHitImmunity: true`
- Pack Tactics: `myAttackAdvantage: "Advantage"`, `situationalContext: "ConsciousAllyNextToTarget"`
- True Strike: `myAttackAdvantage: "Advantage"`, `myAttackAffinityFilter: "Always"`

---

## 7. FeatureDefinitionDamageAffinity

**Purpose:** Damage resistances, immunities, vulnerabilities, and special damage reactions (relentless rage, retaliation).

**Key Fields:**
```json
{
  "$type": "FeatureDefinitionDamageAffinity, Assembly-CSharp",
  "ancestryDefinesDamageType": false,
  "ancestryType": "Sorcerer|Barbarian|...",
  "damageType": "DamageAcid|DamageBludgeoning|DamageFire|DamageCold|DamageLightning|DamageNecrotic|DamagePoison|DamagePsychic|DamageRadiant|DamageThunder|DamagePiercing|DamageSlashing|DamageForce",
  "savingThrowAdvantageType": "None|Advantage",
  "savingThrowModifier": 0,
  "damageAffinityType": "Resistance|Immunity|Vulnerability|None",
  "flatDamageReduction": 0,
  "flatDamageReductionOnlyAppliesToFirstDamageForm": true,
  "tagsIgnoringAffinity": ["MagicalWeapon", "MagicalEffect"],  // bypasses
  "situationalContext": "None|...",

  // HEAL FROM DAMAGE
  "healsBack": false,
  "healBackCap": 0,

  // RETALIATION
  "retaliateWhenHit": false,
  "retaliateProximity": "Melee|Range",
  "retaliateRangeCells": 1,
  "retaliatePower": null,        // "Definition:PowerXxx:guid"
  "retaliateFromSource": false,

  // KNOCKOUT/DEATH PREVENTION (Relentless Rage)
  "knockOutAffinity": "None|ConstitutionCheck|...",
  "knockOutOccurencesNumber": 1,
  "knockOutRequiredCondition": null,  // "Definition:ConditionRaging:guid"
  "knockOutDCAttribute": "RelentlessRageDC",
  "knockOutAddDC": 5,
  "instantDeathImmunity": false
}
```

**Enum: damageAffinityType** = `None`, `Resistance`, `Immunity`, `Vulnerability`
**Enum: damageType** = `DamageAcid`, `DamageBludgeoning`, `DamageCold`, `DamageFire`, `DamageForce`, `DamageLightning`, `DamageNecrotic`, `DamagePiercing`, `DamagePoison`, `DamagePsychic`, `DamageRadiant`, `DamageSlashing`, `DamageThunder`
**Enum: knockOutAffinity** = `None`, `ConstitutionCheck`

**Examples:**
- Acid Resistance: `damageType: "DamageAcid"`, `damageAffinityType: "Resistance"`
- Bludgeoning Resistance (non-magical): `damageType: "DamageBludgeoning"`, `damageAffinityType: "Resistance"`, `tagsIgnoringAffinity: ["MagicalWeapon", "MagicalEffect"]`
- Relentless Rage: `knockOutAffinity: "ConstitutionCheck"`, `knockOutRequiredCondition: "Definition:ConditionRaging:guid"`, `knockOutDCAttribute: "RelentlessRageDC"`

---

## 8. FeatureDefinitionSavingThrowAffinity

**Purpose:** Saving throw advantages, bonuses, and special abilities.

**Key Fields:**
```json
{
  "$type": "FeatureDefinitionSavingThrowAffinity, Assembly-CSharp",
  "useControllerSavingThrows": false,
  "priorityAbilityScore": "",
  "affinityGroups": [
    {
      "abilityScoreName": "Strength|Dexterity|Constitution|Intelligence|Wisdom|Charisma",
      "affinity": "Advantage|Disadvantage|None",
      "savingThrowModifierType": "AddDice|FlatValue",
      "savingThrowModifierDiceNumber": 0,
      "savingThrowModifierDieType": "D1|D4|...",
      "restrictedForms": [],        // shapeshifting form restrictions
      "restrictedSchools": [],       // spell school restrictions
      "restrictedSpells": [],        // specific spell restrictions
      "restrictedPowers": [],        // power restrictions
      "savingThrowContext": "None|..."
    }
  ],
  "indomitableSavingThrows": 0,  // number of rerolls (Fighter Indomitable)
  "canBorrowLuck": false,         // Halfling Lucky
  "canUseDiamondSoul": false      // Monk Diamond Soul
}
```

**Examples:**
- Advantage on All Saves: 6 groups, each ability with `affinity: "Advantage"`
- Danger Sense (Dex only): 1 group, `abilityScoreName: "Dexterity"`, `affinity: "Advantage"`
- Aura of Protection: modifier dice based on Charisma bonus applied to all saves

---

## 9. FeatureDefinitionMagicAffinity

**Purpose:** Spellcasting modifiers - concentration, spell slots, DC bonuses, ritual casting, spell scribing.

**Key Fields:**
```json
{
  "$type": "FeatureDefinitionMagicAffinity, Assembly-CSharp",
  // PREPARED SPELLS
  "preparedSpellModifier": "None|...",

  // SAVE DC / ATTACK
  "saveDCModifierType": "None|FlatValue|...",
  "saveDCModifier": 0,
  "spellAttackModifierType": "None|FlatValue|...",
  "spellAttackModifier": 0,

  // SPELL IMMUNITIES
  "spellImmunities": [],
  "maxSpellLevelImmunity": -1,

  // CONCENTRATION
  "concentrationAffinity": "None|Advantage|...",
  "overConcentrationThreshold": -1,

  // CASTING MODIFIERS
  "castingAffinity": "Normal|...",
  "spellcastingSuccessDC": 10,
  "forceHalfDamageOnCantrips": false,
  "cantripRetribution": false,
  "forcedSavingThrowAffinity": "None|...",
  "impairedSpeech": false,

  // FOCUS RULES
  "somaticWithWeaponOrShield": false,
  "somaticWithWeapon": false,
  "canUseProficientWeaponAsFocus": false,
  "rangeSpellNoProximityPenalty": false,

  // WAR MAGIC
  "usesWarList": false,
  "warListSlotBonus": 1,
  "warListSpells": [],

  // RITUAL CASTING
  "ritualCasting": "None|Prepared|Spellbook",
  "canLearnRitualScrolls": false,

  // SCRIBING
  "scribeAdvantageType": "None|Advantage",
  "scribeDurationMultiplier": 1.0,
  "scribeCostMultiplier": 1.0,
  "additionalScribedSpells": 0,
  "additionalKnownSpellsCount": 0,

  // ADDITIONAL SPELL SLOTS
  "additionalSlots": [
    { "slotLevel": 3, "slotsNumber": 1 }
  ],

  // METAMAGIC
  "metamagicOptions": [],

  // SPELL SLOT PRESERVATION
  "preserveSlotRoll": false,
  "preserveSlotThreshold": 20,
  "preserveSlotLevelCap": 5,

  // SORCERY POINTS
  "healingPerSpentSorceryPoint": 0,

  // COUNTERSPELL
  "counterspellAffinity": "None|...",
  "spellsCounterAffinity": "None|...",

  // DEVICE IDENTIFICATION
  "deviceTagsAutoIdentifying": [],
  "autoIdentifyPossessedMagicalItems": false,
  "ignoreClassRestrictionsOnMagicalItems": false,

  // EXTENDED SPELL LIST
  "extendedSpellList": null,

  // SAVE DC BONUS (specific)
  "addBonusToEffectSaveDC": "None|...",
  "bonusToEffectSaveDC": 0
}
```

---

## 10. FeatureDefinitionMovementAffinity

**Purpose:** Movement speed modifiers, special movement abilities.

**Key Fields:**
```json
{
  "$type": "FeatureDefinitionMovementAffinity, Assembly-CSharp",
  "appliesToAllModes": true,
  "moveMode": "Walk|Fly|Swim|Climb|Burrow",
  "baseSpeedAdditiveModifier": 2,     // cells added to base speed
  "additiveModifierAdvancement": "None|...",
  "additiveModifierByLevelTable": [],
  "forceMinimalBaseSpeed": false,
  "minimalBaseSpeed": 6,
  "baseSpeedMultiplicativeModifier": 1.0,
  "minMaxMoves": 0,
  "speedAddBase": false,

  // CLIMBING
  "fastClimber": false,
  "expertClimber": false,
  "canMoveOnWalls": false,

  // FLYING
  "canFlyWithWalkSpeed": false,

  // JUMPING
  "enhancedJump": false,
  "additionalJumpCells": 0,

  // TERRAIN
  "immuneDifficultTerrain": false,

  // DISABLE MOVEMENT TYPES
  "disableVault": false,
  "disableDrop": false,
  "disableJump": false,
  "disableClimb": false,

  // FALLING
  "additionalFallThreshold": 0,

  // ENCUMBRANCE
  "encumbranceImmunity": false,
  "heavyArmorImmunity": false,

  // CONTEXT
  "situationalContext": "None|NotWearingHeavyArmor|...",
  "additionalDashTag": ""
}
```

**Note:** Speed is in CELLS (1 cell = 5 feet). So `baseSpeedAdditiveModifier: 2` = +10 feet.

**Examples:**
- Barbarian Fast Movement: `baseSpeedAdditiveModifier: 2`, `situationalContext: "NotWearingHeavyArmor"` (+10ft when not in heavy armor)
- Cloak of Arachnida: `canMoveOnWalls: true`
- Land's Stride: `immuneDifficultTerrain: true`

---

## 11. FeatureDefinitionMoveMode

**Purpose:** Grants a specific movement type at a specific speed. Simple definition.

**Key Fields:**
```json
{
  "$type": "FeatureDefinitionMoveMode, Assembly-CSharp",
  "moveMode": "Walk|Fly|Swim|Climb|Burrow",
  "speed": 12    // in cells (12 cells = 60 feet)
}
```

**Enum: moveMode** = `Walk`, `Fly`, `Swim`, `Climb`, `Burrow`

**Known Definitions:** `MoveModeFly10`, `MoveModeFly12`, `MoveModeClimb6`, `MoveModeBurrow8`, `MoveModeBurrow10`, `MoveModeBurrow16`, `MoveModeSwim6`, `MoveModeSwim8`, `MoveModeSwim10`, `MoveModeSwim12`, `MoveModeElfSylvanMoveSpeed` (Walk 6)

---

## 12. FeatureDefinitionSense

**Purpose:** Vision and perception types (darkvision, etc.).

**Key Fields:**
```json
{
  "$type": "FeatureDefinitionSense, Assembly-CSharp",
  "senseType": "Darkvision|SuperiorDarkvision|NormalVision|Blindsight|Tremorsense|Truesight|DetectInvisibility",
  "senseRange": 12,           // in cells
  "stealthBreakerRange": 6,   // range at which stealth is broken
  "revealsHiddenObjects": false
}
```

**Enum: senseType** = `NormalVision`, `Darkvision`, `SuperiorDarkvision`, `Blindsight`, `Tremorsense`, `Truesight`, `DetectInvisibility`

**Examples:**
- Standard Darkvision: `senseType: "Darkvision"`, `senseRange: 12` (60ft)
- Superior Darkvision: `senseType: "SuperiorDarkvision"`, `senseRange: 16` (80ft - Drow uses 24/120ft)

---

## 13. FeatureDefinitionProficiency

**Purpose:** Grants proficiency in weapons, armor, skills, saving throws, tools, and languages.

**Key Fields:**
```json
{
  "$type": "FeatureDefinitionProficiency, Assembly-CSharp",
  "proficiencyType": "Armor|Weapon|SavingThrow|Skill|Tool|Language|FocusType",
  "proficiencies": [
    "LightArmorCategory|MediumArmorCategory|HeavyArmorCategory|ShieldCategory",
    "MartialWeaponCategory|SimpleWeaponCategory|LongswordType|...",
    "Strength|Dexterity|Constitution|Intelligence|Wisdom|Charisma",
    "Athletics|Acrobatics|SleightOfHand|Stealth|Arcana|History|...",
    "EnchantingToolType|SmithToolType|PoisonerKitType|HerbalismKitType|...",
    "Language_Common|Language_Elvish|..."
  ],
  "forbiddenItemTags": []
}
```

**Enum: proficiencyType** = `Armor`, `Weapon`, `SavingThrow`, `Skill`, `Tool`, `Language`, `FocusType`

**Armor Proficiencies:** `LightArmorCategory`, `MediumArmorCategory`, `HeavyArmorCategory`, `ShieldCategory`
**Weapon Proficiencies:** `SimpleWeaponCategory`, `MartialWeaponCategory`, plus individual types like `LongswordType`, `HandaxeType`, etc.
**Skill Proficiencies:** `Athletics`, `Acrobatics`, `SleightOfHand`, `Stealth`, `Arcana`, `History`, `Investigation`, `Nature`, `Religion`, `AnimalHandling`, `Insight`, `Medicine`, `Perception`, `Survival`, `Deception`, `Intimidation`, `Performance`, `Persuasion`
**Saving Throw Proficiencies:** The 6 ability score names

---

## 14. FeatureDefinitionFeatureSet

**Purpose:** Groups multiple features together, with Union (all) or Exclusion (choose one) modes.

**Key Fields:**
```json
{
  "$type": "FeatureDefinitionFeatureSet, Assembly-CSharp",
  "featureSet": [
    "Definition:PointPoolAbilityScoreImprovement:guid",
    "Definition:PointPoolBonusFeat:guid"
  ],
  "mode": "Union|Exclusion",
  "ancestryDamageTypeMap": [],
  "ancestryType": "Sorcerer|Barbarian|...",
  "defaultSelection": 0,
  "uniqueChoices": false,
  "enumerateInDescription": false,
  "hasRacialAffinity": false
}
```

**Enum: mode** = `Union` (grant all features), `Exclusion` (player chooses one)

**Example:** Ability Score Choice: `mode: "Exclusion"` between `PointPoolAbilityScoreImprovement` and `PointPoolBonusFeat` (ASI or Feat choice at level 4, 8, etc.)

---

## 15. FeatureDefinitionPointPool

**Purpose:** Grants a pool of points to spend on ability scores, skills, spells, expertise, etc.

**Key Fields:**
```json
{
  "$type": "FeatureDefinitionPointPool, Assembly-CSharp",
  "poolType": "AbilityScore|Skill|Expertise|Cantrip|Spell|Language|Tool|FightingStyle",
  "poolAmount": 2,
  "restrictedChoices": [
    "AnimalHandling", "Athletics", "Insight", "Investigation",
    "Nature", "Perception", "Survival", "Stealth"
  ],
  "uniqueChoices": false,
  "spellListOverride": null,     // "Definition:SpellListXxx:guid"
  "ritualOnly": false,
  "minSpellLevel": 0,
  "maxSpellLevel": 9,
  "extraSpellsTag": ""
}
```

**Enum: poolType** = `AbilityScore`, `Skill`, `Expertise`, `Cantrip`, `Spell`, `Language`, `Tool`, `FightingStyle`, `Feat`

**Examples:**
- ASI: `poolType: "AbilityScore"`, `poolAmount: 2`
- Ranger Skills: `poolType: "Skill"`, `poolAmount: 3`, `restrictedChoices: [specific skills]`

---

## 16. FeatureDefinitionCastSpell

**Purpose:** The core spellcasting definition. Defines everything about a class's spellcasting ability.

**Key Fields:**
```json
{
  "$type": "FeatureDefinitionCastSpell, Assembly-CSharp",
  "spellCastingOrigin": "Class|Race|Subclass",
  "spellcastingAbility": "Intelligence|Wisdom|Charisma",
  "spellcastingParametersComputation": "Dynamic|Static",
  "staticDCValue": 10,
  "staticToHitValue": 4,
  "spellListDefinition": "Definition:SpellListWizard:guid",
  "restrictedSchools": [],

  // SPELL KNOWLEDGE
  "spellKnowledge": "Spellbook|WholeList|Selection|FixedList",
  "fixedSpellTag": "",
  "spellReadyness": "Prepared|AllKnown",
  "spellPreparationCount": "AbilityBonusPlusLevel|AbilityBonusPlusHalfLevel|FixedNumber",

  // SLOT RECHARGE
  "slotsRecharge": "LongRest|ShortRest",
  "uniqueLevelSlots": false,
  "spellCastingLevel": -1,
  "cantripsOnly": false,

  // PER-LEVEL PROGRESSION (arrays indexed by class level 1-20)
  "knownCantrips": [3, 3, 3, 4, 4, 4, 4, 4, 4, 5, ...],
  "knownSpells":   [0, 0, 0, 0, ...],    // for known-casters like Sorcerer
  "scribedSpells": [6, 2, 2, 2, ...],    // for Wizard spellbook

  "replacedSpells": [0, 0, 0, ...],

  // SPELL SLOTS TABLE
  "slotsPerLevels": [
    {
      "level": 1,
      "slots": [2, 0, 0, 0, 0, 0, 0, 0, 0]  // slots for spell levels 1-9
    },
    {
      "level": 2,
      "slots": [3, 0, 0, 0, 0, 0, 0, 0, 0]
    },
    // ... through level 20
  ],

  "focusType": "Arcane|Druidic|None",
  "hasAccessToInvocations": false,
  "cannotUpcast": false
}
```

**Enum: spellKnowledge** = `Spellbook`, `WholeList`, `Selection`, `FixedList`
**Enum: spellReadyness** = `Prepared`, `AllKnown`
**Enum: spellPreparationCount** = `AbilityBonusPlusLevel`, `AbilityBonusPlusHalfLevel`, `FixedNumber`
**Enum: slotsRecharge** = `LongRest`, `ShortRest`
**Enum: focusType** = `Arcane`, `Druidic`, `None`
**Enum: spellCastingOrigin** = `Class`, `Race`, `Subclass`

---

## 17. FeatureDefinitionHealingModifier

**Purpose:** Modifies healing received or administered.

**Key Fields:**
```json
{
  "$type": "FeatureDefinitionHealingModifier, Assembly-CSharp",
  // HEALING BONUS DICE (when receiving healing)
  "healingBonusDiceNumber": 0,
  "healingBonusDiceType": "D1|D6|...",
  "addLevel": "None|EffectLevel|CharacterLevel",

  // SELF-HEALING WHEN CASTING
  "healsSelfWhenCastingHealingSpell": false,
  "selfHealingDiceNumber": 2,
  "selfHealingDiceType": "D1",
  "selfHealingAddLevel": "EffectLevel|None",

  // CONDITION ON HEAL
  "addsConditionWhenCastingHealingSpell": null,
  "addedConditionOccurenceType": "StartOfTurn|EndOfTurn",

  // HEALING MODIFIERS
  "cannotGainHitPoints": false,
  "advantageOnHitDieSpending": false,
  "maximizeReceivedHealing": true,

  // KINDRED HEALING
  "hitDiceHealsKindred": false,
  "healSelfHealsKindred": false,

  // MEDICINE
  "medecineStabilizeTo1HitPoint": false
}
```

**Example:** Beacon of Hope: `maximizeReceivedHealing: true`

---

## 18. FeatureDefinitionConditionAffinity

**Purpose:** Immunity, advantage, or modifiers against specific conditions.

**Key Fields:**
```json
{
  "$type": "FeatureDefinitionConditionAffinity, Assembly-CSharp",
  "conditionType": "ConditionCharmed|ConditionFrightened|ConditionPoisoned|ConditionParalyzed|ConditionProne|ConditionRestrained|ConditionBlinded|ConditionDeafened|ConditionStunned|ConditionPetrified|ConditionExhausted|ConditionDiseased",
  "savingThrowAdvantageType": "None|Advantage",
  "savingThrowModifier": 0,
  "conditionAffinityType": "Immunity|Advantage|None",
  "silent": false,
  "rerollSaveWhenGained": false,
  "rerollAdvantageType": "None|Advantage"
}
```

**Enum: conditionAffinityType** = `None`, `Immunity`, `Advantage`
**Enum: conditionType** = `ConditionCharmed`, `ConditionFrightened`, `ConditionPoisoned`, `ConditionParalyzed`, `ConditionProne`, `ConditionRestrained`, `ConditionBlinded`, `ConditionDeafened`, `ConditionStunned`, `ConditionPetrified`, `ConditionExhausted`, `ConditionDiseased`, `ConditionIncapacitated`

**Example:** Charm Immunity: `conditionType: "ConditionCharmed"`, `conditionAffinityType: "Immunity"`

---

## 19. FeatureDefinitionAdditionalAction

**Purpose:** Grants extra actions in a turn (Haste, Fighter Action Surge, Monk bonus attacks).

**Key Fields:**
```json
{
  "$type": "FeatureDefinitionAdditionalAction, Assembly-CSharp",
  "actionType": "Main|Bonus|Move|Reaction",
  "forbiddenActions": [],
  "authorizedActions": [],
  "restrictedActions": [
    "AttackMain", "DashMain", "DisengageMain", "HideMain", "UseItemMain"
  ],
  "maxAttacksNumber": 1,
  "triggerCondition": "None|..."
}
```

**Enum: actionType** = `Main`, `Bonus`, `Move`, `Reaction`

**Known Action IDs:** `AttackMain`, `AttackFree`, `DashMain`, `DashBonus`, `DisengageMain`, `DisengageBonus`, `HideMain`, `HideBonus`, `UseItemMain`, `UseItemBonus`, `CastMain`, `CastBonus`, `Shove`, `Ready`

**Example:** Haste: `actionType: "Main"`, `restrictedActions: ["AttackMain", "DashMain", "DisengageMain", "HideMain", "UseItemMain"]`, `maxAttacksNumber: 1`

---

## 20. FeatureDefinitionRegeneration

**Purpose:** Automatic HP regeneration (Ring of Regeneration, troll-like abilities).

**Key Fields:**
```json
{
  "$type": "FeatureDefinitionRegeneration, Assembly-CSharp",
  "diceNumber": 2,
  "dieType": "D1",        // D1 = flat value
  "bonus": 0,
  "tickType": "Round|Minute|Hour",
  "tickNumber": 1,
  "preventingDamages": [],   // damage types that prevent regeneration
  "isActiveWhenDown": false
}
```

**Example:** Ring of Regeneration: `diceNumber: 2`, `dieType: "D1"`, `tickType: "Round"`, `tickNumber: 1` = regenerate 2 HP per round

---

## 21. FeatureDefinitionLightSource

**Purpose:** Grants light-producing abilities (racial traits, spells, items).

**Key Fields (inferred from LightSourceForm sub-object seen in AdditionalDamage):**
```json
{
  "$type": "FeatureDefinitionLightSource, Assembly-CSharp",
  "lightSourceForm": {
    "lightSourceType": "Basic|Sun",
    "brightRange": 4,        // cells of bright light
    "dimAdditionalRange": 4, // additional cells of dim light
    "color": { "r": 1.0, "g": 1.0, "b": 1.0, "a": 1.0 },
    "applyToSelf": false,
    "forceOnSelf": false
  }
}
```

---

## 22. FeatureDefinitionFightingStyleChoice

**Purpose:** Grants a selection of fighting styles (for Fighter, Paladin, Ranger).

**Key Fields (inferred from PointPool pattern):**
```json
{
  "$type": "FeatureDefinitionFightingStyleChoice, Assembly-CSharp",
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

---

## Complete Enum Reference

### AbilityScore
`Strength`, `Dexterity`, `Constitution`, `Intelligence`, `Wisdom`, `Charisma`

### DamageType
`DamageAcid`, `DamageBludgeoning`, `DamageCold`, `DamageFire`, `DamageForce`, `DamageLightning`, `DamageNecrotic`, `DamagePiercing`, `DamagePoison`, `DamagePsychic`, `DamageRadiant`, `DamageSlashing`, `DamageThunder`

### DieType
`D1`, `D4`, `D6`, `D8`, `D10`, `D12`, `D20`

### MoveMode
`Walk`, `Fly`, `Swim`, `Climb`, `Burrow`

### SenseType
`NormalVision`, `Darkvision`, `SuperiorDarkvision`, `Blindsight`, `Tremorsense`, `Truesight`, `DetectInvisibility`

### ConditionType
`ConditionCharmed`, `ConditionFrightened`, `ConditionPoisoned`, `ConditionParalyzed`, `ConditionProne`, `ConditionRestrained`, `ConditionBlinded`, `ConditionDeafened`, `ConditionStunned`, `ConditionPetrified`, `ConditionExhausted`, `ConditionDiseased`, `ConditionIncapacitated`, `ConditionInvisible`, `ConditionBranded`, `ConditionRaging`

### SituationalContext (partial list)
`None`, `NotWearingArmor`, `NotWearingArmorOrMageArmor`, `NotWearingHeavyArmor`, `WearingShield`, `WieldingTwoHandedWeapon`, `ConsciousAllyNextToTarget`, `SourceIsBright`, `SourceIsDimOrDarkness`

### Affinity (generic)
`None`, `Advantage`, `Disadvantage`

### DamageAffinityType
`None`, `Resistance`, `Immunity`, `Vulnerability`

### ConditionAffinityType
`None`, `Immunity`, `Advantage`

### ContentCopyright
`OpenGameContent`, `OpenGameContentModified`, `TacticalAdventuresContent`, `TacticalAdventuresContentHidden`

### ContentPack
`BaseGame`, `CrownOfTheMagister`, `ValleyDLC`

---

## Measurement Notes

- **All distances are in CELLS**, not feet. 1 cell = 5 feet.
  - Darkvision 60ft = `senseRange: 12`
  - Speed 30ft = `speed: 6`
  - Barbarian +10ft = `baseSpeedAdditiveModifier: 2`
  - Fly 60ft = `speed: 12`

- **Spell slots array** is indexed `[1st, 2nd, 3rd, 4th, 5th, 6th, 7th, 8th, 9th]`
- **knownCantrips/knownSpells/scribedSpells** arrays are indexed by class level (index 0 = level 1)
- **diceByRankTable** `rank` means class level for `ClassLevel` advancement, or spell slot level for `SlotLevel` advancement

---

## Architecture Summary

The Feature Definition system is a **composition-based ECS-like architecture** where:

1. **Characters** accumulate features through class levels, race, subclass, equipment, and conditions
2. **Each feature type** is a specific C# class inheriting from `FeatureDefinition`
3. **Features reference other definitions** via the `"Definition:[Name]:[guid]"` string pattern
4. **Situational contexts** control when features apply (armor worn, lighting, ally proximity, etc.)
5. **GUI presentation** is separated from game mechanics via the `guiPresentation` sub-object
6. **Scaling** is handled via `diceByRankTable`, level-indexed arrays, or `damageAdvancement` enums
7. **Conditions** can be applied/removed as side effects of features (via `conditionOperations`)

This is the core mechanical backbone - virtually every character ability, racial trait, class feature, item effect, and spell effect in the game is implemented as one or more of these FeatureDefinition subtypes.
