﻿using System.Linq;
using AlienRace;
using O21Toolbox.Utility;
using RimWorld;
using UnityEngine;
using Verse;

namespace O21Toolbox.PawnConverter
{
    public class Util_PawnConvert
    {
        public static bool IsRequiredSex(Pawn pawn, PawnConvertingDef recipe)
        {
            if (recipe.requiredSex == null)
            {
                return true;
            }

            if (recipe.requiredSex != pawn.gender.ToString())
            {
                return false;
            }

            return true;
        }

        public static bool IsViableRace(Pawn pawn, PawnConvertingDef recipe)
        {
            if (recipe.inputDefs == null)
            {
                return true;
            }

            if (!recipe.inputDefs.Contains(pawn.def))
            {
                return false;
            }

            return true;
        }

        public static Pawn PawnConversion(Pawn pawnToConvert, PawnConvertingDef recipe)
        {
            var request = new PawnGenerationRequest(
                recipe.outputDef,
                Faction.OfPlayer,
                forceGenerateNewPawn: true,
                canGeneratePawnRelations: false,
                colonistRelationChanceFactor: 0f,
                fixedBiologicalAge: pawnToConvert.ageTracker.AgeBiologicalYearsFloat,
                fixedChronologicalAge: pawnToConvert.ageTracker.AgeChronologicalYearsFloat,
                allowFood: false);
            var newPawn = PawnGenerator.GeneratePawn(request);
            var pawn = PawnGenerator.GeneratePawn(request);


            // Transfer everything from old pawn to new pawn
            pawn.drugs = pawnToConvert.drugs;
            pawn.foodRestriction = pawnToConvert.foodRestriction;
            // pawn.guilt = pawnToConvert.guilt; - Caused issues with thoughts. Didn't seem necessary.
            // pawn.health = pawnToConvert.health; - Caused issues with taking damage, to the point of making pawns invulnerable. Didn't seem necessary.
            pawn.health.hediffSet.hediffs = pawnToConvert.health.hediffSet.hediffs;
            // pawn.needs = pawnToConvert.needs; - Caused issues with needs. Didn't seem necessary.
            pawn.records = pawnToConvert.records;
            pawn.skills = pawnToConvert.skills;

            TransferStory(pawn, pawnToConvert, recipe);

            pawn.timetable = pawnToConvert.timetable;
            pawn.workSettings = pawnToConvert.workSettings;
            pawn.Name = pawnToConvert.Name;

            if (recipe.outputSex != null)
            {
                var outputGender = GetOutputGender(pawnToConvert, recipe);
            }
            else
            {
                if (pawnToConvert.def.race.hasGenders)
                {
                    pawn.gender = pawnToConvert.gender;
                }
            }

            ApplyHairChange(pawn, newPawn, recipe);

            ApplyHairColor(pawn, newPawn, recipe);

            ApplySkinChange(pawn, newPawn, recipe);

            ApplyHeadChange(pawn, newPawn, recipe);

            ApplyBodyChange(pawn, recipe);

            RemoveRequiredHediffs(pawn, recipe);

            ApplyForcedHediff(pawn, recipe);

            FixPawnRelations(pawn, pawnToConvert);

            FixPawnEquipment(pawn, pawnToConvert, recipe);

            pawn.health.summaryHealth.Notify_HealthChanged();
            pawn.Drawer.renderer.graphics.ResolveAllGraphics();

            if (recipe.makeFriendly)
            {
                pawn.guest?.SetGuestStatus(null);

                if (pawn.Faction != Faction.OfPlayer)
                {
                    pawn.SetFaction(pawn.Faction);
                }
            }

            if (recipe.chanceOfBecomingHostile > 0.0f)
            {
                if (Rand.Chance(recipe.chanceOfBecomingHostile))
                {
                    MentalBreakDefOf.Berserk.Worker.TryStart(pawn, recipe.label, false);
                }
            }

            Util_FactionConvert.FactionConvert(pawn, recipe.makeFriendly, recipe.chanceOfBecomingHostile,
                recipe.berserkReason);

            pawn.health.summaryHealth.Notify_HealthChanged();

            return pawn;
        }

