using System;
using O21Toolbox.AutomatedProducer;
using RimWorld;
using Verse;
using Verse.AI;

namespace O21Toolbox.PawnCrafter
{
    /// <summary>
    ///     Generic variant of the Android Printer WorkGiver which make pawns attempt to fill up the crafter.
    /// </summary>
    public class WorkGiver_PawnCrafter : WorkGiver_Scanner
    {
        private PawnCrafterWorkgiverProperties intWorkGiverProperties = null;
        public override ThingRequest PotentialWorkThingRequest => ThingRequest.ForDef(WorkGiverProperties.defToScan);

        public override PathEndMode PathEndMode => PathEndMode.Touch;

        public PawnCrafterWorkgiverProperties WorkGiverProperties
        {
            get
            {
                if (intWorkGiverProperties == null)
                {
                    intWorkGiverProperties = def.GetModExtension<PawnCrafterWorkgiverProperties>();
                }

                return intWorkGiverProperties;
            }
        }

        public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            var pawnCrafter = t as Building_PawnCrafter;

            if (pawnCrafter == null || pawnCrafter.crafterStatus != CrafterStatus.Filling)
            {
                return false;
            }

            if (t.IsForbidden(pawn) ||
                !pawn.CanReserveAndReach(t, PathEndMode.Touch, pawn.NormalMaxDanger(), 1, -1, null, forced))
            {
                return false;
            }

            if (pawn.Map.designationManager.DesignationOn(t, DesignationDefOf.Deconstruct) != null)
            {
                return false;
            }

            //Check if there is anything to fill.
            var potentionalRequests = pawnCrafter.orderProcessor.PendingRequests();
            var validRequest = false;
            if (potentionalRequests != null)
            {
                foreach (var request in potentionalRequests)
                {
                    var ingredientThing = FindIngredient(pawn, pawnCrafter, request);
                    if (ingredientThing != null)
                    {
                        validRequest = true;
                        break;
                    }
                }
            }

            return validRequest;
        }

        public override Job JobOnThing(Pawn pawn, Thing crafterThing, bool forced = false)
        {
            var pawnCrafter = crafterThing as Building_PawnCrafter;

            var potentionalRequests = pawnCrafter.orderProcessor.PendingRequests();

            if (potentionalRequests != null)
            {
                foreach (var request in potentionalRequests)
                {
                    var ingredientThing = FindIngredient(pawn, pawnCrafter, request);

                    if (ingredientThing != null)
                    {
                        return new Job(WorkGiverProperties.fillJob, ingredientThing, crafterThing)
                        {
                            count = (int) request.amount
                        };
                    }
                }
            }

            return null;
        }

        /// <summary>
        ///     Tries to find a appropiate ingredient.
        /// </summary>
        /// <param name="pawn">Pawn to search for.</param>
        /// <param name="androidPrinter">Printer to fill.</param>
        /// <param name="request">Thing order request to fulfill.</param>
        /// <returns>Valid thing if found, otherwise null.</returns>
        private Thing FindIngredient(Pawn pawn, Building_PawnCrafter androidPrinter, ThingOrderRequest request)
        {
            if (request != null)
            {
                Predicate<Thing> predicate = x => !x.IsForbidden(pawn) && pawn.CanReserve(x);
                var validator = predicate;

                return GenClosest.ClosestThingReachable(pawn.Position, pawn.Map, request.Request(),
                    PathEndMode.ClosestTouch, TraverseParms.For(pawn), 9999f, validator);
            }

            return null;
        }
    }
}