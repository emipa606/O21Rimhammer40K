using System.Collections.Generic;
using O21Toolbox.AutomatedProducer;
using RimWorld;
using Verse;

namespace O21Toolbox.PawnCrafter
{
    /// <summary>
    ///     Helps in processing multiplie Thing Order Requests.
    /// </summary>
    public class ThingOrderProcessor : IExposable
    {
        /// <summary>
        ///     List of requested. And ideal.
        /// </summary>
        public List<ThingOrderRequest> requestedItems = new List<ThingOrderRequest>();

        /// <summary>
        ///     Storage settings to take in account for Nutrition.
        /// </summary>
        public StorageSettings storageSettings;

        /// <summary>
        ///     Inventory to check for.
        /// </summary>
        public ThingOwner thingHolder;

        public ThingOrderProcessor()
        {
        }

        public ThingOrderProcessor(ThingOwner thingHolder, StorageSettings storageSettings)
        {
            this.thingHolder = thingHolder;
            this.storageSettings = storageSettings;
        }

        public void ExposeData()
        {
            Scribe_Collections.Look(ref requestedItems, "requestedItems", LookMode.Deep);
        }

        /// <summary>
        ///     Gets all pending requests that need to be processed using ideal requests as a base.
        /// </summary>
        /// <returns>Pending requests or none.</returns>
        public IEnumerable<ThingOrderRequest> PendingRequests()
        {
            foreach (var idealRequest in requestedItems)
            {
                //Item
                float totalItemCount = thingHolder.TotalStackCountOfDef(idealRequest.thingDef);
                if (totalItemCount < idealRequest.amount)
                {
                    var request = new ThingOrderRequest();
                    request.thingDef = idealRequest.thingDef;
                    request.amount = idealRequest.amount - totalItemCount;

                    yield return request;
                }
            }
        }

        /// <summary>
        ///     Counts all available nutrition in the printer.
        /// </summary>
        /// <returns>Total nutrition.</returns>
        public float CountNutrition()
        {
            var totalNutrition = 0f;

            //Count nutrition.
            foreach (var item in thingHolder)
            {
                var corpse = item as Corpse;
                if (corpse != null)
                {
                    totalNutrition +=
                        FoodUtility.GetBodyPartNutrition(corpse, corpse.InnerPawn.RaceProps.body.corePart);
                }
                else
                {
                    if (item.def.IsIngestible)
                    {
                        totalNutrition += (item.def?.ingestible.CachedNutrition ?? 0.05f) * item.stackCount;
                    }
                }
            }

            return totalNutrition;
        }
    }
}