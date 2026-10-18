using Verse;

namespace SadSeal.GeneSynthesizer
{
    public class GeneSynthesizerSettings : ModSettings
    {
        public int NeutroamineCost = 100;
        public float WorkAmount = 2500f;
        public bool InstantCraft = false;
        public bool RequireArchiteCapsules = false;
        public bool AllowArchiteGenes = true;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref NeutroamineCost, "NeutroamineCost", 100);
            Scribe_Values.Look(ref WorkAmount, "WorkAmount", 2500f);
            Scribe_Values.Look(ref InstantCraft, "InstantCraft", false);
            Scribe_Values.Look(ref RequireArchiteCapsules, "RequireArchiteCapsules", false);
            Scribe_Values.Look(ref AllowArchiteGenes, "AllowArchiteGenes", true);
        }

        public void ResetToDefaults()
        {
            NeutroamineCost = 100;
            WorkAmount = 2500f;
            InstantCraft = false;
            RequireArchiteCapsules = false;
            AllowArchiteGenes = true;
        }
    }
}
