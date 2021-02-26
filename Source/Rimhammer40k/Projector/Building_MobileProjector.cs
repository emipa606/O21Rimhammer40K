using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace Rimhammer40k.Projector
{
    /// <summary>
    ///     Building_MobileProjector class.
    /// </summary>
    /// <author>Rikiki</author>
    /// <permission>
    ///     Use this code as you want, just remember to add a link to the corresponding Ludeon forum mod release
    ///     thread.
    /// </permission>
    [StaticConstructorOnStartup]
    public abstract class Building_MobileProjector : Building
    {
        private const int targetSearchPeriodInTicks = 30;

        private const int idlePauseDurationInTicks = 3 * GenTicks.TicksPerRealSecond;

        // Projector range and rotation.
        private const float projectorMinRange = 5f;
        private const int projectorRangeRateInTicksIdle = 16; // Ticks necessary to change range by 1 when idle.

        private const int
            projectorRangeRateInTicksTargetting = 8; // Ticks necessary to change range by 1 when targeting.

        private const int projectorRotationRateInTicksIdle = 4; // Ticks between 1° rotation when idle.
        private const int projectorRotationRateInTicksTargetting = 1; // Ticks between 1° rotation when targeting.

        // Synchronization.
        private static int nextGroupId = 1;

        // Textures.
        protected static readonly Material projectorOnTexture =
            MaterialPool.MatFrom("Things/Building/Security/IG/Sabre/IG_sabre_searchlight",
                ShaderDatabase.CutoutComplex);

        protected static readonly Material projectorOffTexture =
            MaterialPool.MatFrom("Things/Building/Security/IG/Sabre/IG_sabre_searchlight",
                ShaderDatabase.CutoutComplex);

        protected static readonly Material projectorLightEffectTexture =
            MaterialPool.MatFrom("Things/Building/Security/IG/Sabre/IG_sabre_searchlight_LightEffect",
                ShaderDatabase.Transparent);

        protected static readonly Material targetLineTexture =
            MaterialPool.MatFrom(GenDraw.LineTexPath, ShaderDatabase.Transparent, new Color(1f, 1f, 1f));

        private int groupId;
        private bool groupIdJustChanged;
        private int idlePauseTicks = 1;
        private Thing light;

        // ===================== Variables =====================
        private LightMode lightMode = LightMode.Conic;
        private int nextTargetSearchTick;

        // Components references.
        protected CompPowerTrader powerComp;
        protected Matrix4x4 projectorLightEffectMatrix = default;
        protected Vector3 projectorLightEffectScale = new Vector3(12f, 1f, 12f);
        protected Matrix4x4 projectorMatrix;
        private float projectorRange = 15f;
        private float projectorRangeBaseOffset = 15f;
        private int projectorRangeRateInTicks = projectorRangeRateInTicksIdle;
        private float projectorRangeTarget = 15f;
        protected float projectorRotation;
        private float projectorRotationBaseOffset;
        private bool projectorRotationClockwise = true;
        private int projectorRotationRateInTicks = projectorRotationRateInTicksIdle;
        private float projectorRotationTarget;
        protected Vector3 projectorScale = new Vector3(4f, 1f, 4f);

        // Target and light references.
        protected Pawn target;

        // ===================== Setup Work =====================
        /// <summary>
        ///     Initialize instance variables.
        /// </summary>
        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);

            powerComp = GetComp<CompPowerTrader>();
            nextTargetSearchTick = Find.TickManager.TicksGame + Rand.Range(0, targetSearchPeriodInTicks);

            FindNextGroupId();

            if (respawningAfterLoad == false)
            {
                // Initial spawn. Align projector with turret rotation.
                projectorRotation = Rotation.AsAngle;
            }

            projectorMatrix.SetTRS(base.DrawPos + Altitudes.AltIncVect, projectorRotation.ToQuat(), projectorScale);
            powerComp.powerStartedAction = OnPoweredOn;
            powerComp.powerStoppedAction = OnPoweredOff;
        }

        /// <summary>
        ///     Remove the light when minifying.
        /// </summary>
        public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish)
        {
            SwitchOffLight();
            base.DeSpawn(mode);
        }

        /// <summary>
        ///     Remove the light and destroy the object.
        /// </summary>
        public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
        {
            SwitchOffLight();
            base.Destroy(mode);
        }

        /// <summary>
        ///     Save and load internal state variables (stored in savegame data).
        /// </summary>
        public override void ExposeData()
        {
            base.ExposeData();

            Scribe_Values.Look(ref nextTargetSearchTick, "nextTargetSearchTick");
            Scribe_References.Look(ref target, "target");
            Scribe_References.Look(ref light, "light");
            Scribe_Values.Look(ref projectorRangeRateInTicks, "projectorRangeRateInTicks");
            Scribe_Values.Look(ref projectorRotationRateInTicks, "projectorRotationRateInTicks");
            Scribe_Values.Look(ref projectorRotationBaseOffset, "projectorRotationBaseOffset");
            Scribe_Values.Look(ref projectorRotation, "projectorRotation");
            Scribe_Values.Look(ref projectorRotationTarget, "projectorRotationTarget");
            Scribe_Values.Look(ref projectorRotationClockwise, "projectorRotationClockwise");
            Scribe_Values.Look(ref projectorRangeBaseOffset, "projectorRangeBaseOffset");
            Scribe_Values.Look(ref projectorRange, "projectorRange");
            Scribe_Values.Look(ref projectorRangeTarget, "projectorRangeTarget");

            Scribe_Values.Look(ref lightMode, "lightMode");
            Scribe_Values.Look(ref idlePauseTicks, "idlePauseTicks");
            Scribe_Values.Look(ref groupId, "groupId");
        }

        // ===================== Main Work Function =====================
        /// <summary>
        ///     Main function:
        ///     - look for a target,
        ///     - light it if it exists,
        ///     - otherwise, idle turn.
        /// </summary>
        public override void Tick()
        {
            base.Tick();

            if (CheckAdditionalConditions() == false)
            {
                return;
            }

            // Check if tower is powered.
            if (powerComp.PowerOn == false)
            {
                return;
            }

            // Check locked target is still valid.
            if (target != null)
            {
                // Check target is still valid: not killed or downed and in sight.
                if (target.DestroyedOrNull()
                    || IsPawnValidTarget(target) == false)
                {
                    // Target is no more valid.
                    StopTargetting();
                    var newTarget = LookForNewTarget();
                    if (newTarget != null)
                    {
                        StartTargetting(newTarget);
                    }
                    else
                    {
                        // Only synchronize towers if no new target is found.
                        SynchronizeProjectorsInGroup();
                    }
                }
            }

            // Periodically look for a new target if idle or update its position.
            if (Find.TickManager.TicksGame >= nextTargetSearchTick)
            {
                nextTargetSearchTick = Find.TickManager.TicksGame + targetSearchPeriodInTicks;

                if (target == null)
                {
                    // No locked target: look for a new target.
                    var newTarget = LookForNewTarget();
                    if (newTarget != null)
                    {
                        StartTargetting(newTarget);
                    }
                }

                if (target != null)
                {
                    // Target locked: update projector rotation and range.
                    projectorRotationTarget = Mathf.Round((target.Position - Position).AngleFlat);
                    ComputeRotationDirection();
                    projectorRangeTarget = (target.Position - Position).ToVector3().magnitude;
                }

                TryBlindTarget();
            }

            // Update the projector rotation and range.
            ProjectorMotionTick();

            // Start a new idle motion when projector is paused for a moment.
            IdleMotionTick();
        }

        // ===================== Utility Function =====================
        /// <summary>
        ///     Only perform main treatment if it returns true.
        /// </summary>
        protected abstract bool CheckAdditionalConditions();

        /// <summary>
        ///     Action when powered on.
        /// </summary>
        protected void OnPoweredOn()
        {
            SynchronizeProjectorsInGroup();
        }

        /// <summary>
        ///     Action when powered off.
        /// </summary>
        protected void OnPoweredOff()
        {
            StopTargetting();
            SwitchOffLight();
        }

        /// <summary>
        ///     Reset rotation and range rates.
        /// </summary>
        private void StopTargetting()
        {
            target = null;
            projectorRotationRateInTicks = projectorRotationRateInTicksIdle;
            projectorRangeRateInTicks = projectorRangeRateInTicksIdle;
        }

        /// <summary>
        ///     Set target, rotation and range rates and play a sound.
        /// </summary>
        private void StartTargetting(Pawn newTarget)
        {
            target = newTarget;
            projectorRotationRateInTicks = projectorRotationRateInTicksTargetting;
            projectorRangeRateInTicks = projectorRangeRateInTicksTargetting;
            SoundDefOf.TurretAcquireTarget.PlayOneShot(new TargetInfo(Position, Map));
            nextTargetSearchTick =
                Find.TickManager.TicksGame; // So target rotation and range will be immediately updated.
        }

        /// <summary>
        ///     Look for a valid target to light: an hostile unroofed pawn within range.
        /// </summary>
        private Pawn LookForNewTarget()
        {
            var hostilesInSight = new List<Pawn>();

            if (Faction == null)
            {
                return null;
            }

            foreach (var pawn in Map.mapPawns.AllPawnsSpawned)
            {
                if (IsPawnValidTarget(pawn))
                {
                    hostilesInSight.Add(pawn);
                }
            }

            if (hostilesInSight.Count > 0)
            {
                return hostilesInSight.RandomElement();
            }

            return null;
        }

        /// <summary>
        ///     Check if a pawn is a valid target.
        /// </summary>
        protected abstract bool IsPawnValidTarget(Pawn pawn);

        /// <summary>
        ///     Blind target if it is facing the projector.
        /// </summary>
        private void TryBlindTarget()
        {
            if (target == null || !target.RaceProps.Humanlike || projectorRotation != projectorRotationTarget ||
                projectorRange != projectorRangeTarget)
            {
                return;
            }

            var deltaAngle = ComputeAbsoluteAngleDelta(projectorRotation, target.Rotation.AsAngle);
            if (!(deltaAngle >= 90f) || !(deltaAngle <= 270f))
            {
                return;
            }

            var targetIsWearingMarineHelmet = false;
            foreach (var apparel in target.apparel.WornApparel)
            {
                if (apparel.def != ThingDef.Named("Apparel_PowerArmorHelmet"))
                {
                    continue;
                }

                targetIsWearingMarineHelmet = true;
                break;
            }

            var targetHasBionicEye = target.health.hediffSet.HasHediff(HediffDefOf.BionicEye)
                                     || target.health.hediffSet.HasHediff(HediffDef.Named("ArchotechEye"));

            if (targetHasBionicEye == false
                && targetIsWearingMarineHelmet == false)
            {
                target.health.AddHediff(Util_Projector.BlindedByProjectorDef);
            }
        }

        /// <summary>
        ///     Power off the light.
        /// </summary>
        protected void SwitchOffLight()
        {
            if (light.DestroyedOrNull() == false)
            {
                light.Destroy();
            }

            light = null;
        }

        /// <summary>
        ///     Light an area at given position.
        /// </summary>
        protected void SwitchOnLight(IntVec3 position)
        {
            if (position.InBounds(Map) == false)
            {
                // Out of bounds.
                // Known limitation: when a projector near map border is facing outside, it will not light objects that should normally block the light.
                SwitchOffLight();
                return;
            }

            // Remove old light if target has moved.
            if (light.DestroyedOrNull() == false
                && position != light.Position)
            {
                SwitchOffLight();
            }

            // Spawn a new light.
            if (!light.DestroyedOrNull())
            {
                return;
            }

            // Note: we could forbid several lights on the same spot but as glowers stack, it is visually better.
            /*Thing potentialLight = position.GetFirstThing(this.Map, Util_Projector.ProjectorLightDef);
                if (potentialLight == null)*/
            {
                light = GenSpawn.Spawn(Util_Projector.MobileProjectorLightDef, position, Map);
            }
        }

        /// <summary>
        ///     Synchronize a group of projectors.
        /// </summary>
        private void SynchronizeProjectorsInGroup()
        {
            if (groupId == 0)
            {
                StartNewIdleMotion();
            }

            foreach (var projector in GetPoweredAndIdleProjectorsWithGroupId(groupId))
            {
                projector.StartNewIdleMotion(true);
            }
        }

        private List<Building_MobileProjector> GetPoweredAndIdleProjectorsWithGroupId(int groupId)
        {
            var projectorsList = new List<Building_MobileProjector>();

            foreach (var building in Map.listerBuildings.AllBuildingsColonistOfDef(Util_Projector.ProjectorTowerDef))
            {
                if (building is Building_MobileProjectorTower projector
                    && projector.groupId == groupId
                    && projector.target == null
                    && projector.isRoofed == false
                    && projector.powerComp.PowerOn)
                {
                    projectorsList.Add(projector);
                }
            }

            /**foreach (Building building in this.Map.listerBuildings.AllBuildingsColonistOfDef(Util_Projector.ProjectorTurretDef))
            {
                Building_MobileProjectorTurret projector = building as Building_MobileProjectorTurret;
                if ((projector != null)
                    && (projector.groupId == groupId)
                    && (projector.target == null)
                    && projector.powerComp.PowerOn)
                {
                    projectorsList.Add(projector);
                }
            }**/
            return projectorsList;
        }

        /// <summary>
        ///     Update projector to face the target.
        /// </summary>
        private void ProjectorMotionTick()
        {
            // Update projector rotation.
            if (projectorRotation != projectorRotationTarget)
            {
                var rotationRate = projectorRotationRateInTicks;
                var deltaAngle = ComputeAbsoluteAngleDelta(projectorRotation, projectorRotationTarget);
                if (deltaAngle < 20f)
                {
                    rotationRate *= 2; // Slow down rotation when reaching target rotation.
                }

                if (Find.TickManager.TicksGame % rotationRate == 0)
                {
                    projectorRotation = projectorRotationClockwise
                        ? Mathf.Repeat(projectorRotation + 1f, 360f)
                        : Mathf.Repeat(projectorRotation - 1f, 360f);
                }
            }

            // Update projector range.
            if (projectorRange != projectorRangeTarget)
            {
                if (Find.TickManager.TicksGame % projectorRangeRateInTicks == 0)
                {
                    if (Mathf.Abs(projectorRangeTarget - projectorRange) < 1f)
                    {
                        projectorRange = projectorRangeTarget;
                    }
                    else if (projectorRange < projectorRangeTarget)
                    {
                        projectorRange++;
                    }
                    else
                    {
                        projectorRange--;
                    }
                }
            }

            // Light the area in front of the projector.
            var lightVector3 = new Vector3(0, 0, projectorRange).RotatedBy(projectorRotation);
            var lightIntVec3 = new IntVec3(Mathf.RoundToInt(lightVector3.x), 0, Mathf.RoundToInt(lightVector3.z));
            var projectorTarget = Position + lightIntVec3;
            TryLightTarget(projectorTarget);
        }

        /// <summary>
        ///     Try light the target position.
        /// </summary>
        protected abstract void TryLightTarget(IntVec3 targetPosition);

        /// <summary>
        ///     Start a new idle motion when projector is paused for a moment.
        /// </summary>
        private void IdleMotionTick()
        {
            if (target == null
                && projectorRotation == projectorRotationTarget
                && projectorRange == projectorRangeTarget
                && idlePauseTicks > 0)
            {
                // Motion is finished, decrement pause counter.
                idlePauseTicks--;
            }

            if (idlePauseTicks != 0)
            {
                return;
            }

            if (groupId == 0)
            {
                // Solo projector.
                StartNewIdleMotion();
            }
            else
            {
                // Group of projectors: check all projectors have finished their pause.
                var allProjectorsAreIdle = true;
                foreach (var projector in GetPoweredAndIdleProjectorsWithGroupId(groupId))
                {
                    if (projector.idlePauseTicks <= 0)
                    {
                        continue;
                    }

                    allProjectorsAreIdle = false;
                    break;
                }

                if (!allProjectorsAreIdle)
                {
                    return;
                }

                foreach (var projector in GetPoweredAndIdleProjectorsWithGroupId(groupId))
                {
                    projector.StartNewIdleMotion();
                }
            }
        }

        /// <summary>
        ///     Compute projector target rotation, target range and rotation direction.
        /// </summary>
        private void StartNewIdleMotion(bool startNewCycle = false)
        {
            idlePauseTicks = idlePauseDurationInTicks;
            switch (lightMode)
            {
                case LightMode.Automatic:
                    projectorRotationTarget = Rand.Range(0, 360);
                    projectorRangeTarget = Rand.Range(projectorMinRange, def.specialDisplayRadius);
                    break;
                case LightMode.Conic:
                    if (startNewCycle
                        || projectorRotation ==
                        Mathf.Repeat(Rotation.AsAngle + projectorRotationBaseOffset - 45f, 360f))
                    {
                        // Projector is targeting the left. Now, target the right.
                        projectorRotationTarget =
                            Mathf.Repeat(Rotation.AsAngle + projectorRotationBaseOffset + 45f, 360f);
                    }
                    else
                    {
                        // Projector is targeting the right. Now, target the left.
                        projectorRotationTarget =
                            Mathf.Repeat(Rotation.AsAngle + projectorRotationBaseOffset - 45f, 360f);
                    }

                    projectorRangeTarget = projectorRangeBaseOffset;
                    break;
                case LightMode.Fixed:
                    // Fixed range and rotation.
                    projectorRotationTarget = Mathf.Repeat(Rotation.AsAngle + projectorRotationBaseOffset, 360f);
                    projectorRangeTarget = projectorRangeBaseOffset;
                    break;
            }

            // Compute rotation direction.
            ComputeRotationDirection();
        }

        /// <summary>
        ///     Compute the optimal rotation direction.
        /// </summary>
        private void ComputeRotationDirection()
        {
            if (projectorRotationTarget >= projectorRotation)
            {
                var dif = projectorRotationTarget - projectorRotation;
                projectorRotationClockwise = dif <= 180f;
            }
            else
            {
                var dif = projectorRotation - projectorRotationTarget;
                projectorRotationClockwise = !(dif <= 180f);
            }
        }

        /// <summary>
        ///     Compute the absolute delta angle between two angles.
        /// </summary>
        private float ComputeAbsoluteAngleDelta(float angle1, float angle2)
        {
            var absoluteDeltaAngle = Mathf.Abs(angle2 - angle1);
            return absoluteDeltaAngle;
        }

        // ===================== Gizmo =====================
        public override IEnumerable<Gizmo> GetGizmos()
        {
            IList<Gizmo> buttonList = new List<Gizmo>();
            var groupKeyBase = 700000102;

            var lightModeButton = new Command_Action();
            switch (lightMode)
            {
                case LightMode.Conic:
                    lightModeButton.defaultLabel = "Ligth mode: conic.";
                    lightModeButton.defaultDesc = "In this mode, the projector patrols in a conic area in front of it.";
                    break;
                case LightMode.Automatic:
                    lightModeButton.defaultLabel = "Ligth mode: automatic.";
                    lightModeButton.defaultDesc = "In this mode, the projector randomly lights the surroundings.";
                    break;
                case LightMode.Fixed:
                    lightModeButton.defaultLabel = "Ligth mode: fixed.";
                    lightModeButton.defaultDesc = "In this mode, the projector lights a fixed area.";
                    break;
            }

            lightModeButton.icon = ContentFinder<Texture2D>.Get("ui/Commands/CommandButton_SwitchMode");
            lightModeButton.activateSound = SoundDef.Named("Click");
            lightModeButton.action = SwitchLigthMode;
            lightModeButton.groupKey = groupKeyBase + 1;
            buttonList.Add(lightModeButton);

            if (lightMode == LightMode.Conic
                || lightMode == LightMode.Fixed)
            {
                var decreaseRangeButton = new Command_Action
                {
                    icon = ContentFinder<Texture2D>.Get("ui/Commands/CommandButton_DecreaseRange"),
                    defaultLabel = "Range: " + projectorRangeBaseOffset,
                    defaultDesc = "Decrease range.",
                    activateSound = SoundDef.Named("Click"),
                    action = DecreaseProjectorRange,
                    groupKey = groupKeyBase + 2
                };
                buttonList.Add(decreaseRangeButton);

                var increaseRangeButton = new Command_Action
                {
                    icon = ContentFinder<Texture2D>.Get("ui/Commands/CommandButton_IncreaseRange"),
                    defaultLabel = "",
                    defaultDesc = "Increase range.",
                    activateSound = SoundDef.Named("Click"),
                    action = IncreaseProjectorRange,
                    groupKey = groupKeyBase + 3
                };
                buttonList.Add(increaseRangeButton);

                var rotation = Mathf.Repeat(Rotation.AsAngle + projectorRotationBaseOffset, 360f);
                var turnLeftButton = new Command_Action
                {
                    icon = ContentFinder<Texture2D>.Get("ui/Commands/CommandButton_TurnLeft"),
                    defaultLabel = "Rotation: " + rotation + "°",
                    defaultDesc = "Turn left.",
                    activateSound = SoundDef.Named("Click"),
                    action = AddProjectorBaseRotationLeftOffset,
                    groupKey = groupKeyBase + 4
                };
                buttonList.Add(turnLeftButton);

                var turnRightButton = new Command_Action
                {
                    icon = ContentFinder<Texture2D>.Get("ui/Commands/CommandButton_TurnRight"),
                    defaultLabel = "",
                    defaultDesc = "Turn right.",
                    activateSound = SoundDef.Named("Click"),
                    action = AddProjectorBaseRotationRightOffset,
                    groupKey = groupKeyBase + 5
                };
                buttonList.Add(turnRightButton);
            }

            var setTargetButton = new Command_Action
            {
                icon = ContentFinder<Texture2D>.Get("UI/Commands/Attack"),
                defaultLabel = "Set target",
                defaultDesc =
                    "Order the tower to light a specific target. Can only target unroofed hostiles in range.",
                activateSound = SoundDef.Named("Click"),
                action = SelectTarget,
                groupKey = groupKeyBase + 6
            };
            buttonList.Add(setTargetButton);

            var synchronizeButton = new Command_Action
            {
                icon = ContentFinder<Texture2D>.Get("ui/Commands/CommandButton_Synchronize"),
                defaultLabel = "Group: " + groupId,
                defaultDesc = "Synchronize the selected projectors and select conic mode.",
                activateSound = SoundDef.Named("Click"),
                action = SetNewTowersGroup,
                groupKey = groupKeyBase + 7
            };
            buttonList.Add(synchronizeButton);

            var basebuttonList = base.GetGizmos();
            var resultButtonList = basebuttonList != null ? basebuttonList.Concat(buttonList) : buttonList;
            return resultButtonList;
        }

        /// <summary>
        ///     Switch light mode.
        /// </summary>
        private void SwitchLigthMode()
        {
            groupId = 0;
            switch (lightMode)
            {
                case LightMode.Automatic:
                    lightMode = LightMode.Conic;
                    break;
                case LightMode.Conic:
                    lightMode = LightMode.Fixed;
                    break;
                case LightMode.Fixed:
                    lightMode = LightMode.Automatic;
                    break;
            }

            StartNewIdleMotion();
        }

        /// <summary>
        ///     Add an offset to the projector base rotation.
        /// </summary>
        private void AddProjectorBaseRotationLeftOffset()
        {
            projectorRotationBaseOffset = Mathf.Repeat(projectorRotationBaseOffset - 10f, 360f);
            if (groupId > 0)
            {
                SynchronizeProjectorsInGroup();
            }
            else
            {
                StartNewIdleMotion();
            }
        }

        /// <summary>
        ///     Add an offset to the projector base rotation.
        /// </summary>
        private void AddProjectorBaseRotationRightOffset()
        {
            projectorRotationBaseOffset = Mathf.Repeat(projectorRotationBaseOffset + 10f, 360f);
            if (groupId > 0)
            {
                SynchronizeProjectorsInGroup();
            }
            else
            {
                StartNewIdleMotion();
            }
        }

        /// <summary>
        ///     Decrease the projector range.
        /// </summary>
        private void DecreaseProjectorRange()
        {
            if (projectorRangeBaseOffset > projectorMinRange)
            {
                projectorRangeBaseOffset -= 1f;
                projectorRangeTarget = projectorRangeBaseOffset;
            }

            if (groupId > 0)
            {
                SynchronizeProjectorsInGroup();
            }
        }

        /// <summary>
        ///     Increase the projector range.
        /// </summary>
        private void IncreaseProjectorRange()
        {
            if (projectorRangeBaseOffset < Mathf.Round(def.specialDisplayRadius))
            {
                projectorRangeBaseOffset += 1f;
                projectorRangeTarget = projectorRangeBaseOffset;
            }

            if (groupId > 0)
            {
                SynchronizeProjectorsInGroup();
            }
        }

        /// <summary>
        ///     Manually select a target to light.
        /// </summary>
        private void SelectTarget()
        {
            var targetingParams = new TargetingParameters
            {
                canTargetPawns = true,
                canTargetBuildings = false,
                neverTargetIncapacitated = true,
                validator = delegate(TargetInfo targ)
                {
                    if (targ.HasThing
                        && targ.Thing is Pawn pawn
                        && IsPawnValidTarget(pawn))
                    {
                        return true;
                    }

                    return false;
                }
            };
            Find.Targeter.BeginTargeting(targetingParams, SetForcedTarget);
        }

        private void SetForcedTarget(LocalTargetInfo forcedTarget)
        {
            StartTargetting(forcedTarget.Thing as Pawn);
        }

        /// <summary>
        ///     Group the selected towers so they are synchronized when idle.
        /// </summary>
        private void SetNewTowersGroup()
        {
            if (groupIdJustChanged)
            {
                return;
            }

            var projectorsInGroup = new List<Building_MobileProjector>();
            foreach (var obj in Find.Selector.SelectedObjectsListForReading)
            {
                if (obj is Building_MobileProjector projector)
                {
                    projectorsInGroup.Add(projector);
                }
            }

            if (projectorsInGroup.Count == 1)
            {
                projectorsInGroup.First().groupId = 0;
            }
            else
            {
                foreach (var mobileProjector in projectorsInGroup)
                {
                    mobileProjector.groupId = nextGroupId;
                    mobileProjector.lightMode = LightMode.Conic;
                    mobileProjector.groupIdJustChanged = true;
                }

                SynchronizeProjectorsInGroup();
                FindNextGroupId();
            }
        }

        private void FindNextGroupId()
        {
            const int maxIdValue = 1000;
            for (var id = 1; id <= 1000; id++)
            {
                if (id == maxIdValue)
                {
                    Log.Warning("MiningCo. projector tower: found no free group ID. Resetting to 1.");
                    nextGroupId = 1;
                    return;
                }

                var idIsFree = true;
                foreach (var thing in Map.listerThings.ThingsOfDef(Util_Projector.ProjectorTowerDef))
                {
                    if (!(thing is Building_MobileProjector tower) || tower.groupId != id)
                    {
                        continue;
                    }

                    idIsFree = false;
                    break;
                }

                /**foreach (Thing thing in this.Map.listerThings.ThingsOfDef(Util_Projector.ProjectorTurretDef))
                {
                    Building_MobileProjector turret = thing as Building_MobileProjector;
                    if ((turret != null)
                        && (turret.groupId == id))
                    {
                        idIsFree = false;
                        break;
                    }
                }**/
                if (!idIsFree)
                {
                    continue;
                }

                nextGroupId = id;
                break;
            }
        }

        // ===================== Draw =====================
        /// <summary>
        ///     Draw the projector and a line to the targeted pawn.
        /// </summary>
        public override void Draw()
        {
            base.Draw();
            groupIdJustChanged =
                false; // This is done in the draw function so we can change the group of a tower several times even when the game is paused.
        }

        private enum LightMode
        {
            Conic,
            Automatic,
            Fixed
        }
    }
}