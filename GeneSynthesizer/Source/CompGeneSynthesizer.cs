using System.Collections.Generic;
using System.Text;
using RimWorld;
using SadSeal.GeneSynthesizer.UI;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace SadSeal.GeneSynthesizer
{
    public class CompGeneSynthesizer : ThingComp, IThingHolder
    {
        public GeneDef TargetGene;
        public int NeutroamineCost;
        public float WorkDone;
        public float WorkRequired;
        public bool IsSynthesizing;
        public ThingOwner innerContainer;

        public CompGeneSynthesizer()
        {
            innerContainer = new ThingOwner<Thing>(this);
        }

        public ThingOwner GetDirectlyHeldThings() => innerContainer;

        public void GetChildHolders(List<IThingHolder> outChildren)
        {
            ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, GetDirectlyHeldThings());
        }

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            if (innerContainer == null)
            {
                innerContainer = new ThingOwner<Thing>(this);
            }
        }

        public void StartSynthesis(GeneDef gene, int neutroCost, float work)
        {
            TargetGene = gene;
            NeutroamineCost = neutroCost;
            WorkRequired = work;
            WorkDone = 0f;
            IsSynthesizing = true;

            if (neutroCost <= 0 && work <= 0f)
            {
                FinishSynthesis();
            }
        }

        public void CancelSynthesis()
        {
            if (innerContainer != null && innerContainer.Count > 0 && parent.Spawned && parent.Map != null)
            {
                innerContainer.TryDropAll(parent.Position, parent.Map, ThingPlaceMode.Near);
            }
            innerContainer?.Clear();
            TargetGene = null;
            IsSynthesizing = false;
            WorkDone = 0f;
            WorkRequired = 0f;
            NeutroamineCost = 0;
        }

        public void FinishSynthesis()
        {
            if (TargetGene == null)
            {
                IsSynthesizing = false;
                return;
            }

            Map map = parent.Map;
            IntVec3 pos = parent.Position;

            Genepack pack = (Genepack)ThingMaker.MakeThing(ThingDefOf.Genepack);
            pack.Initialize(new List<GeneDef> { TargetGene });
            if (map != null)
            {
                GenPlace.TryPlaceThing(pack, pos, map, ThingPlaceMode.Near);
            }

            innerContainer?.ClearAndDestroyContents();
            SoundDefOf.GeneAssembler_Complete?.PlayOneShot(SoundInfo.InMap(parent));
            Messages.Message("GeneSynthesis_Finished".Translate(TargetGene.label), pack, MessageTypeDefOf.PositiveEvent);

            TargetGene = null;
            IsSynthesizing = false;
            WorkDone = 0f;
            WorkRequired = 0f;
            NeutroamineCost = 0;
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            foreach (Gizmo g in base.CompGetGizmosExtra())
            {
                yield return g;
            }

            if (!IsSynthesizing)
            {
                yield return new Command_Action
                {
                    defaultLabel = "GeneSynthesis_GizmoSynthesize".Translate(),
                    defaultDesc = "GeneSynthesis_GizmoSynthesizeDesc".Translate(),
                    icon = ContentFinder<Texture2D>.Get("UI/Gizmos/RecombineGenes", false) ?? TexCommand.DesirePower,
                    action = () =>
                    {
                        Find.WindowStack.Add(new Dialog_GeneSynthesizer(this));
                    }
                };
            }
            else
            {
                yield return new Command_Action
                {
                    defaultLabel = "GeneSynthesis_GizmoCancel".Translate(),
                    defaultDesc = "GeneSynthesis_GizmoCancelDesc".Translate(),
                    icon = ContentFinder<Texture2D>.Get("UI/Designators/Cancel", true),
                    action = CancelSynthesis
                };

                if (Prefs.DevMode)
                {
                    yield return new Command_Action
                    {
                        defaultLabel = "GeneSynthesis_GizmoDevFinish".Translate(),
                        defaultDesc = "GeneSynthesis_GizmoDevFinishDesc".Translate(),
                        icon = TexCommand.DesirePower,
                        action = FinishSynthesis
                    };
                }
            }
        }

        public override string CompInspectStringExtra()
        {
            if (!IsSynthesizing || TargetGene == null)
            {
                return null;
            }

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("GeneSynthesis_TargetGene".Translate(TargetGene.LabelCap));
            sb.AppendLine("GeneSynthesis_NeutroamineProgress".Translate(innerContainer?.TotalStackCount ?? 0, NeutroamineCost));
            float pct = WorkRequired > 0f ? (WorkDone / WorkRequired) : 1f;
            sb.Append("GeneSynthesis_WorkProgress".Translate(pct.ToStringPercent()));
            return sb.ToString().TrimEndNewlines();
        }

        public override void PostDestroy(DestroyMode mode, Map previousMap)
        {
            base.PostDestroy(mode, previousMap);
            if (innerContainer != null && innerContainer.Count > 0 && previousMap != null)
            {
                innerContainer.TryDropAll(parent.Position, previousMap, ThingPlaceMode.Near);
            }
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Defs.Look(ref TargetGene, "targetGene");
            Scribe_Values.Look(ref NeutroamineCost, "neutroamineCost", 0);
            Scribe_Values.Look(ref WorkDone, "workDone", 0f);
            Scribe_Values.Look(ref WorkRequired, "workRequired", 0f);
            Scribe_Values.Look(ref IsSynthesizing, "isSynthesizing", false);
            Scribe_Deep.Look(ref innerContainer, "innerContainer", this);

            if (Scribe.mode == LoadSaveMode.PostLoadInit && innerContainer == null)
            {
                innerContainer = new ThingOwner<Thing>(this);
            }
        }
    }
}
