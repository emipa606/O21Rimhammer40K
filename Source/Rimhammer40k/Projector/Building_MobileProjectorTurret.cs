using RimWorld;
using UnityEngine;
using Verse;

namespace Rimhammer40k.Projector
{
    /// <summary>
    ///     Building_MobileProjectorTurret class.
    /// </summary>
    /// <author>Rikiki</author>
    /// <permission>
    ///     Use this code as you want, just remember to add a link to the corresponding Ludeon forum mod release
    ///     thread.
    /// </permission>
    [StaticConstructorOnStartup]
    public class Building_MobileProjectorTurret : Building_MobileProjector
    {
        // ===================== Main Work Function =====================
        /// <summary>
        ///     No additional conditions.
        /// </summary>
        protected override bool CheckAdditionalConditions()
        {
            return true;
        }

        // ===================== Utility Function =====================
        /// <summary>
        ///     Try light the target position.
        /// </summary>
        protected override void TryLightTarget(IntVec3 targetPosition)
        {
            if (targetPosition.CanBeSeenOver(Map)
                && GenSight.LineOfSight(Position, targetPosition, Map))
            {
                SwitchOnLight(targetPosition);
            }
            else
            {
                var farthestPosition = TryGetFarthestPositionInSight(targetPosition);
                if (farthestPosition.IsValid)
                {
                    SwitchOnLight(farthestPosition);
                }
                else
                {
                    SwitchOffLight();
                }
            }
        }

        /// <summary>
        ///     Check if a pawn is a valid target.
        /// </summary>
        protected override bool IsPawnValidTarget(Pawn pawn)
        {
            if (pawn.HostileTo(Faction)
                && pawn.Downed == false
                && pawn.Position.InHorDistOf(Position, def.specialDisplayRadius)
                && GenSight.LineOfSight(Position, pawn.Position, Map))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        ///     Get the farthest position from the projector in direction of target.
        /// </summary>
        private IntVec3 TryGetFarthestPositionInSight(IntVec3 target)
        {
            var farthestPosition = Position;

            Mathf.Clamp(target.x, 0, Map.Size.x);
            Mathf.Clamp(target.z, 0, Map.Size.z);

            var lineOfSightPoints = GenSight.PointsOnLineOfSight(Position, target);
            foreach (var point in lineOfSightPoints)
            {
                if (point.InBounds(Map) == false)
                {
                    farthestPosition = IntVec3.Invalid;
                    break;
                }

                if (point.CanBeSeenOver(Map) == false)
                {
                    // Return last non-blocked position.
                    break;
                }

                farthestPosition = point; // Store last valid point in sight.
            }

            return farthestPosition;
        }

        // ===================== Draw =====================
        /// <summary>
        ///     Draw the projector and a line to the targeted pawn.
        /// </summary>
        public override void Draw()
        {
            base.Draw();
            projectorMatrix.SetTRS(base.DrawPos + Altitudes.AltIncVect, projectorRotation.ToQuat(), projectorScale);
            if (powerComp.PowerOn)
            {
                Graphics.DrawMesh(MeshPool.plane10, projectorMatrix, projectorOnTexture, 0);
                projectorLightEffectMatrix.SetTRS(base.DrawPos + Altitudes.AltIncVect, projectorRotation.ToQuat(),
                    projectorLightEffectScale);
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