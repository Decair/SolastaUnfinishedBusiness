using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;
using SolastaUnfinishedBusiness.Api.GameExtensions;
using SolastaUnfinishedBusiness.Behaviors;
using SolastaUnfinishedBusiness.Builders;
using SolastaUnfinishedBusiness.Builders.Features;
using SolastaUnfinishedBusiness.CustomUI;
using SolastaUnfinishedBusiness.Interfaces;
using SolastaUnfinishedBusiness.Models;
using SolastaUnfinishedBusiness.Properties;
using static SolastaUnfinishedBusiness.Models.SpellsContext;
using static RuleDefinitions;
using static SolastaUnfinishedBusiness.Api.DatabaseHelper;
using static SolastaUnfinishedBusiness.Api.DatabaseHelper.FeatureDefinitionPowers;
using static SolastaUnfinishedBusiness.Api.DatabaseHelper.FeatureDefinitionSavingThrowAffinitys;
using static SolastaUnfinishedBusiness.Api.DatabaseHelper.SpellDefinitions;

namespace SolastaUnfinishedBusiness.Subclasses;

[UsedImplicitly]
public sealed class SorcerousArcaneSavant : AbstractSubclass
{
    private const string Name = "ArcaneSavant";

    // Stored for use in LateLoad()
    private static FeatureDefinitionMagicAffinity _magicAffinityArcaneManipulation;

    public SorcerousArcaneSavant()
    {
        // =====================================================================
        // LEVEL 01
        // =====================================================================

        // ---------------------------------------------------------------------
        // Expanded Spells (Auto-Prepared)
        // ---------------------------------------------------------------------
        var autoPreparedSpells = FeatureDefinitionAutoPreparedSpellsBuilder
            .Create($"AutoPreparedSpells{Name}")
            .SetGuiPresentation("ExpandedSpells", Category.Feature)
            .SetAutoTag("Origin")
            .SetSpellcastingClass(CharacterClassDefinitions.Sorcerer)
            // Spell level 1 — available at sorcerer level 1
            .AddPreparedSpellGroup(1,
                SpellsContext.ElementalInfusion, // ElementalInfusion is the internal name for Absorb Elements
                HideousLaughter,
                Shield)
            // Spell level 2 — available at sorcerer level 3
            .AddPreparedSpellGroup(3,
                HoldPerson,
                Levitate,
                SpellsContext.PsychicWhip)
            // Spell level 3 — available at sorcerer level 5
            .AddPreparedSpellGroup(5,
                Counterspell,
                DispelMagic,
                Slow)
            // Spell level 4 — available at sorcerer level 7
            .AddPreparedSpellGroup(7,
                Banishment,
                BlackTentacles,
                PhantasmalKiller,
                SpellsContext.SickeningRadiance)
            // Spell level 5 — available at sorcerer level 9
            .AddPreparedSpellGroup(9,
                SpellsContext.Dawn,
                HoldMonster,
                MindTwist)
            // Spell level 6 — available at sorcerer level 11
            .AddPreparedSpellGroup(11,
                SpellsContext.FizbanPlatinumShield,
                GlobeOfInvulnerability,
                SpellsContext.ShelterFromEnergy,
                TrueSeeing)
            // Spell level 7 — available at sorcerer level 13
            .AddPreparedSpellGroup(13,
                PrismaticSpray)
            // Spell level 8 — available at sorcerer level 15
            .AddPreparedSpellGroup(15,
                Feeblemind,
                Maze,
                SpellsContext.MindBlank,
                SpellsContext.SpellWardSpell)
            // Spell level 9 — available at sorcerer level 17
            .AddPreparedSpellGroup(17,
                SpellsContext.Invulnerability,
                SpellsContext.Weird)
            .AddToDB();

        // ---------------------------------------------------------------------
        // Arcane Savant Feature Set
        // (extra reaction + 3 bonus cantrips + extra spell slots)
        // ---------------------------------------------------------------------

        // Extra reaction — recharges at the start of every turn (once per turn limit)
        var actionAffinityExtraReaction = FeatureDefinitionActionAffinityBuilder
            .Create($"ActionAffinity{Name}ExtraReaction")
            .SetGuiPresentationNoContent(true)
            .RechargeReactionsAtEveryTurn()
            .AddCustomSubFeatures(FeatureUseLimiter.OncePerTurn)
            .AddToDB();

        // 3 bonus cantrips — player chooses from sorcerer cantrip list at level-up
        // NoContent: sub-feature of FeatureSetArcaneSavant, not shown separately
        var pointPoolBonusCantrips = FeatureDefinitionPointPoolBuilder
            .Create($"PointPool{Name}BonusCantrips")
            .SetGuiPresentationNoContent(true)
            .SetPool(HeroDefinitions.PointsPoolType.Cantrip, 3)
            .AddToDB();

        // Extra spell slots
        // +3 level 1 slots; +2 at each of levels 2-9
        // The engine will not grant slots above the character's natural casting level
        // NoContent: sub-feature of FeatureSetArcaneSavant, not shown separately
        var magicAffinityExtraSlots = FeatureDefinitionMagicAffinityBuilder
            .Create($"MagicAffinity{Name}ExtraSlots")
            .SetGuiPresentationNoContent(true)
            .SetAdditionalSlots(
                new AdditionalSlotsDuplet { slotLevel = 1, slotsNumber = 3 },
                new AdditionalSlotsDuplet { slotLevel = 2, slotsNumber = 2 },
                new AdditionalSlotsDuplet { slotLevel = 3, slotsNumber = 2 },
                new AdditionalSlotsDuplet { slotLevel = 4, slotsNumber = 2 },
                new AdditionalSlotsDuplet { slotLevel = 5, slotsNumber = 2 },
                new AdditionalSlotsDuplet { slotLevel = 6, slotsNumber = 2 },
                new AdditionalSlotsDuplet { slotLevel = 7, slotsNumber = 2 },
                new AdditionalSlotsDuplet { slotLevel = 8, slotsNumber = 2 },
                new AdditionalSlotsDuplet { slotLevel = 9, slotsNumber = 2 })
            .AddToDB();

        // Wrap all three into a named feature set shown at level 1
        var featureSetArcaneSavant = FeatureDefinitionFeatureSetBuilder
            .Create($"FeatureSet{Name}ArcaneSavant")
            .SetGuiPresentation(Category.Feature)
            .SetFeatureSet(
                actionAffinityExtraReaction,
                pointPoolBonusCantrips,
                magicAffinityExtraSlots)
            .AddToDB();

        // =====================================================================
        // LEVEL 06
        // =====================================================================

        // ---------------------------------------------------------------------
        // Arcane Manipulation
        // All spells cast at one spell level higher than the slot used.
        // Applies to spell levels 1-8 only (level 9 excluded — populated in LateLoad).
        // ---------------------------------------------------------------------
        _magicAffinityArcaneManipulation = FeatureDefinitionMagicAffinityBuilder
            .Create($"MagicAffinity{Name}ArcaneManipulation")
            .SetGuiPresentation(Category.Feature)
            .SetWarList(1) // +1 effective slot level
            .AddToDB();
        // War list is populated in LateLoad() below

        // ---------------------------------------------------------------------
        // Resurgent Sorcery
        // +4 sorcery points (flat) and full sorcery point recovery on short rest.
        // ---------------------------------------------------------------------

        // +4 flat sorcery points bonus
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

        // Clone PowerSorcererManaPainterTap as the base for the short-rest recovery power.
        // This follows the same pattern as PowerSorcerousRestoration in 2024SorcererContext.
        // The CustomBehaviorResurgentSorcery zeroes out usedSorceryPoints on activation,
        // achieving full recovery regardless of current sorcery point total.
        var powerResurgentSorceryRestore = FeatureDefinitionPowerBuilder
            .Create(PowerSorcererManaPainterTap, $"Power{Name}ResurgentSorceryRestore")
            .SetOrUpdateGuiPresentation(Category.Feature)
            .AddCustomSubFeatures(
                ModifyPowerVisibility.Hidden,
                new CustomBehaviorResurgentSorcery())
            .AddToDB();

        // Register as a short-rest activity.
        // RestActivityDefinition registers itself into the DB via AddToDB() and
        // does NOT need to be in the feature set.
        RestActivityDefinitionBuilder
            .Create($"RestActivity{Name}ResurgentSorcery")
            .SetGuiPresentation(Category.Feature)
            .SetRestData(
                RestDefinitions.RestStage.AfterRest,
                RestType.ShortRest,
                RestActivityDefinition.ActivityCondition.CanUsePower,
                "UsePower",
                powerResurgentSorceryRestore.Name)
            .AddToDB();

        var featureSetResurgentSorcery = FeatureDefinitionFeatureSetBuilder
            .Create($"FeatureSet{Name}ResurgentSorcery")
            .SetGuiPresentation(Category.Feature)
            .SetFeatureSet(
                featureResurgentSorceryPoints,
                powerResurgentSorceryRestore)
            .AddToDB();

        // ---------------------------------------------------------------------
        // Counterspell Mastery
        // Part 1: Advantage on YOUR check when casting Counterspell.
        //   Since rolls only occur vs level 4+ spells (Counterspell is level 3,
        //   so lower-level spells are auto-countered), unconditional advantage is
        //   functionally identical to "advantage vs level 4+ spells".
        //   Set directly on the MagicAffinity definition (no builder method available).
        // Part 2: Enemies have disadvantage on their Counterspell check against your spells.
        //   Implemented via CustomBehaviorCounterspellMastery (IRollSavingThrowInitiated).
        // ---------------------------------------------------------------------
        var magicAffinityCounterspellAdvantage = FeatureDefinitionMagicAffinityBuilder
            .Create($"MagicAffinity{Name}CounterspellAdvantage")
            .SetGuiPresentationNoContent(true)
            .AddToDB();
        // SetCounterspellAffinity doesn't exist as a builder method — set directly on definition
        magicAffinityCounterspellAdvantage.counterspellAffinity = AdvantageType.Advantage;

        var featureCounterspellMasteryBehavior = FeatureDefinitionBuilder
            .Create($"Feature{Name}CounterspellMastery")
            .SetGuiPresentationNoContent(true)
            .AddCustomSubFeatures(new CustomBehaviorCounterspellMastery())
            .AddToDB();

        var featureSetCounterspellMastery = FeatureDefinitionFeatureSetBuilder
            .Create($"FeatureSet{Name}CounterspellMastery")
            .SetGuiPresentation(Category.Feature)
            .SetFeatureSet(
                magicAffinityCounterspellAdvantage,
                featureCounterspellMasteryBehavior)
            .AddToDB();

        // =====================================================================
        // LEVEL 14
        // =====================================================================

        // Spell Resistance — advantage on saving throws vs spells and magic effects.
        // References the existing base game feature directly. No custom code needed.

        // =====================================================================
        // LEVEL 18
        // =====================================================================

        // Mystic Recovery — bonus action self-heal for 70 HP, removes blindness
        // and disease, usable once per long rest.
        // Modelled directly on SorcerousDivineHeart's Divine Recovery power.
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

        // =====================================================================
        // SUBCLASS DEFINITION
        // =====================================================================

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
                featureSetCounterspellMastery)
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

    // =========================================================================
    // LATE LOAD
    // =========================================================================
    // Must be called from the mod boot sequence alongside other subclass LateLoad()
    // calls. Find the correct location by searching for SorcerousFieldManipulator.LateLoad().
    //
    // Populates the Arcane Manipulation war list with all spells of levels 1-8.
    // Cantrips (level 0) and level 9 spells are excluded.
    internal static void LateLoad()
    {
        foreach (var spellsByLevel in SpellListDefinitions.SpellListAllSpells.SpellsByLevel)
        {
            if (spellsByLevel.Level is 0 or 9)
            {
                continue;
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

    // =========================================================================
    // RESURGENT SORCERY BEHAVIOR
    // =========================================================================
    // Fires after the short-rest recovery power resolves.
    // Zeroes out usedSorceryPoints, restoring the full pool.
    private sealed class CustomBehaviorResurgentSorcery : IPowerOrSpellFinishedByMe
    {
        public IEnumerator OnPowerOrSpellFinishedByMe(
            CharacterActionMagicEffect action,
            BaseDefinition baseDefinition)
        {
            var rulesetCharacter = action.ActingCharacter.RulesetCharacter;

            rulesetCharacter.usedSorceryPoints = 0;
            rulesetCharacter.SorceryPointsAltered?.Invoke(
                rulesetCharacter,
                rulesetCharacter.usedSorceryPoints);

            yield break;
        }
    }

    // =========================================================================
    // COUNTERSPELL MASTERY BEHAVIOR
    // =========================================================================
    // Part 1 — advantage on OUR Counterspell check:
    //   Handled natively via MagicAffinityCounterspellAdvantage above.
    //
    // Part 2 — enemy disadvantage on their Counterspell check against our spells:
    //   When an enemy casts Counterspell against one of our spells, inject a
    //   disadvantage trend into their spellcasting check via IRollSavingThrowInitiated.
    private sealed class CustomBehaviorCounterspellMastery : IRollSavingThrowInitiated
    {
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
            // Only fire when the source is Counterspell
            if (sourceDefinition != Counterspell)
            {
                return;
            }

            // We must be the defender (our spell is being countered)
            if (rulesetActorDefender is not RulesetCharacter defender)
            {
                return;
            }

            // Confirm the defender has this subclass at level 6+
            if (defender.GetSubclassLevel(
                    CharacterClassDefinitions.Sorcerer, $"Sorcerous{Name}") < 6)
            {
                return;
            }

            // Apply disadvantage to the enemy's Counterspell check
            advantageTrends.Add(new TrendInfo(
                -1,
                FeatureSourceType.CharacterFeature,
                $"Feature{Name}CounterspellMastery",
                null));
        }
    }
}