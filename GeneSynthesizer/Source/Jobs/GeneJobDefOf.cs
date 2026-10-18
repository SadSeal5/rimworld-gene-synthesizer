using RimWorld;
using Verse;

namespace SadSeal.GeneSynthesizer.Jobs
{
    [DefOf]
    public static class GeneJobDefOf
    {
        [MayRequireBiotech]
        public static JobDef HaulNeutroamineToGeneAssembler;

        [MayRequireBiotech]
        public static JobDef SynthesizeGene;

        static GeneJobDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(GeneJobDefOf));
        }
    }
}
