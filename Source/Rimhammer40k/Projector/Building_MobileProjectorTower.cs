using RimWorld;
using UnityEngine;
using Verse;

namespace Rimhammer40k.Projector
{
    /// <summary>
    ///     Building_MobileProjectorTower class.
    /// </summary>
    /// <author>Rikiki</author>
    /// <permission>
    ///     Use this code as you want, just remember to add a link to the corresponding Ludeon forum mod release
    ///     thread.
    /// </permission>
    [StaticConstructorOnStartup]
    public class Building_MobileProjectorTower : Building_MobileProjector
    {
        // ===================== Variables =====================
        public bool isRoofed;

        // ===================== Setup Work =====================
        /// <summary>
        ///     Save and load internal state variables (stored in savegame data).
        /// </summary>
        public override void ExposeData()
        {
            base.ExposeData();

            Scribe_Values.Look(ref isRoofed, "isRoofed");
        }

        // ===================== Utility Function =====================
        /// <summary>
        ///     Update roof state and perform action on state change.
        /// </summary>
        protected override bool CheckAdditionalConditions()
        {
            if (isRoofed
                && Position.Roofed(Map) == false)
            {
                // Becomes unroofed.
                isRoofed = false;
                OnPoweredOn();
            }
            else if (isRoofed == false
                     && Position.Roofed(Map))
            {
                // Becomes roofed.
                isRoofed = true;
                OnPoweredOff();
            }

            return isRoofed == false;
        }

        /// <summary>
        ///     Try light the target position.
        /// </summary>
        protected override void TryLightTarget(IntVec3 targetPosition)
        {
            if (targetPosition.InBounds(Map) == false)
            {
                SwitchOffLight();
                return;
            }

            var building = targetPosition.GetEdifice(Map);
            if (targetPosition.Roofed(Map)
                || building != null
                && building.def.Fillage == FillCategory.Full)
            {
                SwitchOffLight();
                return;
            }

            SwitchOnLight(targetPosition);
        }

        /// <summary>
        ///     Check if a pawn is a valid target.
        /// </summary>
        protected override bool IsPawnValidTarget(Pawn pawn)
        {
            if (pawn.Spawned
                && pawn.HostileTo(Faction)
                && pawn.Downed == false
                && pawn.Position.InHorDistOf(Position, def.specialDisplayRadius)
                && pawn.Position.Roofed(pawn.Map) == false)
            {
                return true;
            }

            return false;
        }

        // ===================== Draw =====================
        /// <summary>
        ///     Draw the projector and a line to the targeted pawn.
        ///     The small draw offset (AltitudeLayer.Blueprint) is used to draw the projector and light above the pawns.
        /// </summary>
        public override void Draw()
        {
            base.Draw();
            projectorMatrix.SetTRS(
                base.DrawPos + new Vector3(0f, AltitudeLayer.Blueprint.AltitudeFor(), 0f) + Altitudes.AltIncVect,
                projectorRotation.ToQuat(), projectorScale);
            if (powerComp.PowerOn
                && isRoofed == false)
            {
                Graphics.DrawMesh(MeshPool.plane10, projectorMatrix, projectorOnTexture, 0);
                projectorLightEffectMatrix.SetTRS(
                    base.DrawPos + new Vector3(0f, AltitudeLayer.Blueprint.AltitudeFor(), 0f) + Altitudes.AltIncVect,
                    projectorRotation.ToQuat(), projectorLightEffectScale);
                Graphics.DrawMesh(MeshPool.plane10, projectorLightEffectMatrix, projectorLightEffectTexture, 0);
            }
            else
            {
                Graphics.DrawMesh(MeshPool.plane10, projectorMatrix, projectorOffTexture, 0);
            }

            if (!Find.Selector.IsSelected(this) || target == null)
            {
                return;
            }

            var lineOrigin = this.TrueCenter();
            var lineTarget = target.Position.ToVector3Shifted();
            lineTarget.y = AltitudeLayer.MetaOverlays.AltitudeFor();
            lineOrigin.y = lineTarget.y;
            GenDraw.DrawLineBetween(lineOrigin, lineTarget, targetLineTexture);
        }
    }
}