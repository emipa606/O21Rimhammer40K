using RimWorld;
using Verse;

namespace Rimhammer40k.Logic
{
    public class ExposedTraitEntry : TraitEntry, IExposable
    {
        public ExposedTraitEntry()
        {
        }

        public ExposedTraitEntry(Trait trait)
        {
            def = trait.def;
            degree = trait.Degree;
        }

        public ExposedTraitEntry(ExposedTraitEntry traitEntry)
        {
            def = traitEntry.def;
            degree = traitEntry.degree;
        }

        public void ExposeData()
        {
            Scribe_Defs.Look(ref def, "def");
            Scribe_Values.Look(ref degree, "degree");
        }

        public override bool Equals(object obj)
        {
            if (this == obj)
            {
                return true;
            }

            if (!(obj is ExposedTraitEntry other))
            {
                return false;
            }

            if (def != other.def ||
                degree != other.degree)
            {
                return false;
            }

            return true;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode() + (degree.GetHashCode() & 0xFFF0000) + ((def?.GetHashCode() ?? 0) & 0x000FFFF);
        }
    }
}