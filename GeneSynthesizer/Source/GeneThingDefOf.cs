using RimWorld;
using Verse;

namespace SadSeal.GeneSynthesizer
{
    [DefOf]
    public static class GeneThingDefOf
    {
        public static ThingDef Neutroamine;
        [MayRequireBiotech]
        public static ThingDef ArchiteCapsule;

        static GeneThingDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(GeneThingDefOf));
        }
    }
}
