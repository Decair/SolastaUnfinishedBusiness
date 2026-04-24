# Solasta Unfinished Business — API Contracts & Interfaces

## 1. Game Event Interfaces (Sub-Feature Dispatch System)

The mod's primary API is a system of **82 C# interfaces** that define hook points for game events. Implementations are attached to `BaseDefinition` objects as "sub-features" and invoked by Harmony patches at runtime.

### Dispatch Mechanism

```csharp
// Attachment (at initialization):
myFeatureDefinition.AddCustomSubFeatures(new MyAttackHandler());

// Discovery (at runtime, in patches):
List<T> handlers = rulesetCharacter.GetSubFeaturesByType<T>();

// Iteration:
foreach (var handler in handlers)
    yield return handler.OnSomeEvent(...);
```

The dispatch searches:
1. All `FeatureDefinition` objects granted by the character's class/subclass/race/feats/items
2. All active `RulesetCondition` objects on the character (and their source definitions)
3. Each definition's own type (if it implements the interface) AND its attached sub-features list

---

## 2. Physical Attack Interfaces

### IPhysicalAttackInitiatedByMe
```csharp
interface IPhysicalAttackInitiatedByMe
{
    IEnumerator OnPhysicalAttackInitiatedByMe(
        GameLocationBattleManager battleManager,
        CharacterAction action,
        GameLocationCharacter attacker,
        GameLocationCharacter defender,
        ActionModifier attackModifier,
        RulesetAttackMode attackMode);
}
```
**When:** After attack declared, before roll. **Use:** Add temporary bonuses, trigger visual effects.

### IPhysicalAttackInitiatedOnMe
```csharp
interface IPhysicalAttackInitiatedOnMe
{
    IEnumerator OnPhysicalAttackInitiatedOnMe(
        GameLocationBattleManager battleManager,
        CharacterAction action,
        GameLocationCharacter attacker,
        GameLocationCharacter defender,
        ActionModifier attackModifier,
        RulesetAttackMode attackMode);
}
```
**When:** You are targeted by a physical attack. **Use:** Defensive reactions (Shield spell trigger).

### IPhysicalAttackInitiatedOnMeOrAlly
Same as above with additional `GameLocationCharacter helper` parameter.
**When:** You or an adjacent ally is targeted. **Use:** Protection fighting style, Sentinel.

### IPhysicalAttackBeforeHitConfirmedOnEnemy
```csharp
interface IPhysicalAttackBeforeHitConfirmedOnEnemy
{
    IEnumerator OnPhysicalAttackBeforeHitConfirmedOnEnemy(
        GameLocationBattleManager battleManager,
        GameLocationCharacter attacker,
        GameLocationCharacter defender,
        ActionModifier actionModifier,
        RulesetAttackMode attackMode,
        bool rangedAttack,
        AdvantageType advantageType,
        List<EffectForm> actualEffectForms,
        bool firstTarget,
        bool criticalHit);
}
```
**When:** Attack hit confirmed, before damage applied. **Use:** Add extra damage dice, modify effect forms.

### IPhysicalAttackBeforeHitConfirmedOnMe
Same signature, defender perspective.
**When:** You are about to take physical damage. **Use:** Damage reduction, defensive abilities.

### IPhysicalAttackFinishedByMe
```csharp
interface IPhysicalAttackFinishedByMe
{
    IEnumerator OnPhysicalAttackFinishedByMe(
        GameLocationBattleManager battleManager,
        CharacterAction action,
        GameLocationCharacter attacker,
        GameLocationCharacter defender,
        RulesetAttackMode attackMode,
        RollOutcome rollOutcome,
        int damageAmount);
}
```
**When:** After YOUR attack fully resolves. **Use:** Post-attack triggers (mark target, regain HP).

### IPhysicalAttackFinishedByMeOrAlly
Same + `GameLocationCharacter helper`. **Use:** Pack tactics, coordinated attacks.

### IPhysicalAttackFinishedOnMe / IPhysicalAttackFinishedOnMeOrAlly
Defender/ally perspective. **Use:** Riposte, reactive damage.

---

## 3. Magic Effect Interfaces

### IMagicEffectInitiatedByMe
```csharp
interface IMagicEffectInitiatedByMe
{
    IEnumerator OnMagicEffectInitiatedByMe(
        CharacterAction action,
        RulesetEffect activeEffect,
        GameLocationCharacter attacker,
        List<GameLocationCharacter> targets);
}
```

