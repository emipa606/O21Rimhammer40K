using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using O21Toolbox.AutomatedProducer;
using O21Toolbox.Utility;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace O21Toolbox.PawnCrafter
{
    /// <summary>
    ///     Base class for all printers and crafters.
    /// </summary>
    public class Building_PawnCrafter : Building, IThingHolder, IStoreSettingsParent, IPawnCrafter
    {
        /// <summary>
        ///     Printer state.
        /// </summary>
        public CrafterStatus crafterStatus;

        //Variables, Construction
        /// <summary>
        ///     Ticks left until pawn is finished printing.
        /// </summary>
        public int craftingTicksLeft;

        /// <summary>
        ///     Set by custom implementations.
        /// </summary>
        public int craftingTime;

        /// <summary>
        ///     Flickable component.
        /// </summary>
        protected CompFlickable flickableComp;

        //Variables
        /// <summary>
        ///     Stored ingredients for use in producing one pawn.
        /// </summary>
        public ThingOwner<Thing> ingredients = new ThingOwner<Thing>();

        /// <summary>
        ///     Storage settings for what nutrition sources to use.
        /// </summary>
        public StorageSettings inputSettings;

        //Repeat crafting stuff.
        public PawnCraftingDef lastDef;

        /// <summary>
        ///     Next resource drain trick-
        /// </summary>
        public int nextResourceTick;

        /// <summary>
        ///     Convenience class for setting what resources is needed to make one pawn.
        /// </summary>
        public ThingOrderProcessor orderProcessor;

        /// <summary>
        ///     Pawn to print.
        /// </summary>
        public Pawn pawnBeingCrafted;

        //Convenience variables
        /// <summary>
        ///     Power component.
        /// </summary>
        protected CompPowerTrader powerComp;

        /// <summary>
        ///     XML properties for the printer.
        /// </summary>
        /// </summary>
        protected PawnCrafterProperties printerProperties;

        public bool repeatLastPawn;

        /// <summary>
        ///     Sustained sound.
        /// </summary>
        private Sustainer soundSustainer;

        /// <summary>
        ///     How finished the crafting is in percentage based time. 0.0f to 1.0f
        /// </summary>
        public float CraftingFinishedPercentage
        {
            get
            {
                if (printerProperties.customCraftingTime)
                {
                    return ((float) craftingTime - craftingTicksLeft) / craftingTime;
                }

                return ((float) printerProperties.ticksToCraft - craftingTicksLeft) /
                       (float) printerProperties.ticksToCraft;
            }
        }

        /// <summary>
        ///     How many ticks it take to craft a pawn.
        /// </summary>
        public int CraftingTicks
        {
            get
            {
                if (printerProperties.customCraftingTime)
                {
                    return craftingTime;
                }

                return printerProperties.ticksToCraft;
            }
        }

        /// <summary>
        ///     Sets the Storage tab to be visible.
        /// </summary>
        public bool StorageTabVisible => true;

        public StorageSettings GetStoreSettings()
        {
            return inputSettings;
        }

        public StorageSettings GetParentStoreSettings()
        {
            return def.building.fixedStorageSettings;
        }

        public void GetChildHolders(List<IThingHolder> outChildren)
        {
            //None
        }

        public ThingOwner GetDirectlyHeldThings()
        {
            return ingredients;
        }

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);

            powerComp = GetComp<CompPowerTrader>();
            flickableComp = GetComp<CompFlickable>();

            if (inputSettings == null)
            {
                inputSettings = new StorageSettings(this);
                if (def.building.defaultStorageSettings != null)
                {
                    inputSettings.CopyFrom(def.building.defaultStorageSettings);
                }
            }

            printerProperties = def.GetModExtension<PawnCrafterProperties>();

            //Setup 'orderProcessor'
            if (!printerProperties.customOrderProcessor)
            {
                orderProcessor = new ThingOrderProcessor(ingredients, inputSettings);
                if (printerProperties != null)
                {
                    orderProcessor.requestedItems.AddRange(printerProperties.costList);
                }
            }

            AdjustPowerNeed();

            if (!respawningAfterLoad)
            {
                orderProcessor = new ThingOrderProcessor(ingredients, inputSettings);
            }
        }

        public override void PostMake()
        {
            base.PostMake();

            inputSettings = new StorageSettings(this);
            if (def.building.defaultStorageSettings != null)
            {
                inputSettings.CopyFrom(def.building.defaultStorageSettings);
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();

            //Save \ Load
            Scribe_Deep.Look(ref ingredients, "ingredients");
            Scribe_Values.Look(ref crafterStatus, "crafterStatus");
            Scribe_Values.Look(ref craftingTicksLeft, "craftingTicksLeft");
            Scribe_Values.Look(ref nextResourceTick, "nextResourceTick");
            Scribe_Deep.Look(ref pawnBeingCrafted, "pawnBeingCrafted");
            Scribe_Deep.Look(ref inputSettings, "inputSettings");
            Scribe_Values.Look(ref craftingTime, "craftingTime");

            Scribe_Deep.Look(ref orderProcessor, "orderProcessor", ingredients, inputSettings);
            Scribe_Defs.Look(ref lastDef, "lastDef");
            Scribe_Values.Look(ref repeatLastPawn, "repeatLastPawn");
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

        public override IEnumerable<Gizmo> GetGizmos()
        {
            var gizmos = new List<Gizmo>(base.GetGizmos());

            if (pawnBeingCrafted != null)
            {
                gizmos.Insert(0, new Gizmo_PrinterPawnInfo(this));
            }

            if (crafterStatus != CrafterStatus.Finished)
            {
                gizmos.Insert(0, new Gizmo_TogglePrinting(this));
            }

            if (DebugSettings.godMode && pawnBeingCrafted != null)
            {
                gizmos.Insert(0, new Command_Action
                {
                    defaultLabel = "DEBUG: Finish crafting.",
                    defaultDesc = "Finishes crafting the pawn.",
                    action = delegate { crafterStatus = CrafterStatus.Finished; }
                });
            }

            gizmos.Insert(0, new Command_Toggle
            {
                defaultLabel = "PawnCrafterRepeatLabel".Translate(),
                defaultDesc = "PawnCrafterRepeatDescription".Translate(),
                icon = ContentFinder<Texture2D>.Get("ui/designators/PlanOn"),
                isActive = () => repeatLastPawn,
                toggleAction = delegate { repeatLastPawn = !repeatLastPawn; }
            });

            return gizmos;
        }

        /// <summary>
        ///     Is the crafter ready to craft?
        /// </summary>
        /// <returns>True if it is.</returns>
        public virtual bool ReadyToCraft()
        {
            var pendingRequests = orderProcessor.PendingRequests();
            var readyToCraft = pendingRequests == null;
            if (pendingRequests != null && pendingRequests.Count() == 0)
            {
                readyToCraft = true;
            }

            return crafterStatus == CrafterStatus.Filling && readyToCraft;
        }

        /// <summary>
        ///     Initiates the crafting of a Pawn. Usually by first opening a interface to customize the Pawn. Should set
        ///     'crafterStatus' to 'CrafterStatus.Filling' when got 'pawnBeingCrafted' set.
        /// </summary>
        public virtual void InitiatePawnCrafting()
        {
            if (crafterStatus != CrafterStatus.Idle)
            {
                //Default behavior
                pawnBeingCrafted = PawnGenerator.GeneratePawn(printerProperties.pawnKind, Faction);

                crafterStatus = CrafterStatus.Filling;
            }

            if (crafterStatus == CrafterStatus.Idle)
            {
                //Bring up Float Menu
                //FloatMenuUtility.
                var floatMenuOptions = new List<FloatMenuOption>();
                foreach (PawnCraftingDef def in DefDatabase<PawnCraftingDef>.AllDefs.OrderBy(def => def.orderID))
                {
                    if (def.recipeUser == null && printerProperties.listOnlyAllowed != true ||
                        def.recipeUser == this.def.defName)
                    {
                        var disabled = false;
                        var labelText = "";
                        if (def.requiredResearch != null && !def.requiredResearch.IsFinished)
                        {
                            disabled = true;
                        }

                        if (disabled)
                        {
                            labelText = "PawnCrafterNeedsResearch".Translate(def.label, def.requiredResearch.LabelCap);
                        }
                        else
                        {
                            labelText = "PawnCrafterMake".Translate(def.label);
                        }

                        var option = new FloatMenuOption(labelText,
                            delegate
                            {
                                if (!disabled)
                                {
                                    lastDef = def;
                                    MakePawnAndInitCrafting(def);
                                }
                            }
                        );

                        option.Disabled = disabled;
                        floatMenuOptions.Add(option);
                    }
                }

                if (floatMenuOptions.Count > 0)
                {
                    var floatMenu = new FloatMenu(floatMenuOptions);
                    Find.WindowStack.Add(floatMenu);
                }
            }
        }

        /// <summary>
        ///     Prepares the crafter for crafting and starts the process.
        /// </summary>
        public virtual void StartPrinting()
        {
            //Setup printing procedure
            craftingTicksLeft = CraftingTicks;
            nextResourceTick = printerProperties.resourceTick;
            crafterStatus = CrafterStatus.Crafting;
        }

        /// <summary>
        ///     Aborts the crafter and refunds any inputted resources.
        /// </summary>
        public virtual void StopPawnCrafting()
        {
            //Reset printer status.
            crafterStatus = CrafterStatus.Idle;

            if (pawnBeingCrafted != null)
            {
                pawnBeingCrafted.Destroy();
            }

            pawnBeingCrafted = null;

            //Eject unused materials.
            ingredients.TryDropAll(InteractionCell, Map, ThingPlaceMode.Near);
        }

        /// <summary>
        ///     Extra actions to do whenever in the Filling and Printing states.
        /// </summary>
        public virtual void ExtraCrafterTickAction()
        {
            /**switch (crafterStatus)
            {
                case CrafterStatus.Filling:
                    //Emit smoke
                    if (powerComp.PowerOn && Current.Game.tickManager.TicksGame % 300 == 0)
                    {
                        MoteMaker.ThrowSmoke(Position.ToVector3(), Map, 1f);
                    }
                break;

                case CrafterStatus.Crafting:
                    //Emit smoke
                    if (powerComp.PowerOn && Current.Game.tickManager.TicksGame % 100 == 0)
                    {
                        MoteMaker.ThrowSmoke(Position.ToVector3(), Map, 1.33f);
                    }
                    break;
            }**/
            if (!powerComp.PowerOn && soundSustainer != null && !soundSustainer.Ended)
            {
                soundSustainer.End();
            }

            //Make construction effects
            switch (crafterStatus)
            {
                case CrafterStatus.Filling:
                    //Emit smoke
                    if (powerComp.PowerOn && Current.Game.tickManager.TicksGame % 300 == 0)
                    {
                        MoteMaker.ThrowSmoke(Position.ToVector3(), Map, 1f);
                    }

                    break;

                case CrafterStatus.Crafting:
                    //Emit smoke
                    if (powerComp.PowerOn && Current.Game.tickManager.TicksGame % 100 == 0)
                    {
                        for (var i = 0; i < 5; i++)
                        {
                            MoteMaker.ThrowMicroSparks(
                                Position.ToVector3() + new Vector3(Rand.Range(-1, 1), 0f, Rand.Range(-1, 1)), Map);
                        }

                        for (var i = 0; i < 3; i++)
                        {
                            MoteMaker.ThrowSmoke(
                                Position.ToVector3() + new Vector3(Rand.Range(-1f, 1f), 0f, Rand.Range(-1f, 1f)), Map,
                                Rand.Range(0.5f, 0.75f));
                        }

                        MoteMaker.ThrowHeatGlow(Position, Map, 1f);

                        if (soundSustainer == null || soundSustainer.Ended)
                        {
                            SoundDef soundDef = printerProperties.craftingSound;
                            if (soundDef != null && soundDef.sustain)
                            {
                                var info = SoundInfo.InMap(this, MaintenanceType.PerTick);
                                soundSustainer = soundDef.TrySpawnSustainer(info);
                            }
                        }
                    }

                    if (soundSustainer != null && !soundSustainer.Ended)
                    {
                        soundSustainer.Maintain();
                    }

                    break;

                default:
                {
                    if (soundSustainer != null && !soundSustainer.Ended)
                    {
                        soundSustainer.End();
                    }
                }
                    break;
            }
        }

        public virtual void FinishAction()
        {
            //Add effects
            orderProcessor.requestedItems.Clear();

            if (repeatLastPawn && lastDef != null)
            {
                MakePawnAndInitCrafting(lastDef);
            }
        }

        public override string GetInspectString()
        {
            if (ParentHolder != null && !(ParentHolder is Map))
            {
                return base.GetInspectString();
            }

            var builder = new StringBuilder(base.GetInspectString());

            builder.AppendLine();
            builder.AppendLine(
                printerProperties.crafterStatusText.Translate(
                    (printerProperties.crafterStatusEnumText + (int) crafterStatus).Translate()));

            if (crafterStatus == CrafterStatus.Crafting)
            {
                builder.AppendLine(
                    printerProperties.crafterProgressText.Translate(CraftingFinishedPercentage.ToStringPercent()));
            }

            if (crafterStatus == CrafterStatus.Filling)
            {
                var needsFulfilled = true;

                foreach (var thingOrderRequest in orderProcessor.requestedItems)
                {
                    var itemCount = ingredients.TotalStackCountOfDef(thingOrderRequest.thingDef);
                    if (itemCount < thingOrderRequest.amount)
                    {
                        builder.Append(printerProperties.crafterMaterialNeedText.Translate(
                            thingOrderRequest.amount - itemCount, thingOrderRequest.thingDef.LabelCap) + " ");
                        needsFulfilled = false;
                    }
                }

                if (!needsFulfilled)
                {
                    builder.AppendLine();
                }
            }

            if (ingredients.Count > 0)
            {
                builder.Append(printerProperties.crafterMaterialsText.Translate() + " ");
            }

            foreach (var item in ingredients)
            {
                builder.Append(item.LabelCap + "; ");
            }

            return builder.ToString().TrimEndNewlines();
        }

        public override void Tick()
        {
            base.Tick();

            AdjustPowerNeed();

            if (flickableComp == null || flickableComp != null && flickableComp.SwitchIsOn)
            {
                //State machine
                switch (crafterStatus)
                {
                    case CrafterStatus.Filling:
                    {
                        ExtraCrafterTickAction();

                        //If we aren't being filled, then start.
                        var pendingRequests = orderProcessor.PendingRequests();
                        var startPrinting = pendingRequests == null;
                        if (pendingRequests != null && pendingRequests.Count() == 0)
                        {
                            startPrinting = true;
                        }

                        if (startPrinting)
                        {
                            //Initiate printing phase.
                            StartPrinting();
                        }
                    }
                        break;

                    case CrafterStatus.Crafting:
                    {
                        ExtraCrafterTickAction();

                        if (powerComp.PowerOn)
                        {
                            //Periodically use resources.
                            nextResourceTick--;

                            if (nextResourceTick <= 0)
                            {
                                nextResourceTick = printerProperties.resourceTick;

                                //Deduct resources from each category.
                                foreach (var thingOrderRequest in orderProcessor.requestedItems)
                                {
                                    //Item
                                    if (ingredients.Any(thing => thing.def == thingOrderRequest.thingDef))
                                    {
                                        //Grab first stack of Plasteel.
                                        var item = ingredients.First(thing => thing.def == thingOrderRequest.thingDef);

                                        if (item != null)
                                        {
                                            var resourceTickAmount = (int) Math.Ceiling(thingOrderRequest.amount /
                                                ((float) CraftingTicks / printerProperties.resourceTick));

                                            var amount = Math.Min(resourceTickAmount, item.stackCount);
                                            var takenItem = ingredients.Take(item, amount);

                                            takenItem.Destroy();
                                        }
                                    }
                                }
                            }

                            //Are we done yet?
                            if (craftingTicksLeft > 0)
                            {
                                craftingTicksLeft--;
                            }
                            else
                            {
                                crafterStatus = CrafterStatus.Finished;
                            }
                        }
                    }
                        break;

                    case CrafterStatus.Finished:
                    {
                        if (pawnBeingCrafted != null)
                        {
                            ExtraCrafterTickAction();

                            //Clear remaining materials.
                            ingredients.ClearAndDestroyContents();

                            //Spawn
                            GenSpawn.Spawn(pawnBeingCrafted, InteractionCell, Map);
                            if (printerProperties.hediffOnPawnCrafted != null)
                            {
                                pawnBeingCrafted.health.AddHediff(printerProperties.hediffOnPawnCrafted);
                            }

                            if (printerProperties.thoughtOnPawnCrafted != null)
                            {
                                pawnBeingCrafted.needs.mood.thoughts.memories.TryGainMemory(printerProperties
                                    .thoughtOnPawnCrafted);
                            }

                            //Make and send letter.
                            var letter = LetterMaker.MakeLetter(
                                printerProperties.pawnCraftedLetterLabel.Translate(pawnBeingCrafted.Name.ToStringShort),
                                printerProperties.pawnCraftedLetterText.Translate(pawnBeingCrafted.Name.ToStringFull),
                                LetterDefOf.PositiveEvent, pawnBeingCrafted);
                            Find.LetterStack.ReceiveLetter(letter);

                            //Reset
                            pawnBeingCrafted = null;
                            crafterStatus = CrafterStatus.Idle;

                            FinishAction();
                        }
                    }
                        break;
                }
            }
        }

        /// <summary>
        ///     Adjusts the required power depending on the state of the printer.
        /// </summary>
        public void AdjustPowerNeed()
        {
            if (flickableComp == null || flickableComp != null && flickableComp.SwitchIsOn)
            {
                if (crafterStatus == CrafterStatus.Crafting)
                {
                    powerComp.PowerOutput = -powerComp.Props.basePowerConsumption;
                }
                else
                {
                    powerComp.PowerOutput = -powerComp.Props.basePowerConsumption *
                                            printerProperties.powerConsumptionFactorIdle;
                }
            }
            else
            {
                powerComp.PowerOutput = 0f;
            }
        }

        /// <summary>
        ///     Counts all available nutrition in the printer.
        /// </summary>
        /// <returns>Total nutrition.</returns>
        public float CountNutrition()
        {
            var totalNutrition = 0f;

            //Count nutrition.
            foreach (var item in ingredients)
            {
                var corpse = item as Corpse;
                if (corpse != null)
                {
                    if (!corpse.IsDessicated())
                    {
                        totalNutrition +=
                            FoodUtility.GetBodyPartNutrition(corpse, corpse.InnerPawn.RaceProps.body.corePart);
                    }
                }
                else
                {
                    if (item.def.IsIngestible)
                    {
                        totalNutrition += (item.def?.ingestible.CachedNutrition ?? 0.05f) * item.stackCount;
                    }
                }
            }

            return totalNutrition;
        }

        /// <summary>
        ///     Gets the current Pawn being crafted.
        /// </summary>
        /// <returns>Pawn being crafted or null.</returns>
        public Pawn PawnBeingCrafted()
        {
            return pawnBeingCrafted;
        }

        /// <summary>
        ///     Gets the status of the crafter.
        /// </summary>
        /// <returns>Crafter status.</returns>
        public CrafterStatus PawnCrafterStatus()
        {
            return crafterStatus;
        }

        public void MakePawnAndInitCrafting(PawnCraftingDef def)
        {
            //Update costs.
            orderProcessor.requestedItems.Clear();

            foreach (ThingOrderRequest cost in def.costList)
            {
                var costCopy = new ThingOrderRequest();
                costCopy.thingDef = cost.thingDef;
                costCopy.amount = cost.amount;

                orderProcessor.requestedItems.Add(costCopy);
            }

            craftingTime = def.timeCost;

            //Apply template.
            if (def.useDroidCreator)
            {
                pawnBeingCrafted = DroidUtility.MakeDroidTemplate(def.pawnKind, Faction, Map.Tile);
            }
            else
            {
                //pawnBeingCrafted = PawnGenerator.GeneratePawn(def.pawnKind, Faction);
                var request = new PawnGenerationRequest(
                    def.pawnKind,
                    Faction.OfPlayer,
                    forceGenerateNewPawn: true,
                    canGeneratePawnRelations: false,
                    colonistRelationChanceFactor: 0f,
                    allowFood: false);
                pawnBeingCrafted = PawnGenerator.GeneratePawn(request);

                //No pregenerated equipment.
                pawnBeingCrafted?.equipment?.DestroyAllEquipment();
                pawnBeingCrafted?.apparel?.DestroyAll();
                pawnBeingCrafted?.inventory?.DestroyAll();
            }

            crafterStatus = CrafterStatus.Filling;
        }
    }

    /// <summary>
    ///     The state the printer currently is in.
    /// </summary>
    public enum CrafterStatus
    {
        /// <summary>
        ///     Does nothing in this mode.
        /// </summary>
        Idle = 0,

        /// <summary>
        ///     Requires filling in the mode.
        /// </summary>
        Filling,

        /// <summary>
        ///     Crafting in which it actively subtracts inputted resources.
        /// </summary>
        Crafting,

        /// <summary>
        ///     Finished state where it resets itself to Idle.
        /// </summary>
        Finished
    }
}