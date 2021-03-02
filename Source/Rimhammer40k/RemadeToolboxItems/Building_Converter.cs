using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace O21Toolbox.PawnConverter
{
    public class Building_Converter : Building_Casket, IThingHolder
    {
        /// <summary>
        ///     Recipe chosen for converting pawn.
        /// </summary>
        public PawnConvertingDef chosenRecipe;

        /// <summary>
        ///     Ticks left until pawn is finished converting.
        /// </summary>
        public int conversionTicksLeft;

        /// <summary>
        ///     Set by recipe.
        /// </summary>
        public int conversionTime;

        /// <summary>
        ///     Converter Component.
        /// </summary>
        protected ConverterProperties converterProperties;

        /// <summary>
        ///     Flickable component.
        /// </summary>
        protected CompFlickable flickableComp;

        /// <summary>
        ///     Current tick progress
        /// </summary>
        protected int ICookingTicking;

        /// <summary>
        ///     Time it takes to cook
        /// </summary>
        protected int ICookingTime;

        /// <summary>
        ///     Stored ingredients for using while converting.
        /// </summary>
        public ThingOwner<Thing> ingredients = new ThingOwner<Thing>();

        /// <summary>
        ///     Ticks left until next resource drain tick.
        /// </summary>
        public int nextResourceTick;

        /// <summary>
        ///     Power component.
        /// </summary>
        protected CompPowerTrader powerComp;

        /// <summary>
        ///     How finished the crafting is in percentage based time. 0.0f to 1.0f
        /// </summary>
        public float ConversionFinishedPercentage
        {
            get
            {
                if (converterProperties.customConversionTime)
                {
                    return ((float) conversionTime - conversionTicksLeft) / conversionTime;
                }

                return ((float) converterProperties.ticksToConvert - conversionTicksLeft) /
                       converterProperties.ticksToConvert;
            }
        }

        /// <summary>
        ///     How many ticks it take to craft a pawn.
        /// </summary>
        public int ConvertingTicks
        {
            get
            {
                if (converterProperties.customConversionTime)
                {
                    return conversionTime;
                }

                return converterProperties.ticksToConvert;
            }
        }

        /// <summary>
        ///     Sets the Storage tab to be visible.
        /// </summary>
        public bool StorageTabVisible => true;

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            powerComp = GetComp<CompPowerTrader>();
            flickableComp = GetComp<CompFlickable>();
            converterProperties = def.GetModExtension<ConverterProperties>();
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref conversionTicksLeft, "conversionTicksLeft");
            Scribe_Values.Look(ref nextResourceTick, "nextResourceTick");
            Scribe_Values.Look(ref conversionTime, "conversionTime");
        }

        public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
        {
            //Drop ingredients.
            if (mode != DestroyMode.Vanish)
            {
                ingredients.TryDropAll(PositionHeld, MapHeld, ThingPlaceMode.Near);
            }

            base.Destroy(mode);
        }

        public override IEnumerable<FloatMenuOption> GetFloatMenuOptions(Pawn myPawn)
        {
            if (converterProperties.requiresPower)
            {
                if (!powerComp.PowerOn)
                {
                    var item = new FloatMenuOption("CannotUseNoPower".Translate(), null);
                    return new List<FloatMenuOption>
                    {
                        item
                    };
                }
            }

            if (!myPawn.CanReserve(this))
            {
                var item2 = new FloatMenuOption("CannotUseReserved".Translate(), null);
                return new List<FloatMenuOption>
                {
                    item2
                };
            }

            if (!myPawn.CanReach(this, PathEndMode.OnCell, Danger.Some))
            {
                var item3 = new FloatMenuOption("CannotUseNoPath".Translate(), null);
                return new List<FloatMenuOption>
                {
                    item3
                };
            }

            if (HasAnyContents)
            {
                return null;
            }

            var floatMenuOptions = new List<FloatMenuOption>();
            foreach (var convertingDef in DefDatabase<PawnConvertingDef>.AllDefs.OrderBy(pawnConvertingDef =>
                pawnConvertingDef.orderID))
            {
                if (convertingDef.recipeUsers == null || !convertingDef.recipeUsers.Any(x => x == def.defName))
                {
                    continue;
                }

                var disabled = false;
                var reason = 0;
                string labelText;
                // Check research
                if (convertingDef.requiredResearch != null && !convertingDef.requiredResearch.IsFinished)
                {
                    disabled = true;
                    reason = 0;
                }

                // Check input Race
                if (!Util_PawnConvert.IsViableRace(myPawn, convertingDef))
                {
                    disabled = true;
                    reason = 1;
                }

                // Check input Sex
                if (!Util_PawnConvert.IsRequiredSex(myPawn, convertingDef))
                {
                    disabled = true;
                    reason = 2;
                }

                // Check input hediffs
                if (!HasRequiredHediffs(myPawn, convertingDef))
                {
                    disabled = true;
                    reason = 3;
                }

                // If disabled, say why.
                if (disabled)
                {
                    switch (reason)
                    {
                        case 0:
                            labelText = "PawnConverterNeedsResearch".Translate(convertingDef.label,
                                convertingDef.requiredResearch.LabelCap);
                            break;
                        case 1:
                            labelText = "PawnConverterInvalidRace".Translate(convertingDef.label);
                            break;
                        case 2:
                            labelText = "PawnConverterInvalidGender".Translate(convertingDef.label);
                            break;
                        case 3:
                            labelText = "PawnConverterInvalidHediffs".Translate(convertingDef.label);
                            break;
                        default:
                            labelText = "PawnConverterNeedsResearch".Translate(convertingDef.label,
                                convertingDef.requiredResearch.LabelCap);
                            break;
                    }
                }
                else
                {
                    labelText = "PawnConverterConvert".Translate(convertingDef.label);
                }

                var option = new FloatMenuOption(labelText,
                    delegate
                    {
                        if (disabled)
                        {
                            return;
                        }

                        var val2 = new Job(ConverterDefOf.EnterConverter, this);
                        myPawn.Reserve(this, val2);
                        myPawn.jobs.TryTakeOrderedJob(val2);
                        chosenRecipe = convertingDef;
                        ICookingTime = chosenRecipe.conversionTime;
                    }) {Disabled = disabled};

                floatMenuOptions.Add(option);
            }

            if (floatMenuOptions.Count > 0)
            {
                return floatMenuOptions;
            }

            //Old Shit
            /** FloatMenuOption item6 = new FloatMenuOption(Translator.Translate("EnterConverter"), (Action)delegate
                {
                    Job val2 = new Job(ConverterDefOf.EnterConverter, this);
                    ReservationUtility.Reserve(myPawn, this, val2);
                    myPawn.jobs.TryTakeOrderedJob(val2);
                }, MenuOptionPriority.Default, (Action)null, null, 0f, (Func<Rect, bool>)null, null);
                return new List<FloatMenuOption>
                {
                    item6
                }; **/

            return null;
        }

        private bool HasRequiredHediffs(Pawn pawn, PawnConvertingDef recipe)
        {
            if (recipe.requiredHediffs == null)
            {
                return true;
            }

            if (!recipe.requiredHediffs.All(x => pawn.health.hediffSet.HasHediff(x)))
            {
                return false;
            }

            if (!recipe.hediffSeverityMatters)
            {
                return true;
            }

            var enumerable = from def in recipe.requiredHediffs
                where pawn.health.hediffSet.HasHediff(def)
                select def;
            foreach (var current in enumerable)
            {
                if (pawn.health.hediffSet.GetFirstHediffOfDef(current) == null)
                {
                    continue;
                }

                if (pawn.health.hediffSet.GetFirstHediffOfDef(current).Severity <
                    recipe.requiredHediffSeverity)
                {
                    return false;
                }
            }

            return true;
        }

        private void CookIt()
        {
            ThingOwner convertedContainer = new ThingOwner<Thing>();
            foreach (var item in innerContainer)
            {
                if (item is Pawn val && chosenRecipe != null)
                {
                    convertedContainer.TryAdd(Util_PawnConvert.PawnConversion(val, chosenRecipe));
                }
            }

            innerContainer.ClearAndDestroyContents();
            innerContainer = convertedContainer;
            chosenRecipe = null;
        }

        public override void Tick()
        {
            if (converterProperties.requiresPower)
            {
                if (powerComp.PowerOn)
                {
                    if (HasAnyContents)
                    {
                        //Not the best way but this just ticks up until max is reached.
                        //It would be better to check the "endtime" with the Tickfinder
                        ICookingTicking++;
                        if (ICookingTicking >= ICookingTime)
                        {
                            CookIt();
                            EjectContents();
                            ICookingTicking = 0;
                        }
                    }
                }
                else
                {
                    ICookingTicking = 0;
                    if (HasAnyContents)
                    {
                        EjectContents();
                    }
                }
            }

            if (converterProperties.requiresPower)
            {
                return;
            }

            if (!HasAnyContents)
            {
                return;
            }

            //Not the best way but this just ticks up until max is reached.
            //It would be better to check the "endtime" with the Tickfinder
            ICookingTicking++;
            if (ICookingTicking < ICookingTime)
            {
                return;
            }

            CookIt();
            EjectContents();
            ICookingTicking = 0;
        }

        public override void EjectContents()
        {
            if (!Destroyed)
            {
                converterProperties.finishingSound.PlayOneShot(SoundInfo.OnCamera());
            }

            ICookingTicking = 0;
            innerContainer.TryDropAll(InteractionCell, Map, ThingPlaceMode.Near);
            contentsKnown = true;
        }

        public override void Draw()
        {
            base.Draw();
            if (converterProperties.timerBarEnabled)
            {
                DrawTimerBar();
            }
        }

        private void DrawTimerBar()
        {
            //Replaced Drawhelper with vanilla drawer here
            var fillableBarRequest = default(GenDraw.FillableBarRequest);
            fillableBarRequest.preRotationOffset = converterProperties.timerBarOffset;
            fillableBarRequest.size = converterProperties.timerBarSize;
            fillableBarRequest.fillPercent = ICookingTicking / (float) ICookingTime;
            fillableBarRequest.filledMat =
                SolidColorMaterials.SimpleSolidColorMaterial(converterProperties.timerBarFill);
            fillableBarRequest.unfilledMat =
                SolidColorMaterials.SimpleSolidColorMaterial(converterProperties.timerBarUnfill);
            var rotation = Rotation;
            rotation.Rotate(RotationDirection.Clockwise);
            fillableBarRequest.rotation = rotation;
            if (fillableBarRequest.rotation == Rot4.West)
            {
                fillableBarRequest.rotation = Rot4.West;
                fillableBarRequest.center = DrawPos + (Vector3.up * 0.1f) + (Vector3.back * 0.45f);
            }

            if (fillableBarRequest.rotation == Rot4.North)
            {
                fillableBarRequest.rotation = Rot4.North;
                fillableBarRequest.center = DrawPos + (Vector3.up * 0.1f) + (Vector3.left * 0.45f);
            }

            if (fillableBarRequest.rotation == Rot4.East)
            {
                fillableBarRequest.rotation = Rot4.East;
                fillableBarRequest.center = DrawPos + (Vector3.up * 0.1f) + (Vector3.forward * 0.45f);
            }

            if (fillableBarRequest.rotation == Rot4.South)
            {
                fillableBarRequest.rotation = Rot4.South;
                fillableBarRequest.center = DrawPos + (Vector3.up * 0.1f) + (Vector3.right * 0.45f);
            }

            GenDraw.DrawFillableBar(fillableBarRequest);
        }

        public override bool TryAcceptThing(Thing thing, bool allowSpecialEffects = true)
        {
            if (!base.TryAcceptThing(thing, allowSpecialEffects))
            {
                return false;
            }

            if (allowSpecialEffects)
            {
                SoundDef.Named("CryptosleepCasketAccept").PlayOneShot(new TargetInfo(Position, Map));
            }

            return true;
        }

        public static Building_Converter FindConverterFor(Pawn p, Pawn traveler, bool ignoreOtherReservations = false)
        {
            var enumerable = from def in DefDatabase<ThingDef>.AllDefs
                where typeof(Building_Converter).IsAssignableFrom(def.thingClass)
                select def;
            foreach (var current in enumerable)
            {
                var building_converter = (Building_Converter) GenClosest.ClosestThingReachable(p.Position, p.Map,
                    ThingRequest.ForDef(current), PathEndMode.InteractionCell, TraverseParms.For(traveler), 9999f,
                    delegate(Thing x)
                    {
                        bool arg_33_0;
                        if (!((Building_Converter) x).HasAnyContents)
                        {
                            var traveler2 = traveler;
                            LocalTargetInfo target = x;
                            var ignoreOtherReservations2 = ignoreOtherReservations;
                            arg_33_0 = traveler2.CanReserve(target, 1, -1, null, ignoreOtherReservations2);
                        }
                        else
                        {
                            arg_33_0 = false;
                        }

                        return arg_33_0;
                    });
                if (building_converter != null)
                {
                    return building_converter;
                }
            }

            return null;
        }

        [DefOf]
        public static class ConverterDefOf
        {
            /// <summary>
            ///     Job def telling pawns to enter the converter.
            /// </summary>
            public static JobDef EnterConverter;
        }
    }

    public class JobDriver_EnterConverter : JobDriver_EnterCryptosleepCasket
    {
        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedOrNull(TargetIndex.A);
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.InteractionCell);
            var prepare = Toils_General.Wait(500);
            prepare.FailOnCannotTouch(TargetIndex.A, PathEndMode.InteractionCell);
            prepare.WithProgressBarToilDelay(TargetIndex.A);
            yield return prepare;
            var enter = new Toil();
            enter.initAction = delegate
            {
                var actor = enter.actor;
                var pod = (Building_Converter) actor.CurJob.targetA.Thing;

                void action()
                {
                    actor.equipment.DropAllEquipment(actor.Position);
                    actor.apparel.DropAll(actor.Position);
                    actor.inventory.DropAllNearPawn(actor.Position);
                    actor.DeSpawn();
                    pod.TryAcceptThing(actor);
                }

                if (!pod.def.building.isPlayerEjectable)
                {
                    var freeColonistsSpawnedOrInPlayerEjectablePodsCount =
                        Map.mapPawns.FreeColonistsSpawnedOrInPlayerEjectablePodsCount;
                    if (freeColonistsSpawnedOrInPlayerEjectablePodsCount <= 1)
                    {
                        Find.WindowStack.Add(
                            Dialog_MessageBox.CreateConfirmation("CasketWarning".Translate().AdjustedFor(actor),
                                action));
                    }
                    else
                    {
                        action();
                    }
                }
                else
                {
                    action();
                }
            };
            enter.defaultCompleteMode = ToilCompleteMode.Instant;
            yield return enter;
        }
    }
}