### IMagicEffectAttackInitiatedOnMe
```csharp
interface IMagicEffectAttackInitiatedOnMe
{
    IEnumerator OnMagicEffectAttackInitiatedOnMe(
        CharacterAction action,
        RulesetEffect activeEffect,
        GameLocationCharacter attacker,
        GameLocationCharacter defender,
        ActionModifier attackModifier,
        bool firstTarget,
        bool checkMagicalAttackDamage);
}
```

### IMagicEffectBeforeHitConfirmedOnEnemy / OnMe
Same pattern as physical attack — modify spell damage/effects before application.

### IMagicEffectFinishedByMe / ByMeOrAlly / OnMe
Post-resolution hooks for magic effects.

### IPowerOrSpellInitiatedByMe / FinishedByMe
```csharp
interface IPowerOrSpellInitiatedByMe
{
    IEnumerator OnPowerOrSpellInitiatedByMe(CharacterActionUsePower action, BaseDefinition baseDefinition);
}
interface IPowerOrSpellFinishedByMe
{
    IEnumerator OnPowerOrSpellFinishedByMe(CharacterActionUsePower action, BaseDefinition baseDefinition);
}
```

### IOnSpellCasted
```csharp
interface IOnSpellCasted
{
    int Priority { get; }
    IEnumerator OnSpellCasted(
        RulesetCharacter featureOwner,
        RulesetCharacter caster,
        CharacterActionCastSpell castAction,
        RulesetEffectSpell effectSpell,
        RulesetSpellRepertoire repertoire,
        SpellDefinition spellDefinition);
}
```
Priority-sorted — multiple handlers execute in order.

---

## 4. Attack Outcome Alteration

### ITryAlterOutcomeAttack
```csharp
interface ITryAlterOutcomeAttack
{
    int HandlerPriority { get; }
    IEnumerator OnTryAlterOutcomeAttack(
        GameLocationBattleManager instance,
        CharacterAction action,
        GameLocationCharacter attacker,
        GameLocationCharacter defender,
        GameLocationCharacter helper,
        ActionModifier actionModifier,
        RulesetAttackMode attackMode,
        RulesetEffect rulesetEffect);
}
```
**Priority semantics:**
- Negative priority → runs BEFORE hit is confirmed (can prevent hit)
- Non-negative → runs AFTER successful hit only

**Use:** Shield spell, Silvery Barbs, Lucky, Cutting Words.

### ITryAlterOutcomeSavingThrow
```csharp
interface ITryAlterOutcomeSavingThrow
{
    IEnumerator OnTryAlterOutcomeSavingThrow(
        GameLocationBattleManager battleManager,
        GameLocationCharacter attacker,
        GameLocationCharacter defender,
        GameLocationCharacter helper,
        SavingThrowData savingThrowData,
        bool hasHitVisual);
}
```
**Note:** Checked on ALL characters in encounter, not just the defender. The `helper` parameter identifies who is offering assistance.

### ITryAlterOutcomeAttributeCheck
```csharp
interface ITryAlterOutcomeAttributeCheck
{
    IEnumerator OnTryAlterAttributeCheck(
        GameLocationBattleManager battleManager,
        int rawRoll,
        AbilityCheckData abilityCheckData,
        GameLocationCharacter defender,
        GameLocationCharacter helper);
}
```

---

## 5. Saving Throw Interfaces

### IRollSavingThrowInitiated
```csharp
interface IRollSavingThrowInitiated
{
    void OnSavingThrowInitiated(
        RulesetActor caster,
        RulesetActor defender,
        ref int saveBonus,
        ref string abilityScoreName,
        BaseDefinition sourceDefinition,
        List<TrendInfo> modifierTrends,
        List<TrendInfo> advantageTrends,
        ref int rollModifier,
        ref int saveDC,
        ref bool hasHitVisual,
        RollOutcome outcome,
        int outcomeDelta,
        List<EffectForm> effectForms);
}
```
Modify save parameters BEFORE the roll. Can change DC, ability, bonuses.

### IRollSavingThrowFinished
Same signature with `ref RollOutcome outcome, ref int outcomeDelta`. Modify results AFTER rolling.

---

## 6. Modifier Interfaces

