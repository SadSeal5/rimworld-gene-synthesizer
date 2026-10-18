using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;

namespace SadSeal.GeneSynthesizer.Patches
{
    [HarmonyPatch(typeof(Building_GeneAssembler), nameof(Building_GeneAssembler.PostPostMake))]
    public static class GeneAssemblerPatch
    {
        [HarmonyPrepare]
        public static bool Prepare() => ModLister.BiotechInstalled;

        [HarmonyPostfix]
        public static void Postfix(Building_GeneAssembler __instance)
        {
            EnsureCompOnInstance(__instance);
        }

        public static void EnsureCompOnInstance(Building_GeneAssembler instance)
        {
            if (instance == null) return;
            EnsureDefHasComp(instance.def);

            if (instance.GetComp<CompGeneSynthesizer>() == null)
            {
                CompGeneSynthesizer comp = new CompGeneSynthesizer
                {
                    parent = instance
                };
                comp.Initialize(new CompProperties_GeneSynthesizer());
                instance.AllComps.Add(comp);
            }
        }

        public static void EnsureDefHasComp(ThingDef def)
        {
            if (def == null) return;
            if (def.comps == null)
            {
                def.comps = new System.Collections.Generic.List<CompProperties>();
            }

            for (int i = 0; i < def.comps.Count; i++)
            {
                CompProperties c = def.comps[i];
                if (c is CompProperties_GeneSynthesizer || c?.compClass == typeof(CompGeneSynthesizer))
                {
                    return;
                }
            }

            def.comps.Add(new CompProperties_GeneSynthesizer());
        }
    }

    [HarmonyPatch(typeof(Building_GeneAssembler), nameof(Building_GeneAssembler.ExposeData))]
    public static class GeneAssemblerExposePatch
    {
        [HarmonyPrepare]
        public static bool Prepare() => ModLister.BiotechInstalled;

        [HarmonyPostfix]
        public static void Postfix(Building_GeneAssembler __instance)
        {
            if (Scribe.mode == LoadSaveMode.LoadingVars || Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                GeneAssemblerPatch.EnsureCompOnInstance(__instance);
            }
        }
    }

    [HarmonyPatch(typeof(Building_GeneAssembler), nameof(Building_GeneAssembler.GetGizmos))]
    public static class GeneAssemblerGetGizmosPatch
    {
        [HarmonyPrepare]
        public static bool Prepare() => ModLister.BiotechInstalled;

        [HarmonyPostfix]
        public static IEnumerable<Gizmo> Postfix(IEnumerable<Gizmo> __result, Building_GeneAssembler __instance)
        {
            bool hasSynthesizerGizmo = false;

            if (__result != null)
            {
                foreach (Gizmo g in __result)
                {
                    if (g is Command_Action cmd && cmd.defaultLabel == "GeneSynthesis_GizmoSynthesize".Translate())
                    {
                        hasSynthesizerGizmo = true;
                    }
                    yield return g;
                }
            }

            if (__instance == null || !__instance.Spawned) yield break;

            GeneAssemblerPatch.EnsureCompOnInstance(__instance);

            CompGeneSynthesizer comp = __instance.GetComp<CompGeneSynthesizer>();
            if (comp != null && !hasSynthesizerGizmo)
            {
                foreach (Gizmo extra in comp.CompGetGizmosExtra())
                {
                    yield return extra;
                }
            }
        }
    }

    [HarmonyPatch(typeof(Building_GeneAssembler), nameof(Building_GeneAssembler.GetInspectString))]
    public static class GeneAssemblerInspectStringPatch
    {
        [HarmonyPrepare]
        public static bool Prepare() => ModLister.BiotechInstalled;

        [HarmonyPostfix]
        public static string Postfix(string __result, Building_GeneAssembler __instance)
        {
            if (__instance == null || !__instance.Spawned) return __result;

            CompGeneSynthesizer comp = __instance.GetComp<CompGeneSynthesizer>();
            if (comp == null || !comp.IsSynthesizing || comp.TargetGene == null) return __result;

            string extra = comp.CompInspectStringExtra();
            if (string.IsNullOrEmpty(extra)) return __result;

            return string.IsNullOrEmpty(__result) ? extra : __result + "\n" + extra;
        }
    }

    [StaticConstructorOnStartup]
    public static class GeneAssemblerDefInitializer
    {
        static GeneAssemblerDefInitializer()
        {
            if (ModLister.BiotechInstalled && ThingDefOf.GeneAssembler != null)
            {
                GeneAssemblerPatch.EnsureDefHasComp(ThingDefOf.GeneAssembler);
            }
        }
    }
}
