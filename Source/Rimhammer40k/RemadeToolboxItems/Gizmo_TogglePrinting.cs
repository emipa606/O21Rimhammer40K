using UnityEngine;
using Verse;

namespace O21Toolbox.PawnCrafter
{
    /// <summary>
    ///     Lets the player to turn on and off printing.
    /// </summary>
    [StaticConstructorOnStartup]
    public class Gizmo_TogglePrinting : Command
    {
        public static Texture2D startIcon = ContentFinder<Texture2D>.Get("UI/Commands/PodEject");
        public static Texture2D stopIcon = ContentFinder<Texture2D>.Get("UI/Designators/Cancel");
        public string descriptionStart = "GizmoTogglePawnCraftingStartDescription";
        public string descriptionStop = "GizmoTogglePawnCraftingStopDescription";

        public string labelStart = "GizmoTogglePawnCraftingStartLabel";

        public string labelStop = "GizmoTogglePawnCraftingStopLabel";

        public IPawnCrafter printer;

        static Gizmo_TogglePrinting()
        {
        }

        public Gizmo_TogglePrinting(IPawnCrafter printer)
        {
            this.printer = printer;

            if (printer.PawnCrafterStatus() == CrafterStatus.Idle)
            {
                //Start
                defaultLabel = labelStart.Translate();
                defaultDesc = descriptionStart.Translate();
                icon = startIcon;
            }
            else if (printer.PawnCrafterStatus() == CrafterStatus.Crafting ||
                     printer.PawnCrafterStatus() == CrafterStatus.Filling)
            {
                //Stop
                defaultLabel = labelStop.Translate();
                defaultDesc = descriptionStop.Translate();
                icon = stopIcon;
            }
        }

        public override void ProcessInput(Event ev)
        {
            base.ProcessInput(ev);

            if (printer.PawnCrafterStatus() == CrafterStatus.Idle)
            {
                //Start
                printer.InitiatePawnCrafting();
            }
            else
            {
                //Stop
                printer.StopPawnCrafting();
            }
        }
    }
}