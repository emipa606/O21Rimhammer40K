using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace Rimhammer40k
{
    public class Settings : ModSettings
    {
        private static readonly Dictionary<string, object> values = new Dictionary<string, object>();

        private static Vector2 _scrollposition = Vector2.zero;

        private static float _settingsHeight = 999f;

        private static readonly float rowHeight = 24f;

        private static readonly float rowMargin = 6f;

        public static void DoSettingsWindowContents(Rect canvas)
        {
            Widgets.BeginScrollView(canvas, ref _scrollposition, new Rect(0f, 0f, canvas.width - 19f, _settingsHeight));
            var num = 0f;
            Text.Font = GameFont.Medium;
            Text.Anchor = TextAnchor.LowerLeft;
            Widgets.Label(new Rect(0f, num, canvas.width, rowHeight * 2f), "Population Limits");
            Text.Anchor = 0;
            Text.Font = GameFont.Small;
            num += rowHeight * 2f;
            DrawLabeledInput(ref num, canvas, "Max Ork Population", ref Rimhammer40kMod.maxOrkPopulation,
                "Maximum Orks that will spawn from spores. Default: 10");
            DrawLabeledInput(ref num, canvas, "Max Grot Population", ref Rimhammer40kMod.maxGrotPopulation,
                "Maximum Grots that will spawn from spores. Default: 5");
            Text.Anchor = 0;
            Text.Font = GameFont.Small;
            var text =
                "Maximum populations will only limit how many of each will spawn from spores, it does not affect Storyteller soft cap and does not take into account non Ork/Grot pawns!";
            Text.Font = 0;
            var num2 = Text.CalcHeight(text, canvas.width);
            Widgets.Label(new Rect(0f, num, canvas.width, num2), text);
            Text.Font = GameFont.Small;
            num += num2;
            _settingsHeight = num;
            Widgets.EndScrollView();
        }

        public override void ExposeData()
        {
            Scribe_Values.Look(ref Rimhammer40kMod.necronsTeleportOnDeath, "NecronsTeleportOnDeath", true, true);
            Scribe_Values.Look(ref Rimhammer40kMod.maxOrkPopulation, "MaxOrkPopulation", 10, true);
            Scribe_Values.Look(ref Rimhammer40kMod.maxGrotPopulation, "MaxGrotPopulation", 5, true);
        }

        public static void DrawLabeledInput(ref float curY, Rect canvas, string label, ref float value, string tip = "")
        {
            Widgets.Label(new Rect(0f, curY, canvas.width / 3f * 2f, rowHeight), label);
            if (!values.ContainsKey(label))
            {
                values.Add(label, value);
            }

            GUI.SetNextControlName(label);
            values[label] = Widgets.TextField(new Rect(canvas.width / 3f * 2f, curY, canvas.width / 3f, rowHeight),
                values[label].ToString());
            if (tip != "")
            {
                TooltipHandler.TipRegion(new Rect(0f, curY, canvas.width, rowHeight), tip);
            }

            if (GUI.GetNameOfFocusedControl() != label && !float.TryParse(values[label].ToString(), out value))
            {
                Messages.Message("Invalid Float", MessageTypeDefOf.RejectInput);
                values[label] = value;
            }

            curY += rowHeight + rowMargin;
        }

        private static void DrawLabeledInput(ref float curY, Rect canvas, string label, ref int value, string tip = "")
        {
            Widgets.Label(new Rect(0f, curY, canvas.width / 3f * 2f, rowHeight), label);
            if (!values.ContainsKey(label))
            {
                values.Add(label, value);
            }

            GUI.SetNextControlName(label);
            values[label] = Widgets.TextField(new Rect(canvas.width / 3f * 2f, curY, canvas.width / 3f, rowHeight),
                values[label].ToString());
            if (tip != "")
            {
                TooltipHandler.TipRegion(new Rect(0f, curY, canvas.width, rowHeight), tip);
            }

            if (GUI.GetNameOfFocusedControl() != label && !int.TryParse(values[label].ToString(), out value))
            {
                Messages.Message("Invalid Integer", MessageTypeDefOf.RejectInput);
                values[label] = value;
            }

            curY += rowHeight + rowMargin;
        }

        public void SetBool(ref bool b, bool set)
        {
            b = set;
        }

        public void SetValue(ref int i, int set)
        {
            i = set;
        }
    }
}