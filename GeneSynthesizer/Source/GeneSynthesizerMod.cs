using HarmonyLib;
using SadSeal.GeneSynthesizer.UI;
using UnityEngine;
using Verse;

namespace SadSeal.GeneSynthesizer
{
    public class GeneSynthesizerMod : Mod
    {
        public static GeneSynthesizerMod Instance { get; private set; }
        public static GeneSynthesizerSettings Settings { get; private set; }

        public GeneSynthesizerMod(ModContentPack content) : base(content)
        {
            Instance = this;
            Settings = GetSettings<GeneSynthesizerSettings>();

            var harmony = new Harmony("sadseal.genesynthesizer");
            harmony.PatchAll();

            Log.Message("[SadSeal's Gene Synthesizer] Initialized successfully.");
        }

        public override string SettingsCategory()
        {
            return "GeneSynthesis_WindowTitle".Translate();
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            GeneSynthesizerSettingsDrawer.DrawSettings(inRect, Settings);
            base.DoSettingsWindowContents(inRect);
        }
    }
}
