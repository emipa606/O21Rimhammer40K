using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace Rimhammer40k.Common
{
    [StaticConstructorOnStartup]
    public class CompHelp : ThingComp
    {
        public static readonly Texture2D LaunchReportTex = ContentFinder<Texture2D>.Get("UI/Commands/LaunchReport");

        public string HelpText
        {
            get
            {
                if ($"{parent.def.defName}_HelpText".TryTranslate(out var text))
                {
                    return text;
                }

                return null;
            }
        }

        public string OrdoText
        {
            get
            {
                if ($"{parent.def.defName}_OrdoText".TryTranslate(out var text))
                {
                    return text;
                }

                return null;
            }
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            foreach (var g in base.CompGetGizmosExtra())
            {
                yield return g;
            }

            var helpText = HelpText;
            if (!string.IsNullOrEmpty(helpText))
            {
                yield return new Command_Action
                {
                    defaultLabel = "CommonHelp".Translate(),
                    defaultDesc = "CommonHelpDesc".Translate(),
                    icon = LaunchReportTex,
                    action = () =>
                    {
                        if (Find.WindowStack.WindowOfType<Dialog_MessageBox>() == null)
                        {
                            Find.WindowStack.Add(new Dialog_MessageBox(helpText));
                        }
                    }
                };
            }
        }
    }
}