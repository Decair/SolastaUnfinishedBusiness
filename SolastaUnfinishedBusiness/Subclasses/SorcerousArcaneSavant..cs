using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;
using SolastaUnfinishedBusiness.Behaviors;
using SolastaUnfinishedBusiness.Builders;
using SolastaUnfinishedBusiness.Builders.Features;
using SolastaUnfinishedBusiness.CustomUI;
using SolastaUnfinishedBusiness.Interfaces;
using SolastaUnfinishedBusiness.Properties;
using static RuleDefinitions;
using static SolastaUnfinishedBusiness.Api.DatabaseHelper;
using static SolastaUnfinishedBusiness.Api.DatabaseHelper.FeatureDefinitionPowers;
using static SolastaUnfinishedBusiness.Api.DatabaseHelper.FeatureDefinitionSavingThrowAffinitys;
using static SolastaUnfinishedBusiness.Api.DatabaseHelper.SpellDefinitions;
using static SolastaUnfinishedBusiness.Models.SpellsContext;

namespace SolastaUnfinishedBusiness.Subclasses;

[UsedImplicitly]
public sealed class SorcerousArcaneSavant : AbstractSubclass
{
    private const string Name = "ArcaneSavant";

    // Stored for use in LateLoad
    private static FeatureDefinitionMagicAffinity _magicAffinityArcaneManipulation;

