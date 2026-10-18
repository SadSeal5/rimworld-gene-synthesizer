using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace SadSeal.GeneSynthesizer.Jobs
{
    public class WorkGiver_SynthesizeGene : WorkGiver_Scanner
    {
        public override ThingRequest PotentialWorkThingRequest
        {
            get
            {
                if (ThingDefOf.GeneAssembler != null)
                {
                    return ThingRequest.ForDef(ThingDefOf.GeneAssembler);
                }
                return ThingRequest.ForGroup(ThingRequestGroup.BuildingArtificial);
            }
        }

        public override PathEndMode PathEndMode => PathEndMode.Touch;

        public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            if (t.Faction != pawn.Faction && t.Faction != Faction.OfPlayer)
            {
                return false;
            }

            CompGeneSynthesizer comp = t.TryGetComp<CompGeneSynthesizer>();
            if (comp == null || !comp.IsSynthesizing || comp.TargetGene == null)
            {
                return false;
            }

            CompPowerTrader power = t.TryGetComp<CompPowerTrader>();
            if (power != null && !power.PowerOn)
            {
                JobFailReason.Is("NoPower".Translate());
                return false;
            }

            if (!pawn.CanReserve(t, 1, -1, null, forced))
            {
                return false;
            }

            if (comp.innerContainer.TotalStackCount < comp.NeutroamineCost)
            {
                Thing neutro = FindNeutroamine(pawn);
                if (neutro == null)
                {
                    JobFailReason.Is("GeneSynthesis_NoNeutroamine".Translate());
                    return false;
                }
                if (!pawn.CanReserve(neutro, 10, 1, null, forced))
                {
                    return false;
                }
                return true;
            }

            return true;
        }

        public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            CompGeneSynthesizer comp = t.TryGetComp<CompGeneSynthesizer>();
            if (comp == null || !comp.IsSynthesizing || comp.TargetGene == null)
            {
                return null;
            }

            if (comp.innerContainer.TotalStackCount < comp.NeutroamineCost)
            {
                Thing neutro = FindNeutroamine(pawn);
                if (neutro == null)
                {
                    return null;
                }

                int needed = comp.NeutroamineCost - comp.innerContainer.TotalStackCount;
                Job haulJob = JobMaker.MakeJob(GeneJobDefOf.HaulNeutroamineToGeneAssembler, t, neutro);
                haulJob.count = Mathf.Min(needed, neutro.stackCount);
                return haulJob;
            }

            return JobMaker.MakeJob(GeneJobDefOf.SynthesizeGene, t);
        }

        private Thing FindNeutroamine(Pawn pawn)
        {
            ThingDef neutroDef = GeneThingDefOf.Neutroamine ?? DefDatabase<ThingDef>.GetNamed("Neutroamine", false);
            if (neutroDef == null)
            {
                return null;
            }

            return GenClosest.ClosestThingReachable(
                pawn.Position,
                pawn.Map,
                ThingRequest.ForDef(neutroDef),
                PathEndMode.ClosestTouch,
                TraverseParms.For(pawn),
                9999f,
                item => !item.IsForbidden(pawn) && pawn.CanReserve(item)
            );
        }
    }
}
