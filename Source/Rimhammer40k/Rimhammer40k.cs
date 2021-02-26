using UnityEngine;
using Verse;

namespace Rimhammer40k
{
    [StaticConstructorOnStartup]
    public class Rimhammer40kMod : Mod
    {
        public static int maxOrkPopulation;

        public static int maxGrotPopulation;

        public static bool necronsTeleportOnDeath;

        public Rimhammer40kMod(ModContentPack content) : base(content)
        {
            GetSettings<Settings>();
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            base.DoSettingsWindowContents(inRect);
            Settings.DoSettingsWindowContents(inRect);
        }

        public override string SettingsCategory()
        {
            return "Rimhammer 40k";
        }
    }
}