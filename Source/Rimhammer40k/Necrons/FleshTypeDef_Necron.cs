using RimWorld;
using Verse;

namespace Rimhammer40k.Necrons.Extensions
{
    [StaticConstructorOnStartup]
    public static class PawnExt
    {
        public static readonly FleshTypeDef necronFlesh;

        static PawnExt()
        {
            necronFlesh = DefDatabase<FleshTypeDef>.GetNamed("Necron");
        }

        public static bool IsNecron(this Pawn pawn)
        {
            return pawn.RaceProps.FleshType == necronFlesh;
        }

        public static bool IsNotNecron(this Pawn pawn)
        {
            return pawn.RaceProps.FleshType != necronFlesh;
        }
    }
}