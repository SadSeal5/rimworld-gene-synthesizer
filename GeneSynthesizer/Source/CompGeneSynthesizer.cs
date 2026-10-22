using System.Collections.Generic;
using System.Linq;
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
        public List<GeneSynthesisOrder> OrderQueue = new List<GeneSynthesisOrder>();
        public ThingOwner innerContainer;

        public GeneSynthesisOrder CurrentOrder => OrderQueue != null && OrderQueue.Count > 0 ? OrderQueue[0] : null;
        public bool IsSynthesizing => CurrentOrder != null;

        // Compatibility properties for jobs and inspect
        public GeneDef TargetGene => CurrentOrder?.Genes?.FirstOrDefault();
        public int NeutroamineCost => CurrentOrder?.NeutroamineCost ?? 0;
        public float WorkRequired => CurrentOrder?.WorkRequired ?? 0f;
        public float WorkDone
        {
            get => CurrentOrder?.WorkDone ?? 0f;
            set
            {
                if (CurrentOrder != null)
                {
                    CurrentOrder.WorkDone = value;
                }
            }
        }

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
            if (OrderQueue == null)
            {
                OrderQueue = new List<GeneSynthesisOrder>();
            }
        }

        public void EnqueueOrder(GeneSynthesisOrder order)
        {
            if (order == null) return;
            if (OrderQueue == null)
            {
                OrderQueue = new List<GeneSynthesisOrder>();
            }
            OrderQueue.Add(order);

            // If this is the only order and it requires no work and no neutroamine (or already satisfied), process immediately
            if (OrderQueue.Count == 1 && order.WorkRequired <= 0f && (innerContainer?.TotalStackCount ?? 0) >= order.NeutroamineCost)
            {
                FinishCurrentOrder();
            }
        }

        public void EnqueueSingleGene(GeneDef gene, int neutroCost, float work)
        {
            if (gene == null) return;
            EnqueueOrder(new GeneSynthesisOrder(gene, neutroCost, work));
        }

        public void EnqueueBatchGenes(IEnumerable<GeneDef> genes, int neutroCostPerGene, float workPerGene)
        {
            if (genes == null) return;
            foreach (GeneDef gene in genes)
            {
                if (gene != null)
                {
                    EnqueueSingleGene(gene, neutroCostPerGene, workPerGene);
                }
            }
        }

        public void EnqueueCombinedXenotype(XenotypeDef xenotype, int totalCost, float totalWork)
        {
            if (xenotype == null || xenotype.genes == null || xenotype.genes.Count == 0) return;
            List<GeneDef> validGenes = xenotype.genes.Where(g => g != null).ToList();
            if (validGenes.Count == 0) return;

            string label = $"{xenotype.LabelCap} ({"GeneSynthesis_CombinedPack".Translate()})";
            GeneSynthesisOrder order = new GeneSynthesisOrder(validGenes, label, totalCost, totalWork, isCombinedPack: true, sourceXenotype: xenotype);
            EnqueueOrder(order);
        }

        public void RemoveOrderAt(int index)
        {
            if (OrderQueue == null || index < 0 || index >= OrderQueue.Count) return;

            if (index == 0)
            {
                CancelCurrentOrder();
            }
            else
            {
                OrderQueue.RemoveAt(index);
            }
        }

        public void ClearQueue()
        {
            CancelAllOrders();
        }

        // Backward compatibility
        public void StartSynthesis(GeneDef gene, int neutroCost, float work)
        {
            EnqueueSingleGene(gene, neutroCost, work);
        }

        public void CancelSynthesis()
        {
            CancelCurrentOrder();
        }

        public void FinishSynthesis()
        {
            FinishCurrentOrder();
        }

        public void CancelCurrentOrder()
        {
            if (innerContainer != null && innerContainer.Count > 0 && parent.Spawned && parent.Map != null)
            {
                innerContainer.TryDropAll(parent.Position, parent.Map, ThingPlaceMode.Near);
            }
            innerContainer?.Clear();

            if (OrderQueue != null && OrderQueue.Count > 0)
            {
                OrderQueue.RemoveAt(0);
            }
        }

        public void CancelAllOrders()
        {
            if (innerContainer != null && innerContainer.Count > 0 && parent.Spawned && parent.Map != null)
            {
                innerContainer.TryDropAll(parent.Position, parent.Map, ThingPlaceMode.Near);
            }
            innerContainer?.Clear();

            OrderQueue?.Clear();
        }

        public void FinishCurrentOrder()
        {
            if (CurrentOrder == null)
            {
                return;
            }

            GeneSynthesisOrder finished = CurrentOrder;
            Map map = parent.Map;
            IntVec3 pos = parent.Position;

            if (finished.Genes != null && finished.Genes.Count > 0)
            {
                Genepack pack = (Genepack)ThingMaker.MakeThing(ThingDefOf.Genepack);
                pack.Initialize(finished.Genes);
                if (map != null && parent.Spawned)
                {
                    GenPlace.TryPlaceThing(pack, pos, map, ThingPlaceMode.Near);
                }

                SoundDefOf.GeneAssembler_Complete?.PlayOneShot(SoundInfo.InMap(parent));

                if (finished.IsCombinedPack && finished.SourceXenotype != null)
                {
                    Messages.Message("GeneSynthesis_XenotypeFinished".Translate(finished.SourceXenotype.LabelCap), pack, MessageTypeDefOf.PositiveEvent);
                }
                else
                {
                    Messages.Message("GeneSynthesis_Finished".Translate(finished.Label), pack, MessageTypeDefOf.PositiveEvent);
                }
            }

            // Consume Neutroamine
            int toConsume = finished.NeutroamineCost;
            if (innerContainer != null && toConsume > 0)
            {
                int remaining = toConsume;
                for (int i = innerContainer.Count - 1; i >= 0 && remaining > 0; i--)
                {
                    Thing item = innerContainer[i];
                    int count = Mathf.Min(item.stackCount, remaining);
                    item.SplitOff(count).Destroy();
                    remaining -= count;
                }
            }

            if (OrderQueue != null && OrderQueue.Count > 0)
            {
                OrderQueue.RemoveAt(0);
            }

            // If the next order is instant craft and has enough neutroamine, finish it too
            while (CurrentOrder != null && CurrentOrder.WorkRequired <= 0f && (innerContainer?.TotalStackCount ?? 0) >= CurrentOrder.NeutroamineCost)
            {
                GeneSynthesisOrder nextOrder = CurrentOrder;
                if (nextOrder.Genes != null && nextOrder.Genes.Count > 0)
                {
                    Genepack pack = (Genepack)ThingMaker.MakeThing(ThingDefOf.Genepack);
                    pack.Initialize(nextOrder.Genes);
                    if (map != null && parent.Spawned)
                    {
                        GenPlace.TryPlaceThing(pack, pos, map, ThingPlaceMode.Near);
                    }

                    SoundDefOf.GeneAssembler_Complete?.PlayOneShot(SoundInfo.InMap(parent));

                    if (nextOrder.IsCombinedPack && nextOrder.SourceXenotype != null)
                    {
                        Messages.Message("GeneSynthesis_XenotypeFinished".Translate(nextOrder.SourceXenotype.LabelCap), pack, MessageTypeDefOf.PositiveEvent);
                    }
                    else
                    {
                        Messages.Message("GeneSynthesis_Finished".Translate(nextOrder.Label), pack, MessageTypeDefOf.PositiveEvent);
                    }
                }

                int nextConsume = nextOrder.NeutroamineCost;
                if (innerContainer != null && nextConsume > 0)
                {
                    int remaining = nextConsume;
                    for (int i = innerContainer.Count - 1; i >= 0 && remaining > 0; i--)
                    {
                        Thing item = innerContainer[i];
                        int count = Mathf.Min(item.stackCount, remaining);
                        item.SplitOff(count).Destroy();
                        remaining -= count;
                    }
                }

                if (OrderQueue != null && OrderQueue.Count > 0)
                {
                    OrderQueue.RemoveAt(0);
                }
                else
                {
                    break;
                }
            }
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            foreach (Gizmo g in base.CompGetGizmosExtra())
            {
                yield return g;
            }

            // Always allow opening the synthesizer dialog so players can queue additional genes or presets
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

            if (IsSynthesizing)
            {
                yield return new Command_Action
                {
                    defaultLabel = "GeneSynthesis_GizmoCancel".Translate(),
                    defaultDesc = "GeneSynthesis_GizmoCancelDesc".Translate(),
                    icon = ContentFinder<Texture2D>.Get("UI/Designators/Cancel", true),
                    action = CancelCurrentOrder
                };

                if (OrderQueue.Count > 1)
                {
                    yield return new Command_Action
                    {
                        defaultLabel = "GeneSynthesis_GizmoClearAll".Translate(),
                        defaultDesc = "GeneSynthesis_GizmoClearAllDesc".Translate(),
                        icon = ContentFinder<Texture2D>.Get("UI/Commands/ForbidOff", false) ?? ContentFinder<Texture2D>.Get("UI/Designators/Cancel", true),
                        action = CancelAllOrders
                    };
                }

                if (Prefs.DevMode)
                {
                    yield return new Command_Action
                    {
                        defaultLabel = "GeneSynthesis_GizmoDevFinish".Translate(),
                        defaultDesc = "GeneSynthesis_GizmoDevFinishDesc".Translate(),
                        icon = TexCommand.DesirePower,
                        action = FinishCurrentOrder
                    };
                }
            }
        }

        public override string CompInspectStringExtra()
        {
            if (!IsSynthesizing || CurrentOrder == null)
            {
                return null;
            }

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("GeneSynthesis_TargetGene".Translate(CurrentOrder.Label));

            if (OrderQueue.Count > 1)
            {
                sb.AppendLine("GeneSynthesis_QueueProgress".Translate(1, OrderQueue.Count));
            }

            sb.AppendLine("GeneSynthesis_NeutroamineProgress".Translate(innerContainer?.TotalStackCount ?? 0, CurrentOrder.NeutroamineCost));
            float pct = CurrentOrder.WorkRequired > 0f ? (CurrentOrder.WorkDone / CurrentOrder.WorkRequired) : 1f;
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
            Scribe_Collections.Look(ref OrderQueue, "orderQueue", LookMode.Deep);
            Scribe_Deep.Look(ref innerContainer, "innerContainer", this);

            // Backward compatibility loading from older save files
            if (Scribe.mode == LoadSaveMode.LoadingVars)
            {
                GeneDef legacyGene = null;
                Scribe_Defs.Look(ref legacyGene, "targetGene");
                if (legacyGene != null)
                {
                    int legacyNeutro = 0;
                    float legacyDone = 0f;
                    float legacyReq = 0f;
                    bool legacySynth = false;
                    Scribe_Values.Look(ref legacyNeutro, "neutroamineCost", 0);
                    Scribe_Values.Look(ref legacyDone, "workDone", 0f);
                    Scribe_Values.Look(ref legacyReq, "workRequired", 0f);
                    Scribe_Values.Look(ref legacySynth, "isSynthesizing", false);

                    if (legacySynth)
                    {
                        if (OrderQueue == null)
                        {
                            OrderQueue = new List<GeneSynthesisOrder>();
                        }
                        OrderQueue.Add(new GeneSynthesisOrder(legacyGene, legacyNeutro, legacyReq) { WorkDone = legacyDone });
                    }
                }
            }

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (innerContainer == null)
                {
                    innerContainer = new ThingOwner<Thing>(this);
                }
                if (OrderQueue == null)
                {
                    OrderQueue = new List<GeneSynthesisOrder>();
                }
                OrderQueue.RemoveAll(o => o == null);
            }
        }
    }
}
