using RimWorld;
using UnityEngine;
using Verse;

namespace O21Toolbox.PawnCrafter
{
    /// <summary>
    ///     When clicked shows the currently printed pawn.
    /// </summary>
    [StaticConstructorOnStartup]
    public class Gizmo_PrinterPawnInfo : Command
    {
        /// <summary>
        ///     Placeholder icon for drawing nothing.
        /// </summary>
        public static Texture2D emptyIcon = ContentFinder<Texture2D>.Get("UI/Overlays/ThingLine");

        public string description = "AndroidGizmoPrinterAndroidInfoDescription";

        /// <summary>
        ///     Linked printer.
        /// </summary>
        public IPawnCrafter printer;

        static Gizmo_PrinterPawnInfo()
        {
        }

        public Gizmo_PrinterPawnInfo(IPawnCrafter printer)
        {
            this.printer = printer;

            //Start
            defaultLabel = printer.PawnBeingCrafted().Name.ToStringFull;
            defaultDesc = description.Translate();
            icon = emptyIcon;
        }

        public override GizmoResult GizmoOnGUI(Vector2 topLeft, float maxWidth)
        {
            var result = base.GizmoOnGUI(topLeft, maxWidth);

            if (printer.PawnBeingCrafted().IsColonistPlayerControlled)
            {
                //Custom render.
                var width = GetWidth(maxWidth);
                var pawnRect = new Rect(topLeft.x + 10f, topLeft.y, width - 40f, width - 20f);
                var PawnPortraitSize = new Vector2(width - 20f, width);

                GUI.DrawTexture(new Rect(pawnRect.x, pawnRect.y, PawnPortraitSize.x, PawnPortraitSize.y),
                    PortraitsCache.Get(printer.PawnBeingCrafted(), PawnPortraitSize));
            }

            return result;
        }

        public override void ProcessInput(Event ev)
        {
            base.ProcessInput(ev);

            Find.WindowStack.Add(new Dialog_InfoCard(printer.PawnBeingCrafted()));
        }
    }
}