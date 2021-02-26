using System.Collections.Generic;
using RimWorld;
using Verse;

namespace O21Toolbox.PawnCrafter
{
    /// <summary>
    ///     Sets skills and what not upon spawn of the Droid to this.
    /// </summary>
    public class DroidSpawnProperties : DefModExtension
    {
        /// <summary>
        ///     Backstory to use.
        /// </summary>
        public BackstoryCategoryFilter backstoryFilter;

        /// <summary>
        ///     Body to use.
        /// </summary>
        public BodyTypeDef bodyType;

        /// <summary>
        ///     Default skill level for skills not listed.
        /// </summary>
        public int defaultSkillLevel = 0;

        /// <summary>
        ///     Gender to set when spawning.
        /// </summary>
        public Gender gender = Gender.Male;

        /// <summary>
        ///     If true it will generate hair in the normal way.
        /// </summary>
        public bool generateHair = false;

        /// <summary>
        ///     Hair tags to look for.
        /// </summary>
        public List<string> hairTags = new List<string>();

        /// <summary>
        ///     The initial hostile response set upon spawning.
        /// </summary>
        public HostilityResponseMode hostileResponse = HostilityResponseMode.Flee;

        /// <summary>
        ///     Skills to be assigned.
        /// </summary>
        public List<DroidSkill> skills = new List<DroidSkill>();
    }
}