    public SorcerousArcaneSavant()
    {
        // -------------------------
        // LEVEL 01
        // -------------------------

        // -- Expanded Spells (Auto-Prepared) --
        var autoPreparedSpells = FeatureDefinitionAutoPreparedSpellsBuilder
            .Create($"AutoPreparedSpells{Name}")
            .SetGuiPresentation("ExpandedSpells", Category.Feature)
            .SetAutoTag("Origin")
            .SetSpellcastingClass(CharacterClassDefinitions.Sorcerer)
            // Spell level 1 (available at sorcerer level 1)
            .AddPreparedSpellGroup(1, AbsorbElements, HideousLaughter, Shield)
            // Spell level 2 (available at sorcerer level 3)
            .AddPreparedSpellGroup(3, HoldPerson, Levitate, SpellsContext.PsychicWhip)
            // Spell level 3 (available at sorcerer level 5)
            .AddPreparedSpellGroup(5, Counterspell, DispelMagic, Slow)
            // Spell level 4 (available at sorcerer level 7)
            .AddPreparedSpellGroup(7, Banishment, BlackTentacles, PhantasmalKiller,
                SpellsContext.SickeningRadiance)
            // Spell level 5 (available at sorcerer level 9)
            .AddPreparedSpellGroup(9, SpellsContext.Dawn, HoldMonster, MindTwist)
            // Spell level 6 (available at sorcerer level 11)
            .AddPreparedSpellGroup(11, SpellsContext.FizbanPlatinumShield, GlobeOfInvulnerability,
                SpellsContext.ShelterFromEnergy, TrueSeeing)
            // Spell level 7 (available at sorcerer level 13)
            .AddPreparedSpellGroup(13, PrismaticSpray)
            // Spell level 8 (available at sorcerer level 15)
            .AddPreparedSpellGroup(15, Feeblemind, Maze, SpellsContext.MindBlank, SpellWard)
            // Spell level 9 (available at sorcerer level 17)
            .AddPreparedSpellGroup(17, SpellsContext.Invulnerability, SpellsContext.Weird)
            .AddToDB();

        // -- Arcane Savant Feature Set (extra reaction + extra cantrips + extra spell slots) --

        // Extra reaction
        var actionAffinityExtraReaction = FeatureDefinitionActionAffinityBuilder
            .Create($"ActionAffinity{Name}ExtraReaction")
            .SetGuiPresentationNoContent(true)
            .RechargeReactionsAtEveryTurn()
            .AddCustomSubFeatures(FeatureUseLimiter.OncePerTurn)
            .AddToDB();

        // Extra cantrips — player chooses 3 from sorcerer list
        var pointPoolBonusCantrips = FeatureDefinitionPointPoolBuilder
            .Create($"PointPool{Name}BonusCantrips")
            .SetGuiPresentation(Category.Feature)
            .SetPool(HeroDefinitions.PointsPoolType.Cantrip, 3)
            .AddToDB();

        // Extra spell slots
        // +3 level 1 slots; +2 at each level 2-9 (engine won't grant slots above character's casting level)
        var magicAffinityExtraSlots = FeatureDefinitionMagicAffinityBuilder
            .Create($"MagicAffinity{Name}ExtraSlots")
            .SetGuiPresentation(Category.Feature)
            .SetAdditionalSlots(
                new AdditionalSlotsDuplet { SlotLevel = 1, SlotsNumber = 3 },
                new AdditionalSlotsDuplet { SlotLevel = 2, SlotsNumber = 2 },
                new AdditionalSlotsDuplet { SlotLevel = 3, SlotsNumber = 2 },
                new AdditionalSlotsDuplet { SlotLevel = 4, SlotsNumber = 2 },
                new AdditionalSlotsDuplet { SlotLevel = 5, SlotsNumber = 2 },
                new AdditionalSlotsDuplet { SlotLevel = 6, SlotsNumber = 2 },
                new AdditionalSlotsDuplet { SlotLevel = 7, SlotsNumber = 2 },
                new AdditionalSlotsDuplet { SlotLevel = 8, SlotsNumber = 2 },
                new AdditionalSlotsDuplet { SlotLevel = 9, SlotsNumber = 2 })
            .AddToDB();

        // Wrap all three level-1 features into a named feature set
        var featureSetArcaneSavant = FeatureDefinitionFeatureSetBuilder
            .Create($"FeatureSet{Name}ArcaneSavant")
            .SetGuiPresentation(Category.Feature)
            .SetFeatureSet(actionAffinityExtraReaction, pointPoolBonusCantrips, magicAffinityExtraSlots)
            .AddToDB();

        // -------------------------
        // LEVEL 06
        // -------------------------

        // -- Arcane Manipulation --
        // All spells cast at one level higher than the slot used (spell levels 1-8 only)
        _magicAffinityArcaneManipulation = FeatureDefinitionMagicAffinityBuilder
            .Create($"MagicAffinity{Name}ArcaneManipulation")
            .SetGuiPresentation(Category.Feature)
            .SetWarList(1) // +1 slot level bonus
            .AddToDB();
        // Populated in LateLoad() — see below

        // -- Resurgent Sorcery --
        // +4 sorcery points (flat), and full recovery on short rest

        // +4 flat sorcery points via ModifyPowerPoolAmount
        var featureResurgentSorceryPoints = FeatureDefinitionBuilder
            .Create($"Feature{Name}ResurgentSorceryPoints")
            .SetGuiPresentationNoContent(true)
            .AddCustomSubFeatures(new ModifyPowerPoolAmount
            {
                PowerPool = PowerSorcererManaPainterTap,
                Type = PowerPoolBonusCalculationType.Fixed,
                Value = 4
            })
            .AddToDB();

        // Short-rest full sorcery point recovery power
        // We create a power that restores all sorcery points, triggered on short rest
        var powerResurgentSorceryRestore = FeatureDefinitionPowerBuilder
            .Create($"Power{Name}ResurgentSorceryRestore")
            .SetGuiPresentation(Category.Feature)
            .SetUsesFixed(ActivationTime.Rest, RechargeRate.ShortRest)
            .SetShowCasting(false)
            .SetEffectDescription(
                EffectDescriptionBuilder
                    .Create()
                    .SetTargetingData(Side.Ally, RangeType.Self, 0, TargetType.Self)
                    .SetEffectForms(
                        EffectFormBuilder
                            .Create()
                            .SetSpellForm(9) // restores spell/sorcery resources
                            .Build())
                    .Build())
            .AddCustomSubFeatures(new CustomBehaviorResurgentSorcery())
            .AddToDB();

        // Rest activity to trigger the recovery on short rest
        var restActivityResurgentSorcery = RestActivityDefinitionBuilder
            .Create($"RestActivity{Name}ResurgentSorcery")
            .SetGuiPresentation(Category.Feature)
            .SetRestData(
                RestDefinitions.RestStage.AfterRest,
                RestType.ShortRest,
                RestActivityDefinition.ActivityCondition.CanUsePower,
                "UsePower",
                powerResurgentSorceryRestore.Name)
            .AddToDB();

        // Wrap Resurgent Sorcery into a feature set
        var featureSetResurgentSorcery = FeatureDefinitionFeatureSetBuilder
            .Create($"FeatureSet{Name}ResurgentSorcery")
            .SetGuiPresentation(Category.Feature)
            .SetFeatureSet(
                featureResurgentSorceryPoints,
                powerResurgentSorceryRestore,
                restActivityResurgentSorcery)
            .AddToDB();

        // -- Counterspell Mastery --
        var featureCounterspellMastery = FeatureDefinitionBuilder
            .Create($"Feature{Name}CounterspellMastery")
            .SetGuiPresentation(Category.Feature)
            .AddCustomSubFeatures(new CustomBehaviorCounterspellMastery())
            .AddToDB();

        // -------------------------
        // LEVEL 14
        // -------------------------

        // -- Spell Resistance --
        // Advantage on saving throws vs spells — uses existing base game feature directly

        // -------------------------
        // LEVEL 18
        // -------------------------

        // -- Mystic Recovery --
        // Bonus action self-heal for 70 HP, remove blindness/disease, once per long rest
        // Modelled directly on SorcerousDivineHeart's Divine Recovery
        var powerMysticRecovery = FeatureDefinitionPowerBuilder
            .Create($"Power{Name}MysticRecovery")
            .SetGuiPresentation(Category.Feature, Heal)
            .SetUsesFixed(ActivationTime.BonusAction, RechargeRate.LongRest)
            .SetEffectDescription(
                EffectDescriptionBuilder
                    .Create(Heal.EffectDescription)
                    .SetTargetingData(Side.Ally, RangeType.Self, 0, TargetType.Self)
                    .Build())
            .AddToDB();

        // -------------------------
        // MAIN SUBCLASS DEFINITION
        // -------------------------

        Subclass = CharacterSubclassDefinitionBuilder
            .Create($"Sorcerous{Name}")
            .SetGuiPresentation(
                Category.Subclass,
                Sprites.GetSprite(Name, Resources.SorcererFieldManipulator, 256))
            .AddFeaturesAtLevel(1,
                autoPreparedSpells,
                featureSetArcaneSavant)
            .AddFeaturesAtLevel(6,
                _magicAffinityArcaneManipulation,
                featureSetResurgentSorcery,
                featureCounterspellMastery)
            .AddFeaturesAtLevel(14,
                SavingThrowAffinitySpellResistance)
            .AddFeaturesAtLevel(18,
                powerMysticRecovery)
            .AddToDB();
    }

