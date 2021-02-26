using RimWorld;

namespace Rimhammer40k.Biomes
{
    [DefOf]
    public static class BiomeDefOf
    {
        public static BiomeDef GaussDesertGreen;

        public static BiomeDef GaussDesertBlue;

        public static BiomeDef GaussDesertOrange;

        static BiomeDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(BiomeDefOf));
        }
    }
}