### IModifyAttackActionModifier
```csharp
interface IModifyAttackActionModifier
{
    void OnAttackComputeModifier(
        RulesetCharacter myself,
        RulesetCharacter defender,
        BattleDefinitions.AttackProximity attackProximity,
        RulesetAttackMode attackMode,
        string effectName,
        ref int attackModifier);
}
```

### IModifyAttackCriticalThreshold
```csharp
interface IModifyAttackCriticalThreshold
{
    int GetCriticalThreshold(int current, RulesetCharacter me, RulesetCharacter target, BaseDefinition attackMethod);
}
```

### IModifyWeaponAttackMode
```csharp
interface IModifyWeaponAttackMode
{
    void ModifyWeaponAttackMode(
        RulesetCharacter character,
        RulesetAttackMode attackMode,
        RulesetItem weapon,
        bool canAddAbilityDamageBonus);
}
```
Called during attack mode refresh. Modify damage dice, properties, range.

### IModifyEffectDescription
```csharp
interface IModifyEffectDescription
{
    bool IsValid(BaseDefinition definition, RulesetCharacter character, EffectDescription effectDescription);
    EffectDescription GetEffectDescription(
        BaseDefinition definition,
        EffectDescription effectDescription,
        RulesetCharacter character,
        RulesetEffect rulesetEffect);
}
```
Dynamically alter spell/power effects based on character state.

### IModifyDamageAffinity
```csharp
interface IModifyDamageAffinity
{
    void ModifyDamageAffinity(RulesetActor defender, RulesetActor attacker, List<FeatureDefinition> features);
}
```
Add/remove resistance/immunity/vulnerability entries.

### IModifyDiceRoll
```csharp
interface IModifyDiceRoll
{
    void BeforeRoll(RollContext rollContext, RulesetCharacter character, ref DieType dieType, ref AdvantageType advantageType);
    void AfterRoll(RollContext rollContext, RulesetCharacter character, ref int firstRoll, ref int secondRoll, ref int result);
}
```
Intercept ANY die roll — change die type, force advantage, override results.

### IModifyAbilityCheck
```csharp
interface IModifyAbilityCheck
{
    void MinRoll(
        RulesetCharacter character,
        int baseBonus,
        string abilityScoreName,
        string proficiencyName,
        List<TrendInfo> advantageTrends,
        List<TrendInfo> modifierTrends,
        ref int rollModifier,
        ref int minRoll);
}
```
Set minimum roll values (Reliable Talent, Silver Tongue).

### IModifyPowerPoolAmount
```csharp
interface IModifyPowerPoolAmount
{
    FeatureDefinitionPower PowerPool { get; }
    int PoolChangeAmount(RulesetCharacter character);
}
```
Dynamically change power pool sizes (e.g., additional Channel Divinity uses from specific levels).

### IModifyAC
```csharp
interface IModifyAC
{
    void ModifyAC(RulesetCharacter owner, bool callRefresh, bool dryRun, FeatureDefinition dryRunFeature, RulesetAttribute armorClass);
}
```

### IModifyConcentrationRequirement
```csharp
interface IModifyConcentrationRequirement
{
    bool RequiresConcentration(RulesetCharacter character, RulesetEffectSpell effectSpell);
}
```

### IModifyMovementSpeedAddition
```csharp
interface IModifyMovementSpeedAddition
{
    int ModifySpeedAddition(RulesetCharacter character, IMovementAffinityProvider provider);
}
```

---

## 7. Turn & Battle Lifecycle Interfaces

### ICharacterBeforeTurnStartListener / ICharacterTurnStartListener / ICharacterBeforeTurnEndListener
```csharp
interface ICharacterTurnStartListener { void OnCharacterTurnStarted(GameLocationCharacter locationCharacter); }
interface ICharacterBeforeTurnEndListener { void OnCharacterBeforeTurnEnded(GameLocationCharacter locationCharacter); }
```

### ICharacterBattleStartedListener / ICharacterBattleEndedListener
```csharp
interface ICharacterBattleStartedListener { void OnCharacterBattleStarted(GameLocationCharacter locationCharacter, bool surprise); }
interface ICharacterBattleEndedListener { void OnCharacterBattleEnded(GameLocationCharacter locationCharacter); }
```

### IInitiativeEndListener
```csharp
interface IInitiativeEndListener { IEnumerator OnInitiativeEnded(GameLocationCharacter locationCharacter); }
```

### IActionFinishedByMe
```csharp
interface IActionFinishedByMe { IEnumerator OnActionFinishedByMe(CharacterAction action); }
```

---