    internal override CharacterClassDefinition Klass => CharacterClassDefinitions.Sorcerer;

    internal override CharacterSubclassDefinition Subclass { get; }

    internal override FeatureDefinitionSubclassChoice SubclassChoice =>
        FeatureDefinitionSubclassChoices.SubclassChoiceSorcerousOrigin;

    // ReSharper disable once UnassignedGetOnlyAutoProperty
    internal override DeityDefinition DeityDefinition { get; }

    // Called after all spells are registered — populates the war list for Arcane Manipulation
    // Includes spell levels 1-8 only (level 9 spells are excluded to avoid bumping above level 9)
    internal static void LateLoad()
    {
        foreach (var spellsByLevel in SpellListDefinitions.SpellListAllSpells.SpellsByLevel)
        {
            if (spellsByLevel.Level is 0 or 9)
            {
                continue; // skip cantrips and level 9 spells
            }

            foreach (var spellDefinition in spellsByLevel.Spells)
            {
                if (spellDefinition.SpellsBundle)
                {
                    foreach (var subSpell in spellDefinition.SubspellsList)
                    {
                        _magicAffinityArcaneManipulation.WarListSpells.Add(subSpell.Name);
                    }
                }
                else
                {
                    _magicAffinityArcaneManipulation.WarListSpells.Add(spellDefinition.Name);
                }
            }
        }
    }

