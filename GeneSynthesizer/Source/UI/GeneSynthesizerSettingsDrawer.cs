using System;
using System.Globalization;
using UnityEngine;
using Verse;

namespace SadSeal.GeneSynthesizer.UI
{
    public static class GeneSynthesizerSettingsDrawer
    {
        private static Vector2 scrollPosition = Vector2.zero;
        private static string costBuffer = null;
        private static string workBuffer = null;

        public static void ClearBuffers()
        {
            costBuffer = null;
            workBuffer = null;
        }

        public static void DrawSettings(Rect inRect, GeneSynthesizerSettings settings)
        {
            Rect outRect = new Rect(inRect.x, inRect.y, inRect.width, inRect.height);
            Rect viewRect = new Rect(0f, 0f, inRect.width - 24f, 520f);

            Widgets.BeginScrollView(outRect, ref scrollPosition, viewRect);
            Listing_Standard listing = new Listing_Standard();
            listing.Begin(viewRect);

            // Title & Description
            Text.Font = GameFont.Medium;
            listing.Label("GeneSynthesis_SettingsHeader".Translate());
            Text.Font = GameFont.Small;
            listing.Label("GeneSynthesis_SettingsDesc".Translate());
            listing.Gap(10f);

            // Presets Section
            Text.Font = GameFont.Medium;
            listing.Label("GeneSynthesis_Presets".Translate());
            Text.Font = GameFont.Small;
            listing.Gap(4f);

            Rect presetRowRect = listing.GetRect(32f);
            float buttonSpacing = 8f;
            float buttonWidth = (presetRowRect.width - (buttonSpacing * 4)) / 5f;

            // Free Preset
            Rect freeRect = new Rect(presetRowRect.x, presetRowRect.y, buttonWidth, 32f);
            if (Widgets.ButtonText(freeRect, "GeneSynthesis_PresetFree".Translate()))
            {
                settings.NeutroamineCost = 0;
                settings.WorkAmount = 0f;
                settings.InstantCraft = true;
                ClearBuffers();
            }

            // Cheap Preset
            Rect cheapRect = new Rect(presetRowRect.x + buttonWidth + buttonSpacing, presetRowRect.y, buttonWidth, 32f);
            if (Widgets.ButtonText(cheapRect, "GeneSynthesis_PresetCheap".Translate()))
            {
                settings.NeutroamineCost = 25;
                settings.WorkAmount = 1000f;
                settings.InstantCraft = false;
                ClearBuffers();
            }

            // Standard Preset
            Rect stdRect = new Rect(presetRowRect.x + (buttonWidth + buttonSpacing) * 2f, presetRowRect.y, buttonWidth, 32f);
            if (Widgets.ButtonText(stdRect, "GeneSynthesis_PresetStandard".Translate()))
            {
                settings.NeutroamineCost = 100;
                settings.WorkAmount = 2500f;
                settings.InstantCraft = false;
                ClearBuffers();
            }

            // Hardcore Preset
            Rect hcRect = new Rect(presetRowRect.x + (buttonWidth + buttonSpacing) * 3f, presetRowRect.y, buttonWidth, 32f);
            if (Widgets.ButtonText(hcRect, "GeneSynthesis_PresetHardcore".Translate()))
            {
                settings.NeutroamineCost = 250;
                settings.WorkAmount = 5000f;
                settings.InstantCraft = false;
                ClearBuffers();
            }

            // Reset Button
            Rect resetRect = new Rect(presetRowRect.x + (buttonWidth + buttonSpacing) * 4f, presetRowRect.y, buttonWidth, 32f);
            if (Widgets.ButtonText(resetRect, "GeneSynthesis_ResetDefaults".Translate()))
            {
                settings.ResetToDefaults();
                ClearBuffers();
            }

            listing.Gap(16f);

            // Cost & Work Config Section
            Text.Font = GameFont.Medium;
            listing.Label("GeneSynthesis_ResourceAndWork".Translate());
            Text.Font = GameFont.Small;
            listing.Gap(6f);

            // Neutroamine Cost Slider & Text
            Rect costRowRect = listing.GetRect(32f);
            DrawIntRow(costRowRect, "GeneSynthesis_NeutroamineCostLabel".Translate(), settings.NeutroamineCost, 0, 1000, ref costBuffer, newVal =>
            {
                settings.NeutroamineCost = newVal;
            });
            listing.Gap(4f);

            // Work Amount Slider & Text
            Rect workRowRect = listing.GetRect(32f);
            string workLabel = settings.InstantCraft || settings.WorkAmount <= 0f
                ? "GeneSynthesis_WorkAmountLabel".Translate() + " (" + "GeneSynthesis_WorkInstantShort".Translate() + ")"
                : "GeneSynthesis_WorkAmountLabel".Translate() + $" ({settings.WorkAmount:F0} ticks / ~{settings.WorkAmount / 60f:F0}s)";

            DrawFloatRow(workRowRect, workLabel, settings.WorkAmount, 0f, 10000f, ref workBuffer, newVal =>
            {
                settings.WorkAmount = newVal;
            });
            listing.Gap(12f);

            // Toggles
            Text.Font = GameFont.Medium;
            listing.Label("GeneSynthesis_RulesAndToggles".Translate());
            Text.Font = GameFont.Small;
            listing.Gap(6f);

            listing.CheckboxLabeled(
                "GeneSynthesis_InstantCraft".Translate(),
                ref settings.InstantCraft,
                "GeneSynthesis_InstantCraft_Desc".Translate()
            );

            listing.CheckboxLabeled(
                "GeneSynthesis_AllowArchite".Translate(),
                ref settings.AllowArchiteGenes,
                "GeneSynthesis_AllowArchite_Desc".Translate()
            );

            listing.CheckboxLabeled(
                "GeneSynthesis_RequireArchiteCapsules".Translate(),
                ref settings.RequireArchiteCapsules,
                "GeneSynthesis_RequireArchiteCapsules_Desc".Translate()
            );

            listing.End();
            Widgets.EndScrollView();
        }

