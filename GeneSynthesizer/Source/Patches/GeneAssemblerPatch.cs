using HarmonyLib;
using RimWorld;
using Verse;

namespace SadSeal.GeneSynthesizer.Patches
{
    [HarmonyPatch(typeof(Building_GeneAssembler), nameof(Building_GeneAssembler.SpawnSetup))]
    public static class GeneAssemblerPatch
    {
        [HarmonyPostfix]
        public static void Postfix(Building_GeneAssembler __instance)
        {
            if (__instance == null) return;

            EnsureDefHasComp(__instance.def);

            if (__instance.GetComp<CompGeneSynthesizer>() == null)
            {
                CompGeneSynthesizer comp = new CompGeneSynthesizer
                {
                    parent = __instance
                };
                comp.Initialize(new CompProperties_GeneSynthesizer());
                __instance.AllComps.Add(comp);
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
