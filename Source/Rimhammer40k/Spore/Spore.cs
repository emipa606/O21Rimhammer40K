using Verse;

namespace Rimhammer40k.Spore
{
    public class Spore : ThingWithComps
    {
        public int ticksTillSelfPlant;

        public override void PostMake()
        {
            base.PostMake();

            ticksTillSelfPlant = Current.Game.tickManager.TicksGame + 4000;
        }

        public override void Tick()
        {
            base.Tick();

            if (Current.Game.tickManager.TicksGame < ticksTillSelfPlant)
            {
                return;
            }

            if (Map.terrainGrid.TerrainAt(Position).fertility >= 0.7)
            {
                if (Position.GetPlant(Map) != null)
                {
                    Position.GetPlant(Map).Destroy();
                }

                GenSpawn.Spawn(ThingDef.Named("O21_Plant_OrkoidShroom"), Position, Map);
            }

            Destroy();
        }
    }
}