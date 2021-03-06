﻿using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

//using O21Toolbox.PawnCrafter;

namespace O21Toolbox.PawnConverter
{
    public class PawnConvertingDef : Def
    {
        /// <summary>
        ///     The string used to provide the reason for the pawn going berserk.
        /// </summary>
        public string berserkReason = "Botched Conversion";

        /// <summary>
        ///     Adds a chance that the pawn will become berserk.
        /// </summary>
        public float chanceOfBecomingHostile = 0.0f;

        /// <summary>
        ///     Sets amount of time pawns will be in the chamber for during conversion.
        ///     Keep in mind if they leave the chamber during this time it will cancel the conversion.
        ///     One Rimworld day is 60000 (60k).
        /// </summary>
        public int conversionTime = 0; // Needs More Testing.

        /// <summary>
        ///     Whether or not a pawn drops equipment, apparel and inventory on change, this
        ///     may be required for some conversions in cases where the projected race cannot
        ///     use items held by the previous race.
        /// </summary>
        public bool dropEverything = false;

        /// <summary>
        ///     Forces the body type to whatever is defined, requires an actual bodyType like Thin, Fat, Male, Female or Hulk.
        /// </summary>
        public BodyTypeDef forcedBody = null; // Needs More Testing.

        /// <summary>
        ///     Use either randomHair or forcedHair, not both.
        ///     forcedHair is a defined hairDef to choose, must match exactly.
        /// </summary>
        public HairDef forcedHair = null; // Needs More Testing.

        /// <summary>
        ///     Sets if to force the hair colour. Do not use with randomHairColor.
        /// </summary>
        public bool forcedHairColor = false; //Needs More Testing.

        public bool forcedHairColorOne = false; // Needs More Testing.
        public bool forcedHairColorTwo = false; // Needs More Testing.

        /// <summary>
        ///     Forces the crownType of a pawn, must match the texture exactly. Writing "RANDOM" instead will use the new random
        ///     pawns head type, useful for if there are no direct matches.
        /// </summary>
        public string forcedHead = null; // Needs More Testing.

        /// <summary>
        ///     Hediffs to force onto the output pawn, if they already have the hediff it will not be applied.
        /// </summary>
        public Hediff forcedHediff = null; // Needs More Testing.

        /// <summary>
        ///     Sets if to change skin colour during conversion.
        ///     Use this OR randomSkinColor, not both.
        /// </summary>
        public bool forcedSkinColor = false; // Needs More Testing.

        /// <summary>
        ///     Defines colours to use if forcedSkinColor is set to True.
        /// </summary>
        public Color forcedSkinColorOne = new Color(0, 0, 0); // Needs More Testing.

        public Color forcedSkinColorTwo = new Color(0, 0, 0); // Needs More Testing.

        /// <summary>
        ///     Sets the colours for if forcedHairColor is True.
        /// </summary>
        public Color hairColorOne = new Color(0, 0, 0); // Needs More Testing.

        public Color hairColorTwo = new Color(0, 0, 0); // Needs More Testing.

        public bool hediffSeverityMatters = false; // Needs More Testing.

        /// <summary>
        ///     Viable inputs for conversion. Uses the defName of the race itself. Should be able to accept any pawn race if left
        ///     'null'.
        /// </summary>
        public List<ThingDef> inputDefs = null; // Done.

        /// <summary>
        ///     Forces the pawns faction to the player if it isn't already.
        /// </summary>
        public bool makeFriendly = true;

        /// <summary>
        ///     In what order to display this in menus.
        /// </summary>
        public int orderID = 0;

        /// <summary>
        ///     Target output for converts. Uses pawnKinds to retrieve a target race as well as generating the random options for
        ///     other variables.
        ///     Doesn't change race at all if left 'null'.
        /// </summary>
        public PawnKindDef outputDef = null; // Done.

        /// <summary>
        ///     Changes sex/gender of the pawn during conversion. Accepts Male or Female.
        /// </summary>
        public string outputSex = null; // Needs More Testing.

        /// <summary>
        ///     Use either randomHair or forcedHair, not both.
        ///     randomHair is a True or False option on if the converter should choose at random from race appropriate hairs.
        /// </summary>
        public bool randomHair = false;

        /// <summary>
        ///     Sets if to randomize hair colour. Do not use with forcedHairColor.
        /// </summary>
        public bool randomHairColor = false; // Needs More Testing.

        /// <summary>
        ///     Sets the skin colour to a random selection from the appropriate race skin colours.
        ///     Use this OR forcedSkinColor, not both.
        /// </summary>
        public bool randomSkinColor = false; // Needs More Testing.

        /// <summary>
        ///     Building DefNames for converters capable of performing this conversion.
        ///     Leave null if the recipe is only to be used for hediff based conversions.
        /// </summary>
        public List<string> recipeUsers = null;

        /// <summary>
        ///     Removes the hediffs listed in requiredHediffs during conversion.
        /// </summary>
        public bool removeRequiredHediffs = false; // Needs More Testing.

        /// <summary>
        ///     Sets required hediffs, all listed hediffs are required not just one or some.
        /// </summary>
        public List<HediffDef> requiredHediffs = null; // Needs More Testing.

        public float requiredHediffSeverity = 1f; // Needs More Testing.

        /// <summary>
        ///     Research required in order to perform conversion.
        /// </summary>
        public ResearchProjectDef requiredResearch;

        /// <summary>
        ///     Sets if the input pawn needs to be a specific sex. Accepts Male or Female, leaving 'null' will ignore the
        ///     requirement.
        /// </summary>
        public string requiredSex = null; // Done.
    }
}