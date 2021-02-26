using O21Toolbox.Utility;
using Verse;

namespace O21Toolbox.PawnConverter
{
    public class Util_FactionConvert
    {
        public static Pawn FactionConvert(Pawn pawn, bool makeFriendly, float chanceToBerserk, string reason)
        {
            if (makeFriendly)
            {
                if (pawn.guest != null)
                {
                    pawn.guest.SetGuestStatus(null);
                }

                pawn.SetFaction(pawn.Faction);
                pawn.workSettings.EnableAndInitialize();
            }

            if (chanceToBerserk > 0.0f)
            {
                if (Rand.Chance(chanceToBerserk))
                {
                    MentalBreakDefOf.Berserk.Worker.TryStart(pawn, reason, false);
                }
            }

            return pawn;
        }
    }
}