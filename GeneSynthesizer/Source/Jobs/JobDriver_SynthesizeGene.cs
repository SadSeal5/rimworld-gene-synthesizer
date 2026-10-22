using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace SadSeal.GeneSynthesizer.Jobs
{
    public class JobDriver_SynthesizeGene : JobDriver
    {
        private const TargetIndex AssemblerIndex = TargetIndex.A;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return pawn.Reserve(job.GetTarget(AssemblerIndex), job, 1, -1, null, errorOnFailed);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedNullOrForbidden(AssemblerIndex);
            this.FailOnBurningImmobile(AssemblerIndex);
            this.FailOn(() =>
            {
                Thing building = job.GetTarget(AssemblerIndex).Thing;
                if (building == null) return true;
                CompGeneSynthesizer comp = building.TryGetComp<CompGeneSynthesizer>();
                if (comp == null || !comp.IsSynthesizing || comp.CurrentOrder == null) return true;
                if (comp.innerContainer.TotalStackCount < comp.CurrentOrder.NeutroamineCost) return true;
                CompPowerTrader power = building.TryGetComp<CompPowerTrader>();
                if (power != null && !power.PowerOn) return true;
                return false;
            });

            PathEndMode endMode = job.GetTarget(AssemblerIndex).Thing?.def.hasInteractionCell == true
                ? PathEndMode.InteractionCell
                : PathEndMode.Touch;

            yield return Toils_Goto.GotoThing(AssemblerIndex, endMode);

            Toil work = ToilMaker.MakeToil("SynthesizeGene");
            work.tickAction = () =>
            {
                Pawn actor = work.actor;
                Thing building = job.GetTarget(AssemblerIndex).Thing;
                CompGeneSynthesizer comp = building?.TryGetComp<CompGeneSynthesizer>();
                if (comp == null || !comp.IsSynthesizing || comp.CurrentOrder == null)
                {
                    actor.jobs.EndCurrentJob(JobCondition.Incompletable);
                    return;
                }

                float speed = actor.GetStatValue(StatDefOf.ResearchSpeed);
                comp.CurrentOrder.WorkDone += speed;
                actor.skills?.Learn(SkillDefOf.Intellectual, 0.05f);
                actor.GainComfortFromCellIfPossible(1);

                if (comp.CurrentOrder.WorkDone >= comp.CurrentOrder.WorkRequired)
                {
                    comp.FinishCurrentOrder();
                    actor.jobs.EndCurrentJob(JobCondition.Succeeded);
                }
            };
            work.defaultCompleteMode = ToilCompleteMode.Never;
            work.WithEffect(EffecterDefOf.GeneAssembler_Working, AssemblerIndex);
            work.WithProgressBar(AssemblerIndex, () =>
            {
                Thing building = job.GetTarget(AssemblerIndex).Thing;
                CompGeneSynthesizer comp = building?.TryGetComp<CompGeneSynthesizer>();
                if (comp == null || comp.CurrentOrder == null || comp.CurrentOrder.WorkRequired <= 0f) return 1f;
                return Mathf.Clamp01(comp.CurrentOrder.WorkDone / comp.CurrentOrder.WorkRequired);
            });

            yield return work;
        }
    }
}
