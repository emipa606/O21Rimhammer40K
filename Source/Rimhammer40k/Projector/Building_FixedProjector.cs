using System.Collections.Generic;
using RimWorld;
using Verse; // Always needed
//using VerseBase;   // Material/Graphics handling functions are found here
// RimWorld specific functions are found here

// RimWorld universal objects are here
//using Verse.AI;    // Needed when you do something with the AI
//using Verse.Sound; // Needed when you do something with the Sound

namespace Rimhammer40k.Projector
{
    /// <summary>
    ///     Building_FixedProjector class.
    /// </summary>
    /// <author>Rikiki</author>
    /// <permission>
    ///     Use this code as you want, just remember to add a link to the corresponding Ludeon forum mod release
    ///     thread.
    /// </permission>
    public class Building_FixedProjector : Building
    {
        private const int lineOfSightCheckPeriodInTicks = 30;

        // Projector range.
        private const int projectorRange = 2;

        // Light references.
        private Thing light;
        private int nextLineOfSightCheckTick;

        // Components references.
        private CompPowerTrader powerComp;


        // ===================== Setup Work =====================
        /// <summary>
        ///     Initialize instance variables.
        /// </summary>
        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);

            powerComp = GetComp<CompPowerTrader>();
            nextLineOfSightCheckTick = Find.TickManager.TicksGame + Rand.Range(0, lineOfSightCheckPeriodInTicks);

            powerComp.powerStartedAction = OnPoweredOn;
            powerComp.powerStoppedAction = OnPoweredOff;
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

            Scribe_Values.Look(ref nextLineOfSightCheckTick, "nextLineOfSightCheckTick");
            Scribe_References.Look(ref light, "light");
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

            if (!powerComp.PowerOn || Find.TickManager.TicksGame < nextLineOfSightCheckTick)
            {
                return;
            }

            nextLineOfSightCheckTick = Find.TickManager.TicksGame + lineOfSightCheckPeriodInTicks;

            var lightCenterIsValid = GetLightCenterPosition(Map, Position, Rotation, out var lightCenter);
            if (lightCenterIsValid
                && GenSight.LineOfSight(Position, lightCenter, Map))
            {
                SwitchOnLight(lightCenter);
            }
            else
            {
                SwitchOffLight();
            }
        }

        // ===================== Utility Function =====================
        /// <summary>
        ///     Get the light center position according to the projector position and rotation.
        /// </summary>
        private static bool GetLightCenterPosition(Map map, IntVec3 projectorPosition, Rot4 projectorRotation,
            out IntVec3 lightCenterPosition)
        {
            lightCenterPosition = projectorPosition + new IntVec3(0, 0, projectorRange).RotatedBy(projectorRotation);
            if (lightCenterPosition.InBounds(map))
            {
                return true;
            }

            lightCenterPosition = projectorPosition;
            return false;
        }

        /// <summary>
        ///     Action when powered on.
        /// </summary>
        private void OnPoweredOn()
        {
            var target = Position + new IntVec3(0, 0, projectorRange).RotatedBy(Rotation);
            if (GenSight.LineOfSight(Position, target, Map))
            {
                SwitchOnLight(target);
            }
        }

        /// <summary>
        ///     Action when powered off.
        /// </summary>
        private void OnPoweredOff()
        {
            SwitchOffLight();
        }

        /// <summary>
        ///     Power off the light.
        /// </summary>
        private void SwitchOffLight()
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
        private void SwitchOnLight(IntVec3 position)
        {
            if (!light.DestroyedOrNull())
            {
                return;
            }

            // Note: we could forbid several lights on the same spot but as glowers stack, it is visually better.
            /*Thing potentialLight = position.GetFirstThing(this.Map, Util_Projector.ProjectorLightDef);
                if (potentialLight == null)*/
            {
                light = GenSpawn.Spawn(Util_Projector.FixedProjectorLightDef, position, Map);
            }
        }

        /// <summary>
        ///     Get the list of light cells.
        /// </summary>
        public static List<IntVec3> GetLightedCells(Map map, IntVec3 position, Rot4 rotation)
        {
            var lightedCellsList = new List<IntVec3>();

            var lightCenterIsValid = GetLightCenterPosition(map, position, rotation, out var lightCenter);
            var room = position.GetRoom(map);
            if (!lightCenterIsValid || room == null)
            {
                return lightedCellsList;
            }

            foreach (var cell in GenRadial.RadialCellsAround(lightCenter, 1.5f, true))
            {
                if (cell.InBounds(map)
                    && room == cell.GetRoom(map)
                    && GenSight.LineOfSight(position, cell, map))
                {
                    lightedCellsList.Add(cell);
                }
            }

            return lightedCellsList;
        }

        // ===================== Draw =====================
        /// <summary>
        ///     Draw the projector and a line to the targeted pawn.
        /// </summary>
        public override void Draw()
        {
            base.Draw();

            if (!Find.Selector.IsSelected(this))
            {
                return;
            }

            var lightedCellsList = GetLightedCells(Map, Position, Rotation);
            GenDraw.DrawFieldEdges(lightedCellsList);
        }
    }
}