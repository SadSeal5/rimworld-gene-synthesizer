using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace SadSeal.GeneSynthesizer.UI
{
    public class Dialog_GeneSynthesizer : Window
    {
        private readonly CompGeneSynthesizer comp;
        private readonly GeneSynthesizerSettings settings;

        private List<GeneDef> allGenes = new List<GeneDef>();
        private List<GeneDef> filteredGenes = new List<GeneDef>();
        private GeneDef selectedGene;

        private string searchText = "";
        private string selectedModFilter = "All";
        private GeneCategoryDef selectedCategoryFilter;

        private Vector2 listScrollPosition = Vector2.zero;
        private Vector2 descScrollPosition = Vector2.zero;

        private List<string> availableMods = new List<string>();
        private List<GeneCategoryDef> availableCategories = new List<GeneCategoryDef>();

        public override Vector2 InitialSize => new Vector2(980f, 700f);

        public Dialog_GeneSynthesizer(CompGeneSynthesizer comp)
        {
            this.comp = comp;
            this.settings = GeneSynthesizerMod.Settings ?? new GeneSynthesizerSettings();
            forcePause = true;
            doCloseX = true;
            closeOnClickedOutside = true;
            absorbInputAroundWindow = true;
        }

        public override void PreOpen()
        {
            base.PreOpen();
            allGenes = DefDatabase<GeneDef>.AllDefsListForReading
                .Where(g => g != null && !g.label.NullOrEmpty())
                .OrderBy(g => g.label)
                .ToList();

            availableMods = allGenes
                .Select(g => g.modContentPack?.Name ?? "Vanilla")
                .Distinct()
                .OrderBy(m => m)
                .ToList();
            availableMods.Insert(0, "All");

            availableCategories = allGenes
                .Select(g => g.displayCategory)
                .Where(c => c != null)
                .Distinct()
                .OrderBy(c => c.label)
                .ToList();

            ApplyFilter();
            if (filteredGenes.Count > 0)
            {
                selectedGene = filteredGenes[0];
            }
        }

        private void ApplyFilter()
        {
            IEnumerable<GeneDef> query = allGenes;

            if (!settings.AllowArchiteGenes)
            {
                query = query.Where(g => g.biostatArc <= 0);
            }

            if (selectedModFilter != "All")
            {
                query = query.Where(g => (g.modContentPack?.Name ?? "Vanilla") == selectedModFilter);
            }

            if (selectedCategoryFilter != null)
            {
                query = query.Where(g => g.displayCategory == selectedCategoryFilter);
            }

            if (!searchText.NullOrEmpty())
            {
                string searchLower = searchText.Trim().ToLower();
                query = query.Where(g =>
                    (!g.label.NullOrEmpty() && g.label.ToLower().Contains(searchLower)) ||
                    (!g.defName.NullOrEmpty() && g.defName.ToLower().Contains(searchLower)) ||
                    (!g.description.NullOrEmpty() && g.description.ToLower().Contains(searchLower)));
            }

            filteredGenes = query.ToList();
            if (selectedGene != null && !filteredGenes.Contains(selectedGene))
            {
                selectedGene = filteredGenes.FirstOrDefault();
            }
        }

        public override void DoWindowContents(Rect inRect)
        {
            // Title Header
            Text.Font = GameFont.Medium;
            Rect titleRect = new Rect(inRect.x, inRect.y, inRect.width, 36f);
            Widgets.Label(titleRect, "GeneSynthesis_WindowTitle".Translate());
            Text.Font = GameFont.Small;

            // Search and Filter Bar
            float topY = inRect.y + 42f;
            Rect searchRect = new Rect(inRect.x, topY, 300f, 30f);
            string newSearch = Widgets.TextField(searchRect, searchText);
            if (newSearch != searchText)
            {
                searchText = newSearch;
                ApplyFilter();
            }

            // Category Filter Dropdown
            Rect catRect = new Rect(searchRect.xMax + 10f, topY, 190f, 30f);
            string catLabel = selectedCategoryFilter != null ? selectedCategoryFilter.LabelCap.ToString() : "GeneSynthesis_AllCategories".Translate().ToString();
            if (Widgets.ButtonText(catRect, catLabel))
            {
                List<FloatMenuOption> options = new List<FloatMenuOption>
                {
                    new FloatMenuOption("GeneSynthesis_AllCategories".Translate(), () =>
                    {
                        selectedCategoryFilter = null;
                        ApplyFilter();
                    })
                };
                foreach (GeneCategoryDef cat in availableCategories)
                {
                    GeneCategoryDef localCat = cat;
                    options.Add(new FloatMenuOption(localCat.LabelCap, () =>
                    {
                        selectedCategoryFilter = localCat;
                        ApplyFilter();
                    }));
                }
                Find.WindowStack.Add(new FloatMenu(options));
            }

            // Mod Filter Dropdown
            Rect modRect = new Rect(catRect.xMax + 10f, topY, 190f, 30f);
            string modLabel = selectedModFilter == "All" ? "GeneSynthesis_AllMods".Translate().ToString() : selectedModFilter;
            if (Widgets.ButtonText(modRect, modLabel))
            {
                List<FloatMenuOption> options = new List<FloatMenuOption>();
                foreach (string m in availableMods)
                {
                    string localM = m;
                    string label = localM == "All" ? "GeneSynthesis_AllMods".Translate().ToString() : localM;
                    options.Add(new FloatMenuOption(label, () =>
                    {
                        selectedModFilter = localM;
                        ApplyFilter();
                    }));
                }
                Find.WindowStack.Add(new FloatMenu(options));
            }

            // Count label
            Rect countRect = new Rect(modRect.xMax + 10f, topY, inRect.width - (modRect.xMax + 10f), 30f);
            Text.Anchor = TextAnchor.MiddleRight;
            Widgets.Label(countRect, "GeneSynthesis_Count".Translate(filteredGenes.Count));
            Text.Anchor = TextAnchor.UpperLeft;

            Widgets.DrawLineHorizontal(inRect.x, topY + 36f, inRect.width);

            // Split content: Left list, Right preview
            float mainY = topY + 44f;
            float mainHeight = inRect.height - mainY;
            float rightWidth = 340f;
            float leftWidth = inRect.width - rightWidth - 12f;

            Rect leftRect = new Rect(inRect.x, mainY, leftWidth, mainHeight);
            Rect rightRect = new Rect(leftRect.xMax + 12f, mainY, rightWidth, mainHeight);

            DrawGeneList(leftRect);
            DrawGenePreview(rightRect);
        }

        private void DrawGeneList(Rect rect)
        {
            Widgets.DrawMenuSection(rect);
            Rect innerRect = rect.ContractedBy(4f);

            // Table Header
            Rect headerRect = new Rect(innerRect.x, innerRect.y, innerRect.width - 16f, 24f);
            Widgets.Label(new Rect(headerRect.x + 40f, headerRect.y, 220f, 24f), "GeneSynthesis_HeaderName".Translate());
            Widgets.Label(new Rect(headerRect.x + 270f, headerRect.y, 70f, 24f), "GeneSynthesis_HeaderCpx".Translate());
            Widgets.Label(new Rect(headerRect.x + 350f, headerRect.y, 70f, 24f), "GeneSynthesis_HeaderMet".Translate());
            Widgets.Label(new Rect(headerRect.x + 430f, headerRect.y, 70f, 24f), "GeneSynthesis_HeaderArc".Translate());
            Widgets.DrawLineHorizontal(innerRect.x, headerRect.yMax, innerRect.width);

            float rowHeight = 36f;
            float scrollContentHeight = filteredGenes.Count * rowHeight;
            Rect viewRect = new Rect(0f, 0f, innerRect.width - 16f, scrollContentHeight);
            Rect scrollRect = new Rect(innerRect.x, headerRect.yMax + 4f, innerRect.width, innerRect.height - 30f);

            Widgets.BeginScrollView(scrollRect, ref listScrollPosition, viewRect);
            float curY = 0f;

            for (int i = 0; i < filteredGenes.Count; i++)
            {
                GeneDef gene = filteredGenes[i];
                Rect rowRect = new Rect(0f, curY, viewRect.width, rowHeight);

                if (i % 2 == 1)
                {
                    Widgets.DrawAltRect(rowRect);
                }

                if (selectedGene == gene)
                {
                    Widgets.DrawHighlightSelected(rowRect);
                }
                else
                {
                    Widgets.DrawHighlightIfMouseover(rowRect);
                }

                // Gene Icon
                Rect iconRect = new Rect(rowRect.x + 4f, rowRect.y + 2f, 32f, 32f);
                if (gene.Icon != null)
                {
                    GUI.color = gene.IconColor;
                    GUI.DrawTexture(iconRect, gene.Icon);
                    GUI.color = Color.white;
                }

                // Label and Mod
                Rect labelRect = new Rect(iconRect.xMax + 8f, rowRect.y + 2f, 220f, 32f);
                string modTag = gene.modContentPack != null ? gene.modContentPack.Name : "Vanilla";
                Text.Font = GameFont.Small;
                Widgets.Label(new Rect(labelRect.x, labelRect.y, labelRect.width, 18f), gene.LabelCap);
                Text.Font = GameFont.Tiny;
                GUI.color = Color.gray;
                Widgets.Label(new Rect(labelRect.x, labelRect.y + 17f, labelRect.width, 14f), modTag);
                GUI.color = Color.white;
                Text.Font = GameFont.Small;

                // Stats: Cpx, Met, Arc
                Rect cpxRect = new Rect(rowRect.x + 270f, rowRect.y + 8f, 70f, 24f);
                Widgets.Label(cpxRect, gene.biostatCpx.ToStringWithSign());

                Rect metRect = new Rect(rowRect.x + 350f, rowRect.y + 8f, 70f, 24f);
                if (gene.biostatMet > 0) GUI.color = Color.green;
                else if (gene.biostatMet < 0) GUI.color = Color.red;
                Widgets.Label(metRect, gene.biostatMet.ToStringWithSign());
                GUI.color = Color.white;

                Rect arcRect = new Rect(rowRect.x + 430f, rowRect.y + 8f, 70f, 24f);
                if (gene.biostatArc > 0)
                {
                    GUI.color = new Color(0.4f, 0.8f, 1f);
                    Widgets.Label(arcRect, gene.biostatArc.ToString());
                    GUI.color = Color.white;
                }
                else
                {
                    Widgets.Label(arcRect, "-");
                }

                if (Widgets.ButtonInvisible(rowRect))
                {
                    selectedGene = gene;
                }

                curY += rowHeight;
            }

            Widgets.EndScrollView();
        }

        private void DrawGenePreview(Rect rect)
        {
            Widgets.DrawMenuSection(rect);
            Rect inner = rect.ContractedBy(10f);

            if (selectedGene == null)
            {
                Text.Anchor = TextAnchor.MiddleCenter;
                GUI.color = Color.gray;
                Widgets.Label(inner, "GeneSynthesis_SelectGenePrompt".Translate());
                GUI.color = Color.white;
                Text.Anchor = TextAnchor.UpperLeft;
                return;
            }

            // Header with Large Icon & Title
            Rect iconRect = new Rect(inner.x, inner.y, 64f, 64f);
            if (selectedGene.Icon != null)
            {
                GUI.color = selectedGene.IconColor;
                GUI.DrawTexture(iconRect, selectedGene.Icon);
                GUI.color = Color.white;
            }

            Rect headerInfoRect = new Rect(iconRect.xMax + 10f, inner.y, inner.width - 74f, 64f);
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(headerInfoRect.x, headerInfoRect.y, headerInfoRect.width, 30f), selectedGene.LabelCap);
            Text.Font = GameFont.Small;
            string modName = selectedGene.modContentPack?.Name ?? "Vanilla";
            string catName = selectedGene.displayCategory?.LabelCap ?? "None";
            GUI.color = Color.gray;
            Widgets.Label(new Rect(headerInfoRect.x, headerInfoRect.y + 30f, headerInfoRect.width, 20f), $"{modName} • {catName}");
            GUI.color = Color.white;

            // Biostats row
            float statsY = inner.y + 72f;
            Rect statsBox = new Rect(inner.x, statsY, inner.width, 32f);
            Widgets.DrawOptionBackground(statsBox, false);
            float colWidth = statsBox.width / 3f;

            // Complexity
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(new Rect(statsBox.x, statsBox.y, colWidth, statsBox.height), $"{"GeneSynthesis_StatCpx".Translate()}: {selectedGene.biostatCpx.ToStringWithSign()}");

            // Metabolism
            if (selectedGene.biostatMet > 0) GUI.color = Color.green;
            else if (selectedGene.biostatMet < 0) GUI.color = Color.red;
            Widgets.Label(new Rect(statsBox.x + colWidth, statsBox.y, colWidth, statsBox.height), $"{"GeneSynthesis_StatMet".Translate()}: {selectedGene.biostatMet.ToStringWithSign()}");
            GUI.color = Color.white;

            // Archite
            if (selectedGene.biostatArc > 0) GUI.color = new Color(0.4f, 0.8f, 1f);
            Widgets.Label(new Rect(statsBox.x + colWidth * 2f, statsBox.y, colWidth, statsBox.height), $"{"GeneSynthesis_StatArc".Translate()}: {selectedGene.biostatArc}");
            GUI.color = Color.white;
            Text.Anchor = TextAnchor.UpperLeft;

            // Description Box
            float descY = statsY + 40f;
            float descHeight = inner.height - descY - 145f;
            Rect descOuterRect = new Rect(inner.x, descY, inner.width, descHeight);
            Widgets.DrawMenuSection(descOuterRect);

            string descText = selectedGene.description ?? "GeneSynthesis_NoDesc".Translate().ToString();
            float textHeight = Text.CalcHeight(descText, descOuterRect.width - 20f);
            Rect descViewRect = new Rect(0f, 0f, descOuterRect.width - 16f, Mathf.Max(textHeight + 10f, descOuterRect.height));

            Widgets.BeginScrollView(descOuterRect.ContractedBy(4f), ref descScrollPosition, descViewRect);
            Widgets.Label(new Rect(4f, 4f, descViewRect.width - 8f, textHeight), descText);
            Widgets.EndScrollView();

            // Synthesis Requirements Card
            float reqY = descOuterRect.yMax + 10f;
            Rect reqBox = new Rect(inner.x, reqY, inner.width, 80f);
            Widgets.DrawOptionBackground(reqBox, false);

            int neutroCost = settings.NeutroamineCost;
            float workAmount = settings.InstantCraft ? 0f : settings.WorkAmount;

            Rect reqTextRect = new Rect(reqBox.x + 8f, reqBox.y + 6f, reqBox.width - 16f, reqBox.height - 12f);
            string costStr = "GeneSynthesis_CostSummary".Translate(neutroCost);
            string workStr = settings.InstantCraft
                ? "GeneSynthesis_WorkInstant".Translate().ToString()
                : "GeneSynthesis_WorkSummary".Translate(workAmount.ToString("F0"), (workAmount / 60f).ToString("F0"));

            Widgets.Label(new Rect(reqTextRect.x, reqTextRect.y, reqTextRect.width, 22f), costStr);
            Widgets.Label(new Rect(reqTextRect.x, reqTextRect.y + 22f, reqTextRect.width, 22f), workStr);

            if (selectedGene.biostatArc > 0 && settings.RequireArchiteCapsules)
            {
                GUI.color = new Color(1f, 0.6f, 0.2f);
                Widgets.Label(new Rect(reqTextRect.x, reqTextRect.y + 44f, reqTextRect.width, 22f), "GeneSynthesis_RequiresArchiteCapsule".Translate(selectedGene.biostatArc));
                GUI.color = Color.white;
            }

            // Start Synthesis Button
            float btnY = reqBox.yMax + 8f;
            Rect btnRect = new Rect(inner.x, btnY, inner.width, 42f);

            if (Widgets.ButtonText(btnRect, "GeneSynthesis_StartBtn".Translate()))
            {
                comp.StartSynthesis(selectedGene, neutroCost, workAmount);
                Close();
            }
        }
    }
}
