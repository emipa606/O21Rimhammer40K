using System;
using RimWorld;
using Verse;

namespace Rimhammer40k.Necrons
{
    public class CompNecronResurrection : ThingComp
    {
        private readonly int ticksToResurrection = 60000;

        private bool IsResurrectable;

        private int resurrectTime = -1;
        public CompProperties_NecronResurrection Props => (CompProperties_NecronResurrection) props;

        private void AttemptResurrection()
        {
            var corpse = parent as Corpse;
            var rnd = new Random();
            var flag = rnd.Next(1, 100);
            if (corpse != null && corpse.InnerPawn.IsColonist)
            {
                if (flag < 75)
                {
                    IsResurrectable = true;
                }
            }
            else
            {
                if (flag < 25)
                {
                    IsResurrectable = true;
                }
            }
        }

        private bool ShouldResurrect()
        {
            if (resurrectTime > 0 && Current.Game.tickManager.TicksGame >= resurrectTime)
            {
                return true;
            }

            return false;
        }

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            if (respawningAfterLoad)
            {
                return;
            }

            AttemptResurrection();
        }

        public override void CompTickRare()
        {
            base.CompTickRare();
            var corpse = parent as Corpse;
            if (IsResurrectable && resurrectTime < 0)
            {
                resurrectTime = Current.Game.tickManager.TicksGame + ticksToResurrection;
            }

            if (!ShouldResurrect())
            {
                return;
            }

            if (corpse != null)
            {
                ResurrectionUtility.Resurrect(corpse.InnerPawn);
            }
        }
    }
}