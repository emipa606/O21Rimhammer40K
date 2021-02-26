using RimWorld;
using Verse; // Always needed
//using VerseBase;   // Material/Graphics handling functions are found here
// RimWorld specific functions are found here

// RimWorld universal objects are here
//using Verse.AI;    // Needed when you do something with the AI
//using Verse.Sound; // Needed when you do something with the Sound

namespace Rimhammer40k.Projector
{
    /// <summary>
    ///     Alert_ProjectorTowerRoofed class.
    /// </summary>
    /// <author>Rikiki</author>
    /// <permission>
    ///     Use this code as you want, just remember to add a link to the corresponding Ludeon forum mod release
    ///     thread.
    /// </permission>
    public class Alert_ProjectorTowerRoofed : Alert
    {
        public Alert_ProjectorTowerRoofed()
        {
            defaultLabel = "Projector tower roofed";
            defaultExplanation =
                "One of your projector towers is roofed and has been deactivated. Remove the roof above it to reactivate it.";
            defaultPriority = AlertPriority.Medium;
        }

        public override AlertReport GetReport()
        {
            var maps = Find.Maps;
            foreach (var map in maps)
            {
                foreach (var building in map.listerBuildings.AllBuildingsColonistOfDef(Util_Projector.ProjectorTowerDef)
                )
                {
                    if (building is Building_MobileProjectorTower tower && tower.isRoofed)
                    {
                        return AlertReport.CulpritIs(tower);
                    }
                }
            }

            return AlertReport.Inactive;
        }
    }
}