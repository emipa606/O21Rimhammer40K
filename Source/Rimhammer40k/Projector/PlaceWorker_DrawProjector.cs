using RimWorld;
using UnityEngine;
using Verse; // Always needed
//using VerseBase;   // Material/Graphics handling functions are found here
// RimWorld specific functions are found here

// RimWorld universal objects are here
//using Verse.AI;    // Needed when you do something with the AI
//using Verse.Sound; // Needed when you do something with the Sound

namespace Rimhammer40k.Projector
{
    /// <summary>
    ///     PlaceWorker_DrawProjector class.
    /// </summary>
    /// <author>Rikiki</author>
    /// <permission>
    ///     Use this code as you want, just remember to add a link to the corresponding Ludeon forum mod release
    ///     thread.
    /// </permission>
    public class PlaceWorker_DrawProjector : PlaceWorker
    {
        public override void DrawGhost(ThingDef def, IntVec3 loc, Rot4 rot, Color ghostCol, Thing thing = null)
        {
            var baseGraphic = GraphicDatabase.Get<Graphic_Single>(
                "Things/Building/Security/IG/Sabre/IG_sabre_searchlight", ShaderDatabase.Cutout, new Vector2(4f, 4f),
                Color.white);
            var graphic = GhostUtility.GhostGraphicFor(baseGraphic, def, ghostCol);
            graphic.DrawFromDef(GenThing.TrueCenter(loc, rot, def.Size, AltitudeLayer.MetaOverlays.AltitudeFor()), rot,
                def);
        }
    }
}