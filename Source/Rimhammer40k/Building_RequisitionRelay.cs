using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace Rimhammer40k
{
    [DefOf]
    public static class RCDefsOf
    {
        public static JobDef UseRequisitionRelay;

        //public static SoundDef FreeReinforcementsOneShotRH;

        //public static SoundDef MediumReinforcementsOneShotRH;

        //public static SoundDef StrongReinforcementsOneShotRH;

        //public static SoundDef AmbientRequisitionRelayRH;

        public static SoundDef PickUpRequisitionRelayRH;
    }

    public class Building_RequisitionRelay : Building
    {
        private CompPowerTrader powerComp;

        public bool CanUseCommsNow
        {
            get
            {
                if (Spawned && Map.gameConditionManager.ConditionIsActive(GameConditionDefOf.SolarFlare))
                {
                    return false;
                }

                return powerComp.PowerOn;
            }
        }

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            powerComp = GetComp<CompPowerTrader>();
            LessonAutoActivator.TeachOpportunity(ConceptDefOf.OpeningComms, OpportunityType.GoodToKnow);
        }

        private FloatMenuOption GetFailureReason(Pawn myPawn)
        {
            FloatMenuOption result;
            if (!myPawn.CanReach(this, PathEndMode.InteractionCell, Danger.Some))
            {
                result = new FloatMenuOption("CannotUseNoPath".Translate(), null);
            }
            else if (Spawned && Map.gameConditionManager.ConditionIsActive(GameConditionDefOf.SolarFlare))
            {
                result = new FloatMenuOption("CannotUseSolarFlare".Translate(), null);
            }
            else if (!powerComp.PowerOn)
            {
                result = new FloatMenuOption("CannotUseNoPower".Translate(), null);
            }
            else if (!myPawn.health.capacities.CapableOf(PawnCapacityDefOf.Talking))
            {
                result = new FloatMenuOption(
                    "CannotUseReason".Translate("IncapableOfCapacity".Translate(PawnCapacityDefOf.Talking.label)),
                    null);
            }
            else if (myPawn.skills.GetSkill(SkillDefOf.Social).TotallyDisabled)
            {
                result = new FloatMenuOption("CannotPrioritizeWorkTypeDisabled".Translate(SkillDefOf.Social.LabelCap),
                    null);
            }
            else if (!CanUseCommsNow)
            {
                Log.Error(myPawn + " could not use comm console for unknown reason.");
                result = new FloatMenuOption("Cannot use now", null);
            }
            else
            {
                result = null;
            }

            return result;
        }

        private IEnumerable<Faction> GetCommTargets()
        {
            var PreselectList = from f in Find.FactionManager.AllFactionsVisibleInViewOrder
                where !f.IsPlayer
                select f;
            var commTargetList = from f in PreselectList
                where f.PlayerRelationKind == FactionRelationKind.Neutral && f.PlayerGoodwill > 0 ||
                      f.PlayerRelationKind == FactionRelationKind.Ally && !f.IsPlayer
                select f;
            return commTargetList;
        }

        public override IEnumerable<FloatMenuOption> GetFloatMenuOptions(Pawn myPawn)
        {
            var failureReason = GetFailureReason(myPawn);
            if (failureReason != null)
            {
                yield return failureReason;
            }
            else
            {
                var factionsToCall = GetCommTargets();
                if (factionsToCall == null)
                {
                    yield return new FloatMenuOption("NoFriendlyFactionRH".Translate(), null);
                }
                else if (!factionsToCall.Any())
                {
                    yield return new FloatMenuOption("NoFriendlyFactionRH".Translate(), null);
                }

                if (factionsToCall == null)
                {
                    yield break;
                }

                foreach (var commTarget in factionsToCall)
                {
                    var option = CommFloatMenuOption(commTarget, myPawn);
                    if (option != null)
                    {
                        yield return option;
                    }
                }
            }
        }

        private FloatMenuOption CommFloatMenuOption(Faction commTarget, Pawn negotiator)
        {
            FloatMenuOption result;
            if (commTarget.IsPlayer)
            {
                result = null;
            }
            else
            {
                string text = "CallOnRadio".Translate(commTarget.GetCallLabel());
                var text2 = text;
                text = string.Concat(text2, " (", commTarget.PlayerRelationKind.GetLabel(), ", ",
                    commTarget.PlayerGoodwill.ToStringWithSign(), ")");
                if (!LeaderIsAvailableToTalk(commTarget))
                {
                    string str;
                    str = commTarget.leader != null
                        ? "LeaderUnavailable".Translate(commTarget.leader.LabelShort, commTarget.leader)
                        : "LeaderUnavailableNoLeader".Translate();
                    result = new FloatMenuOption(text + " (" + str + ")", null);
                }
                else
                {
                    result = FloatMenuUtility.DecoratePrioritizedTask(
                        new FloatMenuOption(text, delegate { GiveUseCommsJob(negotiator, commTarget); },
                            MenuOptionPriority.InitiateSocial), negotiator, this);
                }
            }

            return result;
        }

        private bool LeaderIsAvailableToTalk(Faction faction)
        {
            bool result;
            if (faction.leader == null)
            {
                result = false;
            }
            else
            {
                if (faction.leader.Spawned)
                {
                    if (faction.leader.Downed || faction.leader.IsPrisoner || !faction.leader.Awake() ||
                        faction.leader.InMentalState)
                    {
                        return false;
                    }
                }

                result = true;
            }

            return result;
        }

        private void GiveUseCommsJob(Pawn negotiator, Faction target)
        {
            var job = new Job(RCDefsOf.UseRequisitionRelay, this)
            {
                commTarget = target
            };
            negotiator.jobs.TryTakeOrderedJob(job);
            PlayerKnowledgeDatabase.KnowledgeDemonstrated(ConceptDefOf.OpeningComms, KnowledgeAmount.Total);
        }

        /** public IEnumerable<IntVec3> TradeableCells
        {
            get
            {
                return Building_OrbitalTradeBeacon.TradeableCellsAround(base.Position, base.Map);
            }
        }
        
        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (Gizmo g in base.GetGizmos())
            {
                yield return g;
            }
            if (DesignatorUtility.FindAllowedDesignator<Designator_ZoneAddStockpile_Resources>() != null)
            {
                yield return new Command_Action
                {
                    action = new Action(this.MakeMatchingStockpile),
                    hotKey = KeyBindingDefOf.Misc1,
                    defaultDesc = "CommandMakeBeaconStockpileDesc".Translate(),
                    icon = ContentFinder<Texture2D>.Get("UI/Designators/ZoneCreate_Stockpile", true),
                    defaultLabel = "CommandMakeBeaconStockpileLabel".Translate()
                };
            }
            yield break;
        }
        
        private void MakeMatchingStockpile()
        {
            Designator des = DesignatorUtility.FindAllowedDesignator<Designator_ZoneAddStockpile_Resources>();
            des.DesignateMultiCell(from c in this.TradeableCells
                                   where des.CanDesignateCell(c).Accepted
                                   select c);
        }
        
        public static List<IntVec3> TradeableCellsAround(IntVec3 pos, Map map)
        {
            Building_RequisitionRelay.tradeableCells.Clear();
            if (!pos.InBounds(map))
            {
                return Building_RequisitionRelay.tradeableCells;
            }
            Region region = pos.GetRegion(map, RegionType.Set_Passable);
            if (region == null)
            {
                return Building_RequisitionRelay.tradeableCells;
            }
            RegionTraverser.BreadthFirstTraverse(region, (Region from, Region r) => r.door == null, delegate (Region r)
            {
                foreach (IntVec3 item in r.Cells)
                {
                    if (item.InHorDistOf(pos, 7.9f))
                    {
                        Building_RequisitionRelay.tradeableCells.Add(item);
                    }
                }
                return false;
            }, 13, RegionType.Set_Passable);
            return Building_RequisitionRelay.tradeableCells;
        }
        
        public static IEnumerable<Building_RequisitionRelay> AllPowered(Map map)
        {
            foreach (Building_RequisitionRelay b in map.listerBuildings.AllBuildingsColonistOfClass<Building_RequisitionRelay>())
            {
                CompPowerTrader power = b.GetComp<CompPowerTrader>();
                if (power == null || power.PowerOn)
                {
                    yield return b;
                }
            }
            yield break;
        }
        
        private const float TradeRadius = 7.9f;
        
        private static List<IntVec3> tradeableCells = new List<IntVec3>(); **/
    }


    public class JobDriver_UseRequisitionRelay : JobDriver
    {
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            var toilPawn = pawn;
            var targetA = job.targetA;
            var toilJob = job;
            return toilPawn.Reserve(targetA, toilJob, 1, -1, null, errorOnFailed);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedOrNull(TargetIndex.A);
            yield return Toils_Goto.GotoCell(TargetIndex.A, PathEndMode.InteractionCell).FailOn(delegate(Toil to)
            {
                var building_CommsConsole =
                    (Building_RequisitionRelay) to.actor.jobs.curJob.GetTarget(TargetIndex.A).Thing;
                return !building_CommsConsole.CanUseCommsNow;
            });
            var openComms = new Toil();
            openComms.initAction = delegate
            {
                var actor = openComms.actor;
                var building_CommsConsole =
                    (Building_RequisitionRelay) actor.jobs.curJob.GetTarget(TargetIndex.A).Thing;
                if (building_CommsConsole.CanUseCommsNow)
                {
                    OpenCommsWith(actor, actor.jobs.curJob.commTarget as Faction);
                }
            };
            yield return openComms;
        }

        private void OpenCommsWith(Pawn negotiator, Faction faction)
        {
            var dialog_Negotiation = new Dialog_Negotiation(negotiator, faction,
                ReinforcementDialogMakerRC.FactionDialogFor(negotiator, faction), true);
            //dialog_Negotiation.soundAmbient = RCDefsOf.AmbientRequisitionRelayRH;
            RCDefsOf.PickUpRequisitionRelayRH.PlayOneShot(SoundInfo.OnCamera());
            Find.WindowStack.Add(dialog_Negotiation);
        }
    }

    public static class ReinforcementDialogMakerRC
    {
        public static DiaNode FactionDialogFor(Pawn negotiator, Faction faction)
        {
            var map = negotiator.Map;
            Pawn p;
            string text;
            if (faction.leader != null)
            {
                p = faction.leader;
                text = faction.leader.Name.ToStringFull;
            }
            else
            {
                Log.Error("Faction " + faction + " has no leader.");
                p = negotiator;
                text = faction.Name;
            }

            DiaNode diaNode;
            if (faction.PlayerRelationKind == FactionRelationKind.Hostile)
            {
                string key;
                if (!faction.def.permanentEnemy && "FactionGreetingHostileAppreciative".CanTranslate())
                {
                    key = "FactionGreetingHostileAppreciative";
                }
                else
                {
                    key = "FactionGreetingHostile";
                }

                diaNode = new DiaNode(key.Translate(text).AdjustedFor(p));
            }
            else if (faction.PlayerRelationKind == FactionRelationKind.Neutral)
            {
                diaNode = new DiaNode("FactionGreetingWary"
                    .Translate(text, negotiator.LabelShort, negotiator.Named("NEGOTIATOR"), p.Named("LEADER"))
                    .AdjustedFor(p));
            }
            else
            {
                diaNode = new DiaNode("FactionGreetingWarm"
                    .Translate(text, negotiator.LabelShort, negotiator.Named("NEGOTIATOR"), p.Named("LEADER"))
                    .AdjustedFor(p));
            }

            if (map != null && map.IsPlayerHome)
            {
                diaNode.options.Add(RequestFreeReinforcements(map, faction, negotiator));
                //If the faction is of a tech level that is industrial or higher...
                if (!(faction.def.techLevel < TechLevel.Spacer))
                {
                    diaNode.options.Add(RequestQuickReactionReinforcements(map, faction, negotiator));
                    diaNode.options.Add(RequestStrongQuickReactionReinforcements(map, faction, negotiator));
                }
            }

            var diaOption = new DiaOption("(" + "Disconnect".Translate() + ")")
            {
                resolveTree = true
            };
            diaNode.options.Add(diaOption);
            return diaNode;
        }

        private static DiaOption RequestFreeReinforcements(Map map, Faction faction, Pawn negotiator)
        {
            string text = "RequestFreeMilitaryAidRH".Translate();
            DiaOption result;
            if (faction.PlayerRelationKind != FactionRelationKind.Ally)
            {
                var diaOption = new DiaOption(text);
                diaOption.Disable("MustBeAlly".Translate());
                result = diaOption;
            }
            else if (!faction.def.allowedArrivalTemperatureRange.ExpandedBy(-4f)
                .Includes(map.mapTemperature.SeasonalTemp))
            {
                var diaOption2 = new DiaOption(text);
                diaOption2.Disable("BadTemperature".Translate());
                result = diaOption2;
            }
            else
            {
                var num = faction.lastMilitaryAidRequestTick + 30000 - Find.TickManager.TicksGame;
                if (num > 0)
                {
                    var diaOption3 = new DiaOption(text);
                    diaOption3.Disable("WaitTime".Translate(num.ToStringTicksToPeriod()));
                    result = diaOption3;
                }
                else if (NeutralGroupIncidentUtility.AnyBlockingHostileLord(map, faction))
                {
                    var diaOption4 = new DiaOption(text);
                    diaOption4.Disable("HostileVisitorsPresent".Translate());
                    result = diaOption4;
                }
                else
                {
                    var diaOption5 = new DiaOption(text);
                    var source = (from x in map.attackTargetsCache.TargetsHostileToColony
                        where GenHostility.IsActiveThreatToPlayer(x)
                        select ((Thing) x).Faction
                        into x
                        where x != null && !x.HostileTo(faction)
                        select x).Distinct();
                    if (source.Any())
                    {
                        var diaNode = new DiaNode("MilitaryAidConfirmMutualEnemy".Translate(faction.Name,
                            (from fa in source
                                select fa.Name).ToCommaList(true)));
                        var diaOption6 = new DiaOption("CallConfirm".Translate())
                        {
                            action = delegate { CallForSmallAid(map, faction); },
                            resolveTree = true
                        };
                        var diaOption7 = new DiaOption("CallCancel".Translate())
                        {
                            linkLateBind = ResetToRoot(faction, negotiator)
                        };
                        diaNode.options.Add(diaOption6);
                        diaNode.options.Add(diaOption7);
                        diaOption5.link = diaNode;
                    }
                    else
                    {
                        diaOption5.action = delegate { CallForSmallAid(map, faction); };
                        diaOption5.resolveTree = true;
                    }

                    result = diaOption5;
                }
            }

            return result;
        }

        private static DiaOption RequestQuickReactionReinforcements(Map map, Faction faction, Pawn negotiator)
        {
            var silverToPay = 500;
            var goodWillNeeded = 25;
            string text = "RequestPaidMilitaryAidRH".Translate(silverToPay);
            DiaOption result;
            if (faction.PlayerRelationKind != FactionRelationKind.Ally && faction.PlayerGoodwill < goodWillNeeded)
            {
                var diaOption = new DiaOption(text);
                diaOption.Disable("NeedGoodwill".Translate(goodWillNeeded.ToString("F0")));
                result = diaOption;
            }
            else if (AmountSendableSilver(map) < silverToPay)
            {
                var diaOption3 = new DiaOption(text);
                diaOption3.Disable("NeedSilverLaunchable".Translate(silverToPay.ToString()));
                result = diaOption3;
            }
            else if (!faction.def.allowedArrivalTemperatureRange.ExpandedBy(-4f)
                .Includes(map.mapTemperature.SeasonalTemp))
            {
                var diaOption2 = new DiaOption(text);
                diaOption2.Disable("BadTemperature".Translate());
                result = diaOption2;
            }
            else
            {
                var num = faction.lastMilitaryAidRequestTick + 60000 - Find.TickManager.TicksGame;
                if (num > 0)
                {
                    var diaOption3 = new DiaOption(text);
                    diaOption3.Disable("WaitTime".Translate(num.ToStringTicksToPeriod()));
                    result = diaOption3;
                }
                else if (NeutralGroupIncidentUtility.AnyBlockingHostileLord(map, faction))
                {
                    var diaOption4 = new DiaOption(text);
                    diaOption4.Disable("HostileVisitorsPresent".Translate());
                    result = diaOption4;
                }
                else
                {
                    var diaOption5 = new DiaOption(text);
                    var source = (from x in map.attackTargetsCache.TargetsHostileToColony
                        where GenHostility.IsActiveThreatToPlayer(x)
                        select ((Thing) x).Faction
                        into x
                        where x != null && !x.HostileTo(faction)
                        select x).Distinct();
                    if (source.Any())
                    {
                        var diaNode = new DiaNode("MilitaryAidConfirmMutualEnemy".Translate(faction.Name,
                            (from fa in source
                                select fa.Name).ToCommaList(true)));
                        var diaOption6 = new DiaOption("CallConfirm".Translate())
                        {
                            action = delegate { CallForMediumAid(map, faction); },
                            resolveTree = true
                        };
                        var diaOption7 = new DiaOption("CallCancel".Translate())
                        {
                            linkLateBind = ResetToRoot(faction, negotiator)
                        };
                        diaNode.options.Add(diaOption6);
                        diaNode.options.Add(diaOption7);
                        diaOption5.link = diaNode;
                    }
                    else
                    {
                        diaOption5.action = delegate
                        {
                            TradeUtility.LaunchThingsOfType(ThingDefOf.Silver, silverToPay, map, null);
                            CallForMediumAid(map, faction);
                        };
                        diaOption5.resolveTree = true;
                    }

                    result = diaOption5;
                }
            }

            return result;
        }

        private static DiaOption RequestStrongQuickReactionReinforcements(Map map, Faction faction, Pawn negotiator)
        {
            var silverToPay = 900;
            var goodWillNeeded = 25;
            string text = "RequestHeavyPaidMilitaryAidRH".Translate(silverToPay);
            DiaOption result;
            if (faction.PlayerRelationKind != FactionRelationKind.Ally && faction.PlayerGoodwill < goodWillNeeded)
            {
                var diaOption = new DiaOption(text);
                diaOption.Disable("NeedGoodwill".Translate(goodWillNeeded.ToString("F0")));
                result = diaOption;
            }
            else if (AmountSendableSilver(map) < silverToPay)
            {
                var diaOption3 = new DiaOption(text);
                diaOption3.Disable("NeedSilverLaunchable".Translate(silverToPay.ToString()));
                result = diaOption3;
            }
            else if (!faction.def.allowedArrivalTemperatureRange.ExpandedBy(-4f)
                .Includes(map.mapTemperature.SeasonalTemp))
            {
                var diaOption2 = new DiaOption(text);
                diaOption2.Disable("BadTemperature".Translate());
                result = diaOption2;
            }
            else
            {
                var num = faction.lastMilitaryAidRequestTick + 90000 - Find.TickManager.TicksGame;
                if (num > 0)
                {
                    var diaOption3 = new DiaOption(text);
                    diaOption3.Disable("WaitTime".Translate(num.ToStringTicksToPeriod()));
                    result = diaOption3;
                }
                else if (NeutralGroupIncidentUtility.AnyBlockingHostileLord(map, faction))
                {
                    var diaOption4 = new DiaOption(text);
                    diaOption4.Disable("HostileVisitorsPresent".Translate());
                    result = diaOption4;
                }
                else
                {
                    var diaOption5 = new DiaOption(text);
                    var source = (from x in map.attackTargetsCache.TargetsHostileToColony
                        where GenHostility.IsActiveThreatToPlayer(x)
                        select ((Thing) x).Faction
                        into x
                        where x != null && !x.HostileTo(faction)
                        select x).Distinct();
                    if (source.Any())
                    {
                        var diaNode = new DiaNode("MilitaryAidConfirmMutualEnemy".Translate(faction.Name,
                            (from fa in source
                                select fa.Name).ToCommaList(true)));
                        var diaOption6 = new DiaOption("CallConfirm".Translate())
                        {
                            action = delegate { CallForStrongAid(map, faction); },
                            resolveTree = true
                        };
                        var diaOption7 = new DiaOption("CallCancel".Translate())
                        {
                            linkLateBind = ResetToRoot(faction, negotiator)
                        };
                        diaNode.options.Add(diaOption6);
                        diaNode.options.Add(diaOption7);
                        diaOption5.link = diaNode;
                    }
                    else
                    {
                        diaOption5.action = delegate
                        {
                            TradeUtility.LaunchThingsOfType(ThingDefOf.Silver, silverToPay, map, null);
                            CallForStrongAid(map, faction);
                        };
                        diaOption5.resolveTree = true;
                    }

                    result = diaOption5;
                }
            }

            return result;
        }

        private static int AmountSendableSilver(Map map)
        {
            return (from t in TradeUtility.AllLaunchableThingsForTrade(map)
                where t.def == ThingDefOf.Silver
                select t).Sum(t => t.stackCount);
        }

        private static void CallForSmallAid(Map map, Faction faction)
        {
            var incidentParms = new IncidentParms
            {
                target = map,
                faction = faction,
                points = Rand.RangeInclusive(400, 800)
            };
            faction.lastMilitaryAidRequestTick = Find.TickManager.TicksGame;
            IncidentDefOf.RaidFriendly.Worker.TryExecute(incidentParms);
            //SoundStarter.PlayOneShotOnCamera(RCDefsOf.FreeReinforcementsOneShotRH);
        }

        private static void CallForMediumAid(Map map, Faction faction)
        {
            var incidentParms = new IncidentParms
            {
                target = map,
                faction = faction,
                points = Rand.RangeInclusive(800, 3500)
            };
            //incidentParms.raidArrivalMode = PawnsArrivalModeDefOf.CenterDrop;
            faction.lastMilitaryAidRequestTick = Find.TickManager.TicksGame;
            IncidentDefOf.RaidFriendly.Worker.TryExecute(incidentParms);
            //SoundStarter.PlayOneShotOnCamera(RCDefsOf.MediumReinforcementsOneShotRH);
        }

        private static void CallForStrongAid(Map map, Faction faction)
        {
            var incidentParms = new IncidentParms
            {
                target = map,
                faction = faction,
                points = Rand.RangeInclusive(3500, 5000)
            };
            //incidentParms.raidArrivalMode = PawnsArrivalModeDefOf.CenterDrop;
            faction.lastMilitaryAidRequestTick = Find.TickManager.TicksGame;
            IncidentDefOf.RaidFriendly.Worker.TryExecute(incidentParms);
            //SoundStarter.PlayOneShotOnCamera(RCDefsOf.StrongReinforcementsOneShotRH);
        }

        private static Func<DiaNode> ResetToRoot(Faction faction, Pawn negotiator)
        {
            return () => FactionDialogFor(negotiator, faction);
        }
    }
}