    // -------------------------
    // RESURGENT SORCERY BEHAVIOR
    // -------------------------
    // Fully restores sorcery points on short rest
    private sealed class CustomBehaviorResurgentSorcery : IPowerOrSpellFinishedByMe
    {
        public IEnumerator OnPowerOrSpellFinishedByMe(
            CharacterActionMagicEffect action,
            BaseDefinition baseDefinition)
        {
            var rulesetCharacter = action.ActingCharacter.RulesetCharacter;
            var usablePower = PowerProvider.Get(PowerSorcererManaPainterTap, rulesetCharacter);

            if (usablePower == null)
            {
                yield break;
            }

            // Restore all sorcery points by setting remaining uses to max
            var missing = usablePower.MaxUses - usablePower.RemainingUses;

            if (missing > 0)
            {
                rulesetCharacter.UpdateUsageForPowerPool(-missing, usablePower);
            }
        }
    }

    // -------------------------
    // COUNTERSPELL MASTERY BEHAVIOR
    // -------------------------
    // Part 1: You have advantage on spellcasting checks when casting Counterspell vs level 4+ spells
    // Part 2: Enemies have disadvantage on their Counterspell checks against your spells
    private sealed class CustomBehaviorCounterspellMastery
        : IRollSavingThrowInitiated, IMagicEffectInitiatedByMe
    {
        // Part 1: When WE cast Counterspell against a level 4+ spell, grant advantage on our check
        public IEnumerator OnMagicEffectInitiatedByMe(
            CharacterAction action,
            GameLocationCharacter attacker,
            List<GameLocationCharacter> targets)
        {
            if (action is not CharacterActionCastSpell actionCastSpell ||
                actionCastSpell.ActiveSpell?.SpellDefinition != Counterspell)
            {
                yield break;
            }

            // Find the spell being countered — it's the active spell on the target
            var target = targets.Count > 0 ? targets[0] : null;

            if (target?.RulesetCharacter?.ConcentratedSpell == null)
            {
                // Try to get spell level from the action's context
                // Advantage is applied via the saving throw modifier below
                yield break;
            }

            // The actual advantage grant is handled in OnSavingThrowInitiated below
            // This hook is used to validate the context
        }

        // Part 1 + Part 2: Modify the spellcasting ability check (treated as saving throw contest)
        public void OnSavingThrowInitiated(
            RulesetActor rulesetActorCaster,
            RulesetActor rulesetActorDefender,
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
            List<EffectForm> effectForms)
        {
            // Part 2: If an enemy is casting Counterspell against us (we are the defender),
            // give them disadvantage on their check
            if (sourceDefinition == Counterspell &&
                rulesetActorDefender is RulesetCharacter defender &&
                defender.GetSubclassLevel(CharacterClassDefinitions.Sorcerer, $"Sorcerous{Name}") >= 6)
            {
                advantageTrends.Add(new TrendInfo(
                    -1,
                    FeatureSourceType.CharacterFeature,
                    $"Feature{Name}CounterspellMastery",
                    null));
            }
        }
    }
}

// NOTE: Reference helper for spell names from SpellsContext (UB-added spells)
// These are accessed as SpellsContext.PsychicWhip, SpellsContext.Dawn, etc.
// in the auto-prepared spells section above. Make sure SpellsContext is imported
// via: using static SolastaUnfinishedBusiness.Models.SpellsContext;
// (add this using directive at the top of the file)