        private static void DrawIntRow(Rect rowRect, string label, int currentValue, int min, int max, ref string buffer, Action<int> onChanged)
        {
            float labelWidth = 240f;
            float textWidth = 70f;
            float sliderWidth = rowRect.width - labelWidth - textWidth - 20f;

            Rect labelRect = new Rect(rowRect.x, rowRect.y, labelWidth, rowRect.height);
            Widgets.Label(labelRect, label);

            Rect sliderRect = new Rect(rowRect.x + labelWidth + 5f, rowRect.y + 4f, sliderWidth, rowRect.height - 8f);
            int newSliderValue = Mathf.RoundToInt(Widgets.HorizontalSlider(sliderRect, currentValue, min, max, false, null, min.ToString(), max.ToString(), 1f));

            if (newSliderValue != currentValue)
            {
                buffer = newSliderValue.ToString();
                onChanged(newSliderValue);
            }

            Rect textRect = new Rect(rowRect.x + labelWidth + sliderWidth + 15f, rowRect.y + 2f, textWidth, 26f);
            if (buffer == null)
            {
                buffer = currentValue.ToString();
            }

            string newText = Widgets.TextField(textRect, buffer);
            if (newText != buffer)
            {
                buffer = newText;
                if (int.TryParse(newText, out int parsed))
                {
                    int clamped = Mathf.Clamp(parsed, min, max);
                    onChanged(clamped);
                }
            }
        }

        private static void DrawFloatRow(Rect rowRect, string label, float currentValue, float min, float max, ref string buffer, Action<float> onChanged)
        {
            float labelWidth = 240f;
            float textWidth = 70f;
            float sliderWidth = rowRect.width - labelWidth - textWidth - 20f;

            Rect labelRect = new Rect(rowRect.x, rowRect.y, labelWidth, rowRect.height);
            Widgets.Label(labelRect, label);

            Rect sliderRect = new Rect(rowRect.x + labelWidth + 5f, rowRect.y + 4f, sliderWidth, rowRect.height - 8f);
            float newSliderValue = Widgets.HorizontalSlider(sliderRect, currentValue, min, max, false, null, min.ToString("F0"), max.ToString("F0"), 50f);

            if (Math.Abs(newSliderValue - currentValue) > 1f)
            {
                float rounded = Mathf.Round(newSliderValue);
                buffer = rounded.ToString("F0", CultureInfo.InvariantCulture);
                onChanged(rounded);
            }

            Rect textRect = new Rect(rowRect.x + labelWidth + sliderWidth + 15f, rowRect.y + 2f, textWidth, 26f);
            if (buffer == null)
            {
                buffer = currentValue.ToString("F0", CultureInfo.InvariantCulture);
            }

            string newText = Widgets.TextField(textRect, buffer);
            if (newText != buffer)
            {
                buffer = newText;
                if (float.TryParse(newText, NumberStyles.Float, CultureInfo.InvariantCulture, out float parsed) ||
                    float.TryParse(newText, NumberStyles.Float, CultureInfo.CurrentCulture, out parsed))
                {
                    float clamped = Mathf.Clamp(parsed, min, max);
                    onChanged(clamped);
                }
            }
        }
    }
}
