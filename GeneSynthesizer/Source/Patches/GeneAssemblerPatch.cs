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