        private static Pawn FixPawnEquipment(Pawn pawn, Pawn pawnToConvert, PawnConvertingDef recipe)
        {
            // No pregenerated equipment.
            pawn?.equipment?.DestroyAllEquipment();
            pawn?.apparel?.DestroyAll();
            pawn?.inventory?.DestroyAll();

            if (recipe.dropEverything)
            {
                pawnToConvert.equipment.DropAllEquipment(pawnToConvert.Position);
                pawnToConvert.apparel.DropAll(pawnToConvert.Position);
                pawnToConvert.inventory.DropAllNearPawn(pawnToConvert.Position);
            }
            else
            {
                // Transfer Old Equipmnet.
                var equipment = pawnToConvert.equipment.GetDirectlyHeldThings().ToList();
                foreach (var eq in equipment)
                {
                    if (eq == null)
                    {
                        continue;
                    }

                    if (pawn?.equipment != null)
                    {
                        pawnToConvert.equipment.GetDirectlyHeldThings()
                            .TryTransferToContainer(eq, pawn.equipment.GetDirectlyHeldThings());
                    }
                }

                var apparels = pawnToConvert.apparel.GetDirectlyHeldThings().ToList();
                foreach (var ap in apparels)
                {
                    if (ap == null)
                    {
                        continue;
                    }

                    if (pawn?.apparel != null)
                    {
                        pawnToConvert.apparel.GetDirectlyHeldThings()
                            .TryTransferToContainer(ap, pawn.apparel.GetDirectlyHeldThings());
                    }
                }

                var items = pawnToConvert.inventory.GetDirectlyHeldThings().ToList();
                foreach (var item in items)
                {
                    if (item == null)
                    {
                        continue;
                    }

                    if (pawn?.equipment != null)
                    {
                        pawnToConvert.inventory.GetDirectlyHeldThings()
                            .TryTransferToContainer(item, pawn.equipment.GetDirectlyHeldThings());
                    }
                }
            }

            return pawn;
        }

        public static Pawn TransferStory(Pawn newPawn, Pawn oldPawn, PawnConvertingDef recipe)
        {
            if (oldPawn.TryGetComp<AlienPartGenerator.AlienComp>() == null ||
                newPawn.TryGetComp<AlienPartGenerator.AlienComp>() == null)
            {
                return newPawn;
            }

            newPawn.story.childhood = oldPawn.story.childhood;
            newPawn.story.adulthood = oldPawn.story.adulthood;
            newPawn.story.title = oldPawn.story.title;
            newPawn.story.traits = oldPawn.story.traits;
            if (recipe.forcedHead == null)
            {
                newPawn.story.crownType = oldPawn.story.crownType;
            }

            if (!recipe.forcedSkinColor && !recipe.randomSkinColor)
            {
                newPawn.TryGetComp<AlienPartGenerator.AlienComp>().ColorChannels.Clear();
                foreach (var exposableValueTuple in oldPawn.TryGetComp<AlienPartGenerator.AlienComp>().ColorChannels)
                {
                    newPawn.TryGetComp<AlienPartGenerator.AlienComp>().ColorChannels[exposableValueTuple.Key] =
                        exposableValueTuple.Value;
                }
            }

            if (!recipe.randomHair && recipe.forcedHair == null)
            {
                newPawn.story.hairDef = oldPawn.story.hairDef;
            }

            if (!recipe.randomHairColor && !recipe.forcedHairColor)
            {
                newPawn.story.hairColor = oldPawn.story.hairColor;
            }

            newPawn.Drawer.renderer.graphics.ResolveAllGraphics();

            return newPawn;
        }

        public static Pawn ApplyHairChange(Pawn pawn, Pawn newPawn, PawnConvertingDef recipe)
        {
            // Change hair if needed.
            if (!recipe.randomHair)
            {
                if (recipe.forcedHair != null)
                {
                    pawn.story.hairDef = recipe.forcedHair;
                }
            }

            if (recipe.randomHair)
            {
                pawn.story.hairDef = newPawn.story.hairDef;
            }

            return pawn;
        }

        public static Pawn ApplyHairColor(Pawn pawn, Pawn newPawn, PawnConvertingDef recipe)
        {
            // Change hair colour if needed.
            if (recipe.forcedHairColor)
            {
                if (recipe.forcedHairColorOne)
                {
                    pawn.story.hairColor = recipe.hairColorOne;
                }
            }

            if (recipe.randomHairColor)
            {
                pawn.story.hairColor = newPawn.story.hairColor;
            }

            pawn.Drawer.renderer.graphics.ResolveAllGraphics();

            return pawn;
        }