## 8. Condition Interfaces

### IOnConditionAddedOrRemoved
```csharp
interface IOnConditionAddedOrRemoved
{
    void OnConditionAdded(RulesetCharacter target, RulesetCondition rulesetCondition);
    void OnConditionRemoved(RulesetCharacter target, RulesetCondition rulesetCondition);
}
```

### IForceConditionCategory
```csharp
interface IForceConditionCategory
{
    string GetForcedCategory(RulesetActor actor, RulesetCondition newCondition, string category);
}
```

---

## 9. Damage & Kill Interfaces

### IOnReducedToZeroHpByEnemy
```csharp
interface IOnReducedToZeroHpByEnemy
{
    IEnumerator HandleReducedToZeroHpByEnemy(
        RulesetCharacter attacker,
        RulesetCharacter source,
        RulesetAttackMode attackMode,
        RulesetEffect activeEffect);
}
```

### IOnReducedToZeroHpByMe / ByMeOrAlly
```csharp
interface IOnReducedToZeroHpByMe
{
    IEnumerator HandleReducedToZeroHpByMe(
        RulesetCharacter attacker,
        RulesetCharacter downedCreature,
        RulesetAttackMode attackMode,
        RulesetEffect activeEffect);
}
```

### IForceMaxDamageTypeDependent
```csharp
interface IForceMaxDamageTypeDependent { bool IsValid(RulesetActor rulesetActor, DamageForm damageForm); }
```

### IAllowRerollDice
```csharp
interface IAllowRerollDice { bool IsValid(RulesetActor rulesetActor, bool attackModeDamage, DamageForm damageForm); }
```

---

## 10. Targeting & Filtering Interfaces

### IFilterTargetingCharacter
```csharp
interface IFilterTargetingCharacter
{
    bool EnforceFullSelection { get; }
    bool IsValid(CursorLocationSelectTarget cursorInstance, GameLocationCharacter target);
}
```

### IFilterTargetingPosition
```csharp
interface IFilterTargetingPosition
{
    IEnumerator ComputeValidPositions(CursorLocationSelectPosition cursorLocationSelectPosition);
}
```

### IFilterRulesetEffectTargets
```csharp
interface IFilterRulesetEffectTargets
{
    bool CanAffectTarget(RulesetEffect rulesetEffect, RulesetCharacter caster, RulesetCharacter target);
}
```

---

## 11. Prevention / Immunity Interfaces

| Interface | Type | Description |
|-----------|------|-------------|
| `IPreventEnemySenseMode` | Methods | Block specific senses on enemies |
| `IPreventRemoveConcentrationOnDamage` | Methods | Bypass concentration saves |
| `IPreventRemoveConcentrationOnPowerUse` | Marker | Keep concentration on power use |
| `IPreventRemoveEffectOnLocationChange` | Methods | Keep effects on travel |
| `IIgnoreAoOImmunity` | Methods | Pierce AoO immunity (Sentinel) |
| `IIgnoreAoOOnMe` | Methods | Prevent AoO triggers on self |
| `IIgnoreInvisibilityInterruptionCheck` | Marker | Don't break invisibility |
| `IRemoveSpellOrSpellLevelImmunity` | Methods | Pierce spell immunities |

---

## 12. Validation Interfaces

### IValidatePowerUse
```csharp
interface IValidatePowerUse { bool CanUsePower(RulesetCharacter character, FeatureDefinitionPower power); }
```

### IValidateDefinitionApplication
```csharp
interface IValidateDefinitionApplication { bool IsValid(BaseDefinition definition, RulesetCharacter character); }
```

### IValidateContextInsteadOfRestrictedProperty
```csharp
interface IValidateContextInsteadOfRestrictedProperty
{
    (OperationType, bool) ValidateContext(
        BaseDefinition definition,
        IRestrictedContextProvider provider,
        RulesetCharacter character,
        ItemDefinition itemDef,
        bool rangedAttack,
        RulesetAttackMode attackMode,
        RulesetEffect rulesetEffect);
}
```

---

## 13. UI / Display Interfaces

### ICustomPortraitPointPoolProvider
```csharp
interface ICustomPortraitPointPoolProvider
{
    string Name { get; }
    Sprite Icon { get; }
    bool IsActive(RulesetCharacter character);
    string Tooltip(RulesetCharacter character);
    string GetPoints(RulesetCharacter character);
}
```

