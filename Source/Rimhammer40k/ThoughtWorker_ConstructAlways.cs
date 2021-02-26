using Rimhammer40k.Necrons.Extensions;
using RimWorld;
using Verse;

namespace Rimhammer40k
{
    public class ThoughtWorker_ConstructAlways : ThoughtWorker
    {
        protected override ThoughtState CurrentStateInternal(Pawn p)
        {
            var flag = p.def.race.FleshType == PawnExt.necronFlesh;
            ThoughtState result;
            if (flag)
            {
                result = ThoughtState.ActiveAtStage(0);
            }
            else
            {
                result = ThoughtState.Inactive;
            }

            return result;
        }
    }
}