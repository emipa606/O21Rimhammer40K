using Verse;

namespace Rimhammer40k.Spore
{
    public class CompProperties_SporeSelfPlanting : CompProperties
    {
        public bool isOrkoid = true;

        public ThingDef spawnedThing = null;

        public int timeTillSpawn = 60000;

        public CompProperties_SporeSelfPlanting()
        {
            compClass = typeof(Comp_SporeSelfPlanting);
        }
    }
}