### ICustomReactionResource
```csharp
interface ICustomReactionResource
{
    Sprite Icon { get; }
    string GetUses(RulesetCharacter character);
}
```

### IActionItemDiceBox
```csharp
interface IActionItemDiceBox
{
    (DieType dieType, int number, string format) GetDiceInfo(RulesetCharacter character);
}
```

### ILimitEffectInstances
```csharp
interface ILimitEffectInstances
{
    string Name { get; }
    int GetLimit(RulesetCharacter character);
}
```

---

## 14. Builder API Contracts

### Base Pattern (all builders)
```csharp
TBuilder Create(string name)                    // New from scratch
TBuilder Create(TDefinition original, string name) // Clone existing
TBuilder SetGuiPresentation(Category category, Sprite sprite = null, bool hidden = false)
TBuilder SetGuiPresentation(string title, string description, Sprite sprite = null)
TBuilder AddCustomSubFeatures(params object[] features)
TDefinition AddToDB()                           // Register + return
```

### SpellDefinitionBuilder
```csharp
SetSpellLevel(int level)
SetSchoolOfMagic(SchoolOfMagicDefinition school)
SetCastingTime(ActivationTime time)
SetRequiresConcentration(bool value)
SetRitualCasting(ActivationTime time)
SetMaterialComponent(MaterialComponentType type)
SetVerboseComponent(bool value)
SetSomaticComponent(bool value)
SetEffectDescription(EffectDescription effect)
SetSubSpells(params SpellDefinition[] spells)
SetSpellsBundle(bool value)
SetUniqueInstance(bool value)
```

### EffectDescriptionBuilder
```csharp
SetTargetingData(Side side, RangeType range, int rangeParameter, TargetType type, int targetParameter = 0, int maxTargets = 0)
SetDurationData(DurationType type, int parameter = 0, TurnOccurenceType endTurn = TurnOccurenceType.EndOfTurn)
SetSavingThrowData(bool disableSavingThrowOnAllies, string savingThrowAbility, bool ignoreCover, EffectDifficultyClassComputation computation, string computeAbility = "")
SetEffectAdvancement(EffectIncrementMethod method, int additionalDicePerIncrement = 0, int additionalHPPerIncrement = 0)
SetEffectForms(params EffectForm[] forms)
SetParticleEffectParameters(SpellDefinition referenceSpell)
SetRecurrentEffect(RecurrentEffect recurrence)
Build()
```

### EffectFormBuilder
```csharp
SetDamageForm(string damageType, int diceNumber, DieType dieType, int bonusDamage = 0)
SetConditionForm(ConditionDefinition condition, ConditionForm.ConditionOperation operation)
SetMotionForm(MotionForm.MotionType type, int distance = 0)
SetHealingForm(HealingComputation computation, int diceNumber, DieType dieType, int bonusHP = 0)
SetSummonCreatureForm(int number, string monsterDefinitionName)
SetTempHPForm(int diceNumber, DieType dieType, int bonusHP = 0)
HasSavingThrow(EffectSavingThrowType type)
SetLevelAdvancement(EffectForm.LevelApplianceType type, LevelSourceType source, int denominator = 1)
Build()
```

### ConditionDefinitionBuilder
```csharp
SetConditionType(RuleDefinitions.ConditionType type)
SetDuration(DurationType type, int parameter = 0)
SetSpecialDuration(DurationType type, int parameter, TurnOccurenceType endTurn)
SetTurnOccurence(TurnOccurenceType type)
SetConditionParticleReference(AssetReference reference)
SetRecurrentEffectForms(params EffectForm[] forms)
SetFeatures(params FeatureDefinition[] features)
AddFeatures(params FeatureDefinition[] features)
SetSpecialInterruptions(params ConditionInterruption[] interruptions)
AddCustomSubFeatures(params object[] features)
```

### FeatureDefinitionPowerBuilder
```csharp
SetUsesFixed(ActivationTime activation, int fixedUses = 1, RechargeRate recharge = RechargeRate.LongRest)
SetUsesAbilityBonus(ActivationTime activation, RechargeRate recharge, string abilityScore)
SetUsesProficiencyBonus(ActivationTime activation, RechargeRate recharge = RechargeRate.LongRest)
SetEffectDescription(EffectDescription effect)
SetOverriddenPower(FeatureDefinitionPower overridden)
SetShowCasting(bool value)
SetDisableIfConditionIsOwned(ConditionDefinition condition)
SetExplicitAbilityScore(string abilityScore)
```

