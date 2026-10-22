using System.Collections.Generic;
using RimWorld;
using Verse;

namespace SadSeal.GeneSynthesizer
{
    public class GeneSynthesisOrder : IExposable
    {
        public List<GeneDef> Genes = new List<GeneDef>();
        public string Label;
        public int NeutroamineCost;
        public float WorkRequired;
        public float WorkDone;
        public bool IsCombinedPack;
        public XenotypeDef SourceXenotype;

        public GeneSynthesisOrder()
        {
        }

        public GeneSynthesisOrder(GeneDef gene, int neutroCost, float work)
        {
            if (gene != null)
            {
                Genes.Add(gene);
            }
            Label = gene?.LabelCap ?? "Unknown Gene";
            NeutroamineCost = neutroCost;
            WorkRequired = work;
            WorkDone = 0f;
            IsCombinedPack = false;
            SourceXenotype = null;
        }

        public GeneSynthesisOrder(List<GeneDef> genes, string label, int neutroCost, float work, bool isCombinedPack = false, XenotypeDef sourceXenotype = null)
        {
            if (genes != null)
            {
                Genes.AddRange(genes);
            }
            Label = label;
            NeutroamineCost = neutroCost;
            WorkRequired = work;
            WorkDone = 0f;
            IsCombinedPack = isCombinedPack;
            SourceXenotype = sourceXenotype;
        }

        public void ExposeData()
        {
            Scribe_Collections.Look(ref Genes, "genes", LookMode.Def);
            Scribe_Values.Look(ref Label, "label");
            Scribe_Values.Look(ref NeutroamineCost, "neutroamineCost", 0);
            Scribe_Values.Look(ref WorkRequired, "workRequired", 0f);
            Scribe_Values.Look(ref WorkDone, "workDone", 0f);
            Scribe_Values.Look(ref IsCombinedPack, "isCombinedPack", false);
            Scribe_Defs.Look(ref SourceXenotype, "sourceXenotype");

            if (Scribe.mode == LoadSaveMode.PostLoadInit && Genes == null)
            {
                Genes = new List<GeneDef>();
            }
        }
    }
}
