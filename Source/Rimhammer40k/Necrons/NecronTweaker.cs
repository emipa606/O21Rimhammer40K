using System;
using RimWorld;
using Verse;

namespace Rimhammer40k.Necrons
{
    internal class NecronTweaker : DefModExtension
    {
        //percentage of products returned when butchering
        public readonly float corpseButcherProductsRatio = 1.0f;

        //if true, product percentage will round up, to prevent zero from being returned.
        public readonly bool corpseButcherRoundUp = false;

        //tweaks corpse butchering products by importing costs from recipe
        public readonly bool tweakCorpseButcherProducts = true;

        //remove corpse rotting
        public readonly bool tweakCorpseRot = true;

        //add necron resurrection
        public readonly bool tweakNecronResurrect = true;

        //stop pawn from being able to socialize
        public bool canSocialize = false;

        //stop deterioration of skills over time
        public bool noSkillLoss = true;

        //products butchering gives
        public RecipeDef recipeDef;

        //add necron repair ability
        public bool tweakNecronRepair = true;
    }

    [StaticConstructorOnStartup]
    public static class PostInitializationTweaker
    {
        static PostInitializationTweaker()
        {
            //Start tweaking.
            //IEnumerable<ThingDef> corpseDefs = DefDatabase<ThingDef>.AllDefs.Where(thingDef => thingDef.defName.EndsWith("_Corpse"));

            foreach (var thingDef in DefDatabase<ThingDef>.AllDefs)
            {
                //If the Def got a NecronTweaker do stuff, otherwise do not bother.
                var tweaker = thingDef.GetModExtension<NecronTweaker>();
                if (tweaker == null)
                {
                    continue;
                }

                var corpseDef = thingDef.race?.corpseDef;
                if (corpseDef == null)
                {
                    continue;
                }

                //Removes corpse rotting.
                if (tweaker.tweakCorpseRot)
                {
                    corpseDef.comps.RemoveAll(compProperties => compProperties is CompProperties_Rottable);
                    corpseDef.comps.RemoveAll(compProperties => compProperties is CompProperties_SpawnerFilth);
                }

                if (tweaker.tweakNecronResurrect)
                {
                    var compProperties_NecronResurrection = new CompProperties_NecronResurrection();
                    corpseDef.comps.Add(compProperties_NecronResurrection);
                }

                //Modifies the butchering products by importing the costs from a recipe.
                var recipeDef = tweaker.recipeDef;
                if (!tweaker.tweakCorpseButcherProducts || recipeDef == null)
                {
                    continue;
                }

                corpseDef.butcherProducts.Clear();

                foreach (var ingredient in recipeDef.ingredients)
                {
                    var finalCount = 0f;
                    var ingredientThingDef = ingredient.filter.AnyAllowedDef;
                    var requiredCount = ingredient.CountRequiredOfFor(ingredientThingDef, recipeDef);

                    if (tweaker.corpseButcherRoundUp)
                    {
                        finalCount = (float) Math.Ceiling(requiredCount * tweaker.corpseButcherProductsRatio);
                    }
                    else
                    {
                        finalCount = (float) Math.Floor(requiredCount * tweaker.corpseButcherProductsRatio);
                    }
                }
            }
        }
    }
}