### CharacterSubclassDefinitionBuilder
```csharp
SetGuiPresentation(Category category, Sprite sprite)
AddFeaturesAtLevel(int level, params FeatureDefinition[] features)
AddToDB()
```

### FeatDefinitionBuilder
```csharp
SetFeatures(params FeatureDefinition[] features)
SetAbilityScorePrerequisite(string ability, int minimum)
SetMustCastSpellsPrerequisite()
SetFeatFamily(string family)
AddCustomSubFeatures(params object[] features)
```

---

## 15. Validator Delegate Signatures

```csharp
// Character validation (is feature applicable to this character?)
delegate bool IsCharacterValidHandler(RulesetCharacter character);

// Weapon validation (is feature applicable to this weapon/attack?)
delegate bool IsWeaponValidHandler(RulesetAttackMode attackMode, RulesetItem rulesetItem, RulesetCharacter character);

// Power use validation
delegate bool IsPowerUseValidHandler(RulesetCharacter character, FeatureDefinitionPower power);

// Feat prerequisite (returns result + display message)
delegate (bool result, string output) ValidateFeatPrerequisite(
    FeatDefinition feat, RulesetCharacterHero hero);

// Context override
delegate (OperationType, bool) IsContextValidHandler(
    BaseDefinition definition, IRestrictedContextProvider provider,
    RulesetCharacter character, ItemDefinition itemDef,
    bool rangedAttack, RulesetAttackMode attackMode, RulesetEffect rulesetEffect);
```

---

## 16. Configuration Schema

### Settings Properties (key subset)

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `EnableMulticlass` | bool | false | Allow multiclassing |
| `MaxAllowedClasses` | int | 2 | Max classes per character |
| `EnableLevel20` | bool | false | Allow leveling past 12 |
| `EnableFlanking` | bool | false | Optional flanking rules |
| `EnableActionSwitching` | bool | false | Action economy flexibility |
| `EnableTabletop2014` | bool | false | 2014 PHB rules |
| `EnableTabletop2024` | bool | false | 2024 PHB rules |
| `SpellEnabled{Name}` | bool | varies | Per-spell toggle |
| `SubclassEnabled{Name}` | bool | varies | Per-subclass toggle |
| `FeatEnabled{Name}` | bool | varies | Per-feat toggle |

### Info.json (UMM Manifest)
```json
{
    "Id": "SolastaUnfinishedBusiness",
    "DisplayName": "Solasta Unfinished Business",
    "Author": "Various",
    "Version": "1.5.97.126",
    "GameVersion": "1.5.97",
    "ManagerVersion": "0.27.10",
    "EntryMethod": "SolastaUnfinishedBusiness.Main.Load",
    "Requirements": [],
    "HomePage": "https://github.com/EnderWiggin/SolastaUnfinishedBusiness"
}
```

---

## 17. Event / Message System

### Game Service Events Used
| Service | Event | Mod Reaction |
|---------|-------|--------------|
| `IRuntimeService` | `RuntimeLoaded` | Triggers LateLoad phase |
| `IGameLocationBattleService` | Battle events | Combat interface dispatch |
| `IGameLocationActionService` | Action events | Action patches |
| `IGameLocationCharacterService` | Character events | Turn lifecycle |

### Custom Events (via UsedSpecialFeatures)
The mod tracks per-turn state using the game's `Dictionary<string, int> UsedSpecialFeatures` on `RulesetCharacter`:
```csharp
character.UsedSpecialFeatures["MyFeatureName"] = 1; // Track usage
character.UsedSpecialFeatures.TryGetValue("MyFeatureName", out var count); // Check
// Automatically cleared on turn start by game engine
```

---

## 18. Blueprint JSON Schema (Diagnostic Output)

```json
{
    "$type": "TypeName, Assembly-CSharp",
    "name": "DefinitionUniqueName",
    "guid": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx",
    "contentCopyright": "UserContent",
    "contentPack": 9999,
    "guiPresentation": {
        "title": "Category/&NameTitle",
        "description": "Category/&NameDescription",
        "spriteReference": { "m_AssetGUID": "...", "m_SubObjectName": "" },
        "sortOrder": 0,
        "hidden": false
    },
    // ... type-specific fields (all game properties serialized)
}
```

Blueprint files are diagnostic-only — NOT loaded at runtime. They document the state of definitions after builder construction.
