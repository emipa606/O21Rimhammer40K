using System.Collections.Generic;
using O21Toolbox.AutomatedProducer;
using Verse;

namespace O21Toolbox.PawnCrafter
{
    /// <summary>
    ///     Defines
    /// </summary>
    public class PawnCraftingDef : Def
    {
        /// <summary>
        ///     The cost to manufacture one Droid.
        /// </summary>
        public List<ThingOrderRequest> costList = new List<ThingOrderRequest>();

        /// <summary>
        ///     Whether or not to render the pawn as an animal.
        /// </summary>
        public bool isAnimalPawn = false;

        /// <summary>
        ///     In what order to display this in menus.
        /// </summary>
        public int orderID = 0;

        /// <summary>
        ///     The Droid kind to spawn upon construction.
        /// </summary>
        public PawnKindDef pawnKind;

        /// <summary>
        ///     Building defName that the pawns recipe should show up in.
        /// </summary>
        public string recipeUser = null;

        /// <summary>
        ///     Research required in order for it to be craftable.
        /// </summary>
        public ResearchProjectDef requiredResearch;

        /// <summary>
        ///     The time it takes to manufacture one Droid.
        /// </summary>
        public int timeCost = 0;

        /// <summary>
        ///     Whether to use the Utility way of creating the Droid or not.
        /// </summary>
        public bool useDroidCreator = false;
    }
}