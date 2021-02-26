using RimWorld;
using Verse;

namespace Rimhammer40k.Orks.Extensions
{
    [StaticConstructorOnStartup]
    public static class PawnExt
    {
        public static readonly FleshTypeDef orkFlesh;

        static PawnExt()
        {
            orkFlesh = DefDatabase<FleshTypeDef>.GetNamed("Ork");
        }

        public static bool IsOrk(this Pawn pawn)
        {
            return pawn.RaceProps.FleshType == orkFlesh;
        }

        public static bool IsNotOrk(this Pawn pawn)
        {
            return pawn.RaceProps.FleshType != orkFlesh;
        }
    }
}