        public static Pawn ApplySkinChange(Pawn pawn, Pawn newPawn, PawnConvertingDef recipe)
        {
            //// Change skin colour if needed.
            if (recipe.forcedSkinColor)
            {
                var skinTuple = new AlienPartGenerator.ExposableValueTuple<Color, Color>
                {
                    first = recipe.forcedSkinColorOne, second = recipe.forcedSkinColorTwo
                };
                pawn.TryGetComp<AlienPartGenerator.AlienComp>().ColorChannels["skin"] = skinTuple;
            }

            if (recipe.randomSkinColor)
            {
                pawn.TryGetComp<AlienPartGenerator.AlienComp>().ColorChannels["skin"] =
                    newPawn.TryGetComp<AlienPartGenerator.AlienComp>().ColorChannels["skin"];
            }

            pawn.Drawer.renderer.graphics.ResolveAllGraphics();

            return pawn;
        }

        public static Pawn ApplyHeadChange(Pawn pawn, Pawn newPawn, PawnConvertingDef recipe)
        {
            // Change crown if needed.
            if (recipe.forcedHead == null)
            {
                return pawn;
            }

            if (recipe.forcedHead == "RANDOM")
            {
                pawn.TryGetComp<AlienPartGenerator.AlienComp>().crownType =
                    newPawn.TryGetComp<AlienPartGenerator.AlienComp>().crownType;
                pawn.Drawer.renderer.graphics.ResolveAllGraphics();
            }
            else
            {
                pawn.TryGetComp<AlienPartGenerator.AlienComp>().crownType = recipe.forcedHead;
                pawn.Drawer.renderer.graphics.ResolveAllGraphics();
            }

            return pawn;
        }

        public static Pawn ApplyBodyChange(Pawn pawn, PawnConvertingDef recipe)
        {
            // Change body if needed.
            if (recipe.forcedBody == null)
            {
                return pawn;
            }

            pawn.story.bodyType = recipe.forcedBody;
            pawn.Drawer.renderer.graphics.ResolveAllGraphics();

            return pawn;
        }

        public static Pawn RemoveRequiredHediffs(Pawn pawn, PawnConvertingDef recipe)
        {
            // Remove required hediffs if needed.
            if (!recipe.removeRequiredHediffs)
            {
                return pawn;
            }

            var enumerable = from def in recipe.requiredHediffs
                where pawn.health.hediffSet.HasHediff(def)
                select def;
            foreach (var current in enumerable)
            {
                pawn.health.hediffSet.hediffs.Remove(pawn.health.hediffSet.GetFirstHediffOfDef(current));
            }

            return pawn;
        }

        public static Pawn ApplyForcedHediff(Pawn pawn, PawnConvertingDef recipe)
        {
            // Apply Forced Hediff if needed.
            if (recipe.forcedHediff == null)
            {
                return pawn;
            }

            if (!pawn.health.hediffSet.hediffs.Contains(recipe.forcedHediff))
            {
                pawn.health.hediffSet.AddDirect(recipe.forcedHediff);
            }

            Log.Message("Pawn already has forced Hediff, new hediff was not applied.");

            return pawn;
        }

        public static Gender GetOutputGender(Pawn pawn, PawnConvertingDef recipe)
        {
            Gender outputGender;
            if (recipe.outputSex != null)
            {
                var outputSex = recipe.outputSex;
                switch (outputSex)
                {
                    case "Male":
                        outputGender = Gender.Male;
                        break;
                    case "Female":
                        outputGender = Gender.Male;
                        break;
                    default:
                        Log.Message("Defined sex/gender does not exist in this context. Defaulting to original.");
                        outputGender = pawn.gender;
                        break;
                }
            }
            else
            {
                outputGender = pawn.gender;
            }

            return outputGender;
        }

        public static Pawn FixPawnRelations(Pawn newPawn, Pawn oldPawn)
        {
            var relations = oldPawn.relations.DirectRelations.ToList();
            foreach (var relation in relations)
            {
                relation.otherPawn.relations.AddDirectRelation(relation.def, newPawn);
                relation.otherPawn.relations.RemoveDirectRelation(relation.def, oldPawn);
            }

            return newPawn;
        }
    }
}