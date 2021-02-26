using System;
using System.Collections.Generic;
using O21Toolbox.AutomatedProducer;
using RimWorld;
using Verse;

namespace O21Toolbox.PawnCrafter
{
    /// <summary>
    ///     Properties for pawn crafters.
    /// </summary>
    public class PawnCrafterProperties : DefModExtension
    {
        /// <summary>
        ///     The cost to manufacture one Pawn.
        /// </summary>
        public List<ThingOrderRequest> costList = new List<ThingOrderRequest>();

        /// <summary>
        ///     Crafter material need.
        /// </summary>
        public string crafterMaterialNeedText = "PawnCrafterNeed";

        /// <summary>
        ///     Crafter materials text.
        /// </summary>
        public string crafterMaterialsText = "PawnCrafterMaterials";

        /// <summary>
        ///     Crafter nutrition text.
        /// </summary>
        public string crafterNutritionText = "PawnCrafterNutrition";

        /// <summary>
        ///     Crafter progress text.
        /// </summary>
        public string crafterProgressText = "PawnCrafterProgress";

        /// <summary>
        ///     Enum prefix text.
        /// </summary>
        public string crafterStatusEnumText = "PawnCrafterStatusEnum";

        /// <summary>
        ///     Status text on the crafter.
        /// </summary>
        public string crafterStatusText = "PawnCrafterStatus";

        /// <summary>
        ///     Sound played during crafting.
        /// </summary>
        public SoundDef craftingSound;

        /// <summary>
        ///     If true the derived class will handle it.
        /// </summary>
        public bool customCraftingTime = false;

        /// <summary>
        ///     If true the derived class will handle it.
        /// </summary>
        public bool customOrderProcessor = false;

        /// <summary>
        ///     If not skill is named this will be the default skill level.
        /// </summary>
        public int defaultSkillLevel = 6;

        /// <summary>
        ///     Races that are not possible to print in this printer.
        /// </summary>
        public List<ThingDef> disabledRaces = new List<ThingDef>();

        /// <summary>
        ///     Optional Hediff to apply on newly crafted pawn.
        /// </summary>
        public HediffDef hediffOnPawnCrafted;

        /// <summary>
        ///     Whether or not to only list pawns designated to be craftable in this crafter.
        ///     True = Only designated pawns.
        ///     False = Any pawns, as long as they have no designation.
        /// </summary>
        public bool listOnlyAllowed = false;

        /// <summary>
        ///     Label on the letter when finished crafting.
        /// </summary>
        public string pawnCraftedLetterLabel = "PawnCraftedLetterLabel";

        /// <summary>
        ///     Text on the letter when finished crafting.
        /// </summary>
        public string pawnCraftedLetterText = "PawnCraftedLetterDescription";

        /// <summary>
        ///     Pawn kind to craft.
        /// </summary>
        public PawnKindDef pawnKind;

        /// <summary>
        ///     The factor 0.0 - 1.0 in which power is consumed when not crafting.
        /// </summary>
        public float powerConsumptionFactorIdle = 0.1f;

        /// <summary>
        ///     How often a "resource" tick happen in which resources are deducted from internal storage.
        /// </summary>
        public int resourceTick = 2500;

        /// <summary>
        ///     Optional set of skills to give to newly created pawns.
        /// </summary>
        public List<SkillRequirement> skills = new List<SkillRequirement>();

        /// <summary>
        ///     Optional thought to apply on newly crafted pawn.
        /// </summary>
        public ThoughtDef thoughtOnPawnCrafted;

        /// <summary>
        ///     How many ticks are required to craft the pawn.
        /// </summary>
        public int ticksToCraft = 60000;

        /// <summary>
        ///     How many ticks will happen in the crafting period.
        /// </summary>
        /// <returns>Resource ticks.</returns>
        public float ResourceTicks()
        {
            return (float) Math.Ceiling((double) ticksToCraft / resourceTick);
        }
    }
}