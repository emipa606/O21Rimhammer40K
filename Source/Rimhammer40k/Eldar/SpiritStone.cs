using System.Collections.Generic;
using Rimhammer40k.Logic;
using RimWorld;
using Verse;

namespace Rimhammer40k.Eldar
{
    public class SpiritStone : ThingWithComps
    {
        public Backstory backstoryAdult;
        public Backstory backstoryChild;
        public Faction faction;
        public Gender gender = Gender.None;

        public List<SkillRecord> skills = new List<SkillRecord>();

        // Relevant Information
        public string sourceName;
        public List<ExposedTraitEntry> traits = new List<ExposedTraitEntry>();

        public override void ExposeData()
        {
            base.ExposeData();

            Scribe_Values.Look(ref sourceName, "sourceName");

            var childhoodIdentifier = backstoryChild == null ? null : backstoryChild.identifier;
            Scribe_Values.Look(ref childhoodIdentifier, "backstoryChild");
            if (Scribe.mode == LoadSaveMode.LoadingVars && !childhoodIdentifier.NullOrEmpty())
            {
                if (!BackstoryDatabase.TryGetWithIdentifier(childhoodIdentifier, out backstoryChild))
                {
                    Log.Error("Couldn't load child backstory with identifier " + childhoodIdentifier +
                              ". Giving random.");
                    backstoryChild = BackstoryDatabase.RandomBackstory(BackstorySlot.Childhood);
                }
            }

            var adulthoodIdentifier = backstoryAdult == null ? null : backstoryAdult.identifier;
            Scribe_Values.Look(ref adulthoodIdentifier, "backStoryAdult");
            if (Scribe.mode == LoadSaveMode.LoadingVars && !adulthoodIdentifier.NullOrEmpty())
            {
                if (!BackstoryDatabase.TryGetWithIdentifier(adulthoodIdentifier, out backstoryAdult))
                {
                    Log.Error("Couldn't load adult backstory with identifier " + adulthoodIdentifier +
                              ". Giving random.");
                    backstoryAdult = BackstoryDatabase.RandomBackstory(BackstorySlot.Adulthood);
                }
            }

            Scribe_Collections.Look(ref skills, "skills", LookMode.Deep);
        }
    }
}