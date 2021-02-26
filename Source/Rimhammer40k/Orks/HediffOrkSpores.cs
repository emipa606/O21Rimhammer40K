using UnityEngine;
using Verse;

namespace Rimhammer40k.Orks
{
    public class HediffOrkSpores : HediffWithComps
    {
        public int ticksUntilNextSpore;

        public override void PostMake()
        {
            base.PostMake();
            SetNextSporeTick();
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref ticksUntilNextSpore, "ticksUntilNextSpore");
        }

        public override void Tick()
        {
            base.Tick();
            if (Current.Game.tickManager.TicksGame >= ticksUntilNextSpore)
            {
                TryDropSpore();
            }
        }

        public void SetNextSporeTick()
        {
            ticksUntilNextSpore = Current.Game.tickManager.TicksGame + Random.Range(60000, 300000);
        }

        public void TryDropSpore()
        {
            if (pawn.Map != null)
            {
                GenSpawn.Spawn(ThingDef.Named("O21_OrkoidSpore"), pawn.Position, pawn.Map);
            }

            SetNextSporeTick();
        }
    }
}