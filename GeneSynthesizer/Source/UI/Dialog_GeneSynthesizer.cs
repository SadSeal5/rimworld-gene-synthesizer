using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace SadSeal.GeneSynthesizer.UI
{
    public enum SynthesizerTab
    {
        Genes,
        Xenotypes,
        Queue
    }

    public class Dialog_GeneSynthesizer : Window
    {
        private readonly CompGeneSynthesizer comp;
        private readonly GeneSynthesizerSettings settings;

        private SynthesizerTab currentTab = SynthesizerTab.Genes;

        // Genes Tab State
        private List<GeneDef> allGenes = new List<GeneDef>();
        private List<GeneDef> filteredGenes = new List<GeneDef>();
        private GeneDef selectedGene;
        private string geneSearchText = "";
        private string selectedGeneModFilter = "All";
        private GeneCategoryDef selectedCategoryFilter;
        private Vector2 geneListScrollPosition = Vector2.zero;
        private Vector2 geneDescScrollPosition = Vector2.zero;
        private List<string> availableGeneMods = new List<string>();
        private List<GeneCategoryDef> availableCategories = new List<GeneCategoryDef>();

        // Xenotypes Tab State
        private List<XenotypeDef> allXenotypes = new List<XenotypeDef>();
        private List<XenotypeDef> filteredXenotypes = new List<XenotypeDef>();
        private XenotypeDef selectedXenotype;
        private string xenotypeSearchText = "";
        private string selectedXenotypeModFilter = "All";
        private Vector2 xenotypeListScrollPosition = Vector2.zero;
        private Vector2 xenotypeDescScrollPosition = Vector2.zero;
        private Vector2 xenotypeGenesScrollPosition = Vector2.zero;
        private List<string> availableXenotypeMods = new List<string>();

        // Queue Tab State
        private Vector2 queueScrollPosition = Vector2.zero;

        public override Vector2 InitialSize => new Vector2(1020f, 720f);

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

            // Load Genes
            allGenes = DefDatabase<GeneDef>.AllDefsListForReading
                .Where(g => g != null && !g.label.NullOrEmpty())
                .OrderBy(g => g.label)
                .ToList();

            availableGeneMods = allGenes
                .Select(g => g.modContentPack?.Name ?? "Vanilla")
                .Distinct()
                .OrderBy(m => m)
                .ToList();
            availableGeneMods.Insert(0, "All");

            availableCategories = allGenes
                .Select(g => g.displayCategory)
                .Where(c => c != null)
                .Distinct()
                .OrderBy(c => c.label)
                .ToList();

            ApplyGeneFilter();
            if (filteredGenes.Count > 0)
            {
                selectedGene = filteredGenes[0];
            }

            // Load Xenotypes (supports all vanilla and all modded xenotypes)
            allXenotypes = DefDatabase<XenotypeDef>.AllDefsListForReading
                .Where(x => x != null && !x.label.NullOrEmpty())
                .OrderBy(x => x.label)
                .ToList();

            availableXenotypeMods = allXenotypes
                .Select(x => x.modContentPack?.Name ?? "Vanilla")
                .Distinct()
                .OrderBy(m => m)
                .ToList();
            availableXenotypeMods.Insert(0, "All");

            ApplyXenotypeFilter();
            if (filteredXenotypes.Count > 0)
            {
                selectedXenotype = filteredXenotypes[0];
            }
        }

        private void ApplyGeneFilter()
        {
            IEnumerable<GeneDef> query = allGenes;

            if (!settings.AllowArchiteGenes)
            {
                query = query.Where(g => g.biostatArc <= 0);
            }

            if (selectedGeneModFilter != "All")
            {
                query = query.Where(g => (g.modContentPack?.Name ?? "Vanilla") == selectedGeneModFilter);
            }

            if (selectedCategoryFilter != null)
            {
                query = query.Where(g => g.displayCategory == selectedCategoryFilter);
            }

            if (!geneSearchText.NullOrEmpty())
            {
                string searchLower = geneSearchText.Trim().ToLower();
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

        private void ApplyXenotypeFilter()
        {
            IEnumerable<XenotypeDef> query = allXenotypes;

            if (selectedXenotypeModFilter != "All")
            {
                query = query.Where(x => (x.modContentPack?.Name ?? "Vanilla") == selectedXenotypeModFilter);
            }

            if (!xenotypeSearchText.NullOrEmpty())
            {
                string searchLower = xenotypeSearchText.Trim().ToLower();
                query = query.Where(x =>
                    (!x.label.NullOrEmpty() && x.label.ToLower().Contains(searchLower)) ||
                    (!x.defName.NullOrEmpty() && x.defName.ToLower().Contains(searchLower)) ||
                    (!x.description.NullOrEmpty() && x.description.ToLower().Contains(searchLower)) ||
                    (x.genes != null && x.genes.Any(g => g != null && !g.label.NullOrEmpty() && g.label.ToLower().Contains(searchLower))));
            }

            filteredXenotypes = query.ToList();
            if (selectedXenotype != null && !filteredXenotypes.Contains(selectedXenotype))
            {
                selectedXenotype = filteredXenotypes.FirstOrDefault();
            }
        }

        public override void DoWindowContents(Rect inRect)
        {
            // Title Header
            Text.Font = GameFont.Medium;
            Rect titleRect = new Rect(inRect.x, inRect.y, inRect.width, 32f);
            Widgets.Label(titleRect, "GeneSynthesis_WindowTitle".Translate());
            Text.Font = GameFont.Small;

            // Navigation Tabs Bar
            Rect tabRow = new Rect(inRect.x, titleRect.yMax + 4f, inRect.width, 32f);
            float tabWidth = 220f;

            Rect tab1 = new Rect(tabRow.x, tabRow.y, tabWidth, 30f);
            Rect tab2 = new Rect(tab1.xMax + 8f, tabRow.y, tabWidth, 30f);
            Rect tab3 = new Rect(tab2.xMax + 8f, tabRow.y, tabWidth, 30f);

            if (currentTab == SynthesizerTab.Genes) Widgets.DrawHighlightSelected(tab1);
            if (Widgets.ButtonText(tab1, "🧬 " + "GeneSynthesis_TabGenes".Translate()))
            {
                currentTab = SynthesizerTab.Genes;
            }

            if (currentTab == SynthesizerTab.Xenotypes) Widgets.DrawHighlightSelected(tab2);
            if (Widgets.ButtonText(tab2, "👥 " + "GeneSynthesis_TabXenotypes".Translate()))
            {
                currentTab = SynthesizerTab.Xenotypes;
            }

            int queueCount = comp.OrderQueue?.Count ?? 0;
            string queueLabel = queueCount > 0
                ? "📋 " + "GeneSynthesis_TabQueueCount".Translate(queueCount)
                : "📋 " + "GeneSynthesis_TabQueue".Translate();
            if (currentTab == SynthesizerTab.Queue) Widgets.DrawHighlightSelected(tab3);
            if (Widgets.ButtonText(tab3, queueLabel))
            {
                currentTab = SynthesizerTab.Queue;
            }

            Widgets.DrawLineHorizontal(inRect.x, tabRow.yMax + 4f, inRect.width);

            // Tab Content
            Rect contentRect = new Rect(inRect.x, tabRow.yMax + 8f, inRect.width, inRect.height - (tabRow.yMax + 8f));

            switch (currentTab)
            {
                case SynthesizerTab.Genes:
                    DoGenesTab(contentRect);
                    break;
                case SynthesizerTab.Xenotypes:
                    DoXenotypesTab(contentRect);
                    break;
                case SynthesizerTab.Queue:
                    DoQueueTab(contentRect);
                    break;
            }
        }

        // =========================================================================
        // TAB 1: SINGLE GENES
        // =========================================================================
        private void DoGenesTab(Rect inRect)
        {
            // Search and Filter Bar
            float topY = inRect.y;
            Rect searchRect = new Rect(inRect.x, topY, 280f, 30f);
            string newSearch = Widgets.TextField(searchRect, geneSearchText);
            if (newSearch != geneSearchText)
            {
                geneSearchText = newSearch;
                ApplyGeneFilter();
            }

            // Category Filter Dropdown
            Rect catRect = new Rect(searchRect.xMax + 8f, topY, 190f, 30f);
            string catLabel = selectedCategoryFilter != null ? selectedCategoryFilter.LabelCap.ToString() : "GeneSynthesis_AllCategories".Translate().ToString();
            if (Widgets.ButtonText(catRect, catLabel))
            {
                List<FloatMenuOption> options = new List<FloatMenuOption>
                {
                    new FloatMenuOption("GeneSynthesis_AllCategories".Translate(), () =>
                    {
                        selectedCategoryFilter = null;
                        ApplyGeneFilter();
                    })
                };
                foreach (GeneCategoryDef cat in availableCategories)
                {
                    GeneCategoryDef localCat = cat;
                    options.Add(new FloatMenuOption(localCat.LabelCap, () =>
                    {
                        selectedCategoryFilter = localCat;
                        ApplyGeneFilter();
                    }));
                }
                Find.WindowStack.Add(new FloatMenu(options));
            }

            // Mod Filter Dropdown
            Rect modRect = new Rect(catRect.xMax + 8f, topY, 190f, 30f);
            string modLabel = selectedGeneModFilter == "All" ? "GeneSynthesis_AllMods".Translate().ToString() : selectedGeneModFilter;
            if (Widgets.ButtonText(modRect, modLabel))
            {
                List<FloatMenuOption> options = new List<FloatMenuOption>();
                foreach (string m in availableGeneMods)
                {
                    string localM = m;
                    string label = localM == "All" ? "GeneSynthesis_AllMods".Translate().ToString() : localM;
                    options.Add(new FloatMenuOption(label, () =>
                    {
                        selectedGeneModFilter = localM;
                        ApplyGeneFilter();
                    }));
                }
                Find.WindowStack.Add(new FloatMenu(options));
            }

            // Count label
            Rect countRect = new Rect(modRect.xMax + 8f, topY, inRect.width - (modRect.xMax + 8f), 30f);
            Text.Anchor = TextAnchor.MiddleRight;
            Widgets.Label(countRect, "GeneSynthesis_Count".Translate(filteredGenes.Count));
            Text.Anchor = TextAnchor.UpperLeft;

            Widgets.DrawLineHorizontal(inRect.x, topY + 34f, inRect.width);

            // Split content: Left list, Right preview
            float mainY = topY + 40f;
            float mainHeight = inRect.height - 40f;
            float rightWidth = 360f;
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

            Widgets.BeginScrollView(scrollRect, ref geneListScrollPosition, viewRect);
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
            Rect iconRect = new Rect(inner.x, inner.y, 60f, 60f);
            if (selectedGene.Icon != null)
            {
                GUI.color = selectedGene.IconColor;
                GUI.DrawTexture(iconRect, selectedGene.Icon);
                GUI.color = Color.white;
            }

            Rect headerInfoRect = new Rect(iconRect.xMax + 10f, inner.y, inner.width - 70f, 60f);
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(headerInfoRect.x, headerInfoRect.y, headerInfoRect.width, 28f), selectedGene.LabelCap);
            Text.Font = GameFont.Small;
            string modName = selectedGene.modContentPack?.Name ?? "Vanilla";
            string catName = selectedGene.displayCategory?.LabelCap ?? "None";
            GUI.color = Color.gray;
            Widgets.Label(new Rect(headerInfoRect.x, headerInfoRect.y + 28f, headerInfoRect.width, 20f), $"{modName} • {catName}");
            GUI.color = Color.white;

            // Biostats row
            float statsY = inner.y + 68f;
            Rect statsBox = new Rect(inner.x, statsY, inner.width, 32f);
            Widgets.DrawOptionBackground(statsBox, false);
            float colWidth = statsBox.width / 3f;

            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(new Rect(statsBox.x, statsBox.y, colWidth, statsBox.height), $"{"GeneSynthesis_StatCpx".Translate()}: {selectedGene.biostatCpx.ToStringWithSign()}");

            if (selectedGene.biostatMet > 0) GUI.color = Color.green;
            else if (selectedGene.biostatMet < 0) GUI.color = Color.red;
            Widgets.Label(new Rect(statsBox.x + colWidth, statsBox.y, colWidth, statsBox.height), $"{"GeneSynthesis_StatMet".Translate()}: {selectedGene.biostatMet.ToStringWithSign()}");
            GUI.color = Color.white;

            if (selectedGene.biostatArc > 0) GUI.color = new Color(0.4f, 0.8f, 1f);
            Widgets.Label(new Rect(statsBox.x + colWidth * 2f, statsBox.y, colWidth, statsBox.height), $"{"GeneSynthesis_StatArc".Translate()}: {selectedGene.biostatArc}");
            GUI.color = Color.white;
            Text.Anchor = TextAnchor.UpperLeft;

            // Description Box
            float descY = statsY + 38f;
            float descHeight = inner.height - descY - 145f;
            Rect descOuterRect = new Rect(inner.x, descY, inner.width, descHeight);
            Widgets.DrawMenuSection(descOuterRect);

            string descText = selectedGene.description ?? "GeneSynthesis_NoDesc".Translate().ToString();
            float textHeight = Text.CalcHeight(descText, descOuterRect.width - 20f);
            Rect descViewRect = new Rect(0f, 0f, descOuterRect.width - 16f, Mathf.Max(textHeight + 10f, descOuterRect.height));

            Widgets.BeginScrollView(descOuterRect.ContractedBy(4f), ref geneDescScrollPosition, descViewRect);
            Widgets.Label(new Rect(4f, 4f, descViewRect.width - 8f, textHeight), descText);
            Widgets.EndScrollView();

            // Synthesis Requirements Card
            float reqY = descOuterRect.yMax + 8f;
            Rect reqBox = new Rect(inner.x, reqY, inner.width, 76f);
            Widgets.DrawOptionBackground(reqBox, false);

            int neutroCost = settings.NeutroamineCost;
            float workAmount = settings.InstantCraft ? 0f : settings.WorkAmount;

            Rect reqTextRect = new Rect(reqBox.x + 8f, reqBox.y + 4f, reqBox.width - 16f, reqBox.height - 8f);
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

            // Action Buttons: "Start Synthesis" & "Add to Queue"
            float btnY = reqBox.yMax + 8f;
            float btnWidth = (inner.width - 8f) / 2f;
            Rect btnStartRect = new Rect(inner.x, btnY, btnWidth, 38f);
            Rect btnQueueRect = new Rect(btnStartRect.xMax + 8f, btnY, btnWidth, 38f);

            if (Widgets.ButtonText(btnStartRect, "GeneSynthesis_StartBtn".Translate()))
            {
                comp.EnqueueSingleGene(selectedGene, neutroCost, workAmount);
                Messages.Message("GeneSynthesis_OrderAdded".Translate(selectedGene.LabelCap), MessageTypeDefOf.TaskCompletion, false);
                Close();
            }

            if (Widgets.ButtonText(btnQueueRect, "GeneSynthesis_AddToQueue".Translate()))
            {
                comp.EnqueueSingleGene(selectedGene, neutroCost, workAmount);
                Messages.Message("GeneSynthesis_OrderAdded".Translate(selectedGene.LabelCap), MessageTypeDefOf.TaskCompletion, false);
            }
        }

        // =========================================================================
        // TAB 2: XENOTYPES & PRESETS
        // =========================================================================
        private void DoXenotypesTab(Rect inRect)
        {
            // Search and Filter Bar
            float topY = inRect.y;
            Rect searchRect = new Rect(inRect.x, topY, 320f, 30f);
            string newSearch = Widgets.TextField(searchRect, xenotypeSearchText);
            if (newSearch != xenotypeSearchText)
            {
                xenotypeSearchText = newSearch;
                ApplyXenotypeFilter();
            }

            // Mod Filter Dropdown
            Rect modRect = new Rect(searchRect.xMax + 10f, topY, 220f, 30f);
            string modLabel = selectedXenotypeModFilter == "All" ? "GeneSynthesis_AllMods".Translate().ToString() : selectedXenotypeModFilter;
            if (Widgets.ButtonText(modRect, modLabel))
            {
                List<FloatMenuOption> options = new List<FloatMenuOption>();
                foreach (string m in availableXenotypeMods)
                {
                    string localM = m;
                    string label = localM == "All" ? "GeneSynthesis_AllMods".Translate().ToString() : localM;
                    options.Add(new FloatMenuOption(label, () =>
                    {
                        selectedXenotypeModFilter = localM;
                        ApplyXenotypeFilter();
                    }));
                }
                Find.WindowStack.Add(new FloatMenu(options));
            }

            // Count label
            Rect countRect = new Rect(modRect.xMax + 10f, topY, inRect.width - (modRect.xMax + 10f), 30f);
            Text.Anchor = TextAnchor.MiddleRight;
            Widgets.Label(countRect, "GeneSynthesis_AllXenotypesCount".Translate(filteredXenotypes.Count));
            Text.Anchor = TextAnchor.UpperLeft;

            Widgets.DrawLineHorizontal(inRect.x, topY + 34f, inRect.width);

            // Split content: Left list, Right preview
            float mainY = topY + 40f;
            float mainHeight = inRect.height - 40f;
            float rightWidth = 380f;
            float leftWidth = inRect.width - rightWidth - 12f;

            Rect leftRect = new Rect(inRect.x, mainY, leftWidth, mainHeight);
            Rect rightRect = new Rect(leftRect.xMax + 12f, mainY, rightWidth, mainHeight);

            DrawXenotypeList(leftRect);
            DrawXenotypePreview(rightRect);
        }

        private void DrawXenotypeList(Rect rect)
        {
            Widgets.DrawMenuSection(rect);
            Rect innerRect = rect.ContractedBy(4f);

            // Table Header
            Rect headerRect = new Rect(innerRect.x, innerRect.y, innerRect.width - 16f, 24f);
            Widgets.Label(new Rect(headerRect.x + 40f, headerRect.y, 220f, 24f), "GeneSynthesis_HeaderName".Translate());
            Widgets.Label(new Rect(headerRect.x + 280f, headerRect.y, 160f, 24f), "GeneSynthesis_AllMods".Translate());
            Widgets.Label(new Rect(headerRect.x + 450f, headerRect.y, 80f, 24f), "GeneSynthesis_HeaderGenesCount".Translate());
            Widgets.DrawLineHorizontal(innerRect.x, headerRect.yMax, innerRect.width);

            float rowHeight = 40f;
            float scrollContentHeight = filteredXenotypes.Count * rowHeight;
            Rect viewRect = new Rect(0f, 0f, innerRect.width - 16f, scrollContentHeight);
            Rect scrollRect = new Rect(innerRect.x, headerRect.yMax + 4f, innerRect.width, innerRect.height - 30f);

            Widgets.BeginScrollView(scrollRect, ref xenotypeListScrollPosition, viewRect);
            float curY = 0f;

            for (int i = 0; i < filteredXenotypes.Count; i++)
            {
                XenotypeDef xenotype = filteredXenotypes[i];
                Rect rowRect = new Rect(0f, curY, viewRect.width, rowHeight);

                if (i % 2 == 1)
                {
                    Widgets.DrawAltRect(rowRect);
                }

                if (selectedXenotype == xenotype)
                {
                    Widgets.DrawHighlightSelected(rowRect);
                }
                else
                {
                    Widgets.DrawHighlightIfMouseover(rowRect);
                }

                // Xenotype Icon
                Rect iconRect = new Rect(rowRect.x + 4f, rowRect.y + 4f, 32f, 32f);
                if (xenotype.Icon != null)
                {
                    GUI.DrawTexture(iconRect, xenotype.Icon);
                }

                // Xenotype Label
                Rect labelRect = new Rect(iconRect.xMax + 8f, rowRect.y + 2f, 230f, 36f);
                Text.Font = GameFont.Small;
                Widgets.Label(new Rect(labelRect.x, labelRect.y, labelRect.width, 20f), xenotype.LabelCap);
                Text.Font = GameFont.Tiny;
                GUI.color = Color.gray;
                string heritageTag = xenotype.inheritable
                    ? "GeneSynthesis_InheritableGermline".Translate().ToString()
                    : "GeneSynthesis_InheritableXenogerm".Translate().ToString();
                Widgets.Label(new Rect(labelRect.x, labelRect.y + 18f, labelRect.width, 16f), heritageTag);
                GUI.color = Color.white;
                Text.Font = GameFont.Small;

                // Mod Source
                Rect modSourceRect = new Rect(rowRect.x + 280f, rowRect.y + 10f, 160f, 22f);
                string modName = xenotype.modContentPack != null ? xenotype.modContentPack.Name : "Vanilla";
                GUI.color = new Color(0.85f, 0.85f, 0.85f);
                Widgets.Label(modSourceRect, modName);
                GUI.color = Color.white;

                // Gene Count
                Rect geneCountRect = new Rect(rowRect.x + 450f, rowRect.y + 10f, 80f, 22f);
                int geneCount = xenotype.genes?.Count ?? 0;
                Widgets.Label(geneCountRect, $"{geneCount}");

                if (Widgets.ButtonInvisible(rowRect))
                {
                    selectedXenotype = xenotype;
                }

                curY += rowHeight;
            }

            Widgets.EndScrollView();
        }

        private void DrawXenotypePreview(Rect rect)
        {
            Widgets.DrawMenuSection(rect);
            Rect inner = rect.ContractedBy(8f);

            if (selectedXenotype == null)
            {
                Text.Anchor = TextAnchor.MiddleCenter;
                GUI.color = Color.gray;
                Widgets.Label(inner, "GeneSynthesis_SelectXenotypePrompt".Translate());
                GUI.color = Color.white;
                Text.Anchor = TextAnchor.UpperLeft;
                return;
            }

            List<GeneDef> validGenes = selectedXenotype.genes?.Where(g => g != null).ToList() ?? new List<GeneDef>();

            // Header: Icon + Title + Mod
            Rect iconRect = new Rect(inner.x, inner.y, 52f, 52f);
            if (selectedXenotype.Icon != null)
            {
                GUI.DrawTexture(iconRect, selectedXenotype.Icon);
            }

            Rect headerInfoRect = new Rect(iconRect.xMax + 8f, inner.y, inner.width - 60f, 52f);
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(headerInfoRect.x, headerInfoRect.y, headerInfoRect.width, 28f), selectedXenotype.LabelCap);
            Text.Font = GameFont.Small;
            string modName = selectedXenotype.modContentPack?.Name ?? "Vanilla";
            string heritageStr = selectedXenotype.inheritable
                ? "GeneSynthesis_InheritableGermline".Translate().ToString()
                : "GeneSynthesis_InheritableXenogerm".Translate().ToString();
            GUI.color = Color.gray;
            Widgets.Label(new Rect(headerInfoRect.x, headerInfoRect.y + 28f, headerInfoRect.width, 20f), $"{modName} • {heritageStr}");
            GUI.color = Color.white;

            // Stats summary row: Total Complexity, Metabolism, Archite, Genes count
            float statsY = inner.y + 58f;
            Rect statsBox = new Rect(inner.x, statsY, inner.width, 30f);
            Widgets.DrawOptionBackground(statsBox, false);
            float colWidth = statsBox.width / 4f;

            int totalCpx = validGenes.Sum(g => g.biostatCpx);
            int totalMet = validGenes.Sum(g => g.biostatMet);
            int totalArc = validGenes.Sum(g => g.biostatArc);

            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(new Rect(statsBox.x, statsBox.y, colWidth, statsBox.height), $"Cpx: {totalCpx.ToStringWithSign()}");

            if (totalMet > 0) GUI.color = Color.green;
            else if (totalMet < 0) GUI.color = Color.red;
            Widgets.Label(new Rect(statsBox.x + colWidth, statsBox.y, colWidth, statsBox.height), $"Met: {totalMet.ToStringWithSign()}");
            GUI.color = Color.white;

            if (totalArc > 0) GUI.color = new Color(0.4f, 0.8f, 1f);
            Widgets.Label(new Rect(statsBox.x + colWidth * 2f, statsBox.y, colWidth, statsBox.height), $"Arc: {totalArc}");
            GUI.color = Color.white;

            Widgets.Label(new Rect(statsBox.x + colWidth * 3f, statsBox.y, colWidth, statsBox.height), $"{validGenes.Count} genes");
            Text.Anchor = TextAnchor.UpperLeft;

            // Description Box
            float descY = statsY + 34f;
            float descHeight = 65f;
            Rect descOuterRect = new Rect(inner.x, descY, inner.width, descHeight);
            Widgets.DrawMenuSection(descOuterRect);

            string descText = selectedXenotype.description ?? "GeneSynthesis_NoDesc".Translate().ToString();
            float textHeight = Text.CalcHeight(descText, descOuterRect.width - 16f);
            Rect descViewRect = new Rect(0f, 0f, descOuterRect.width - 16f, Mathf.Max(textHeight + 6f, descOuterRect.height));

            Widgets.BeginScrollView(descOuterRect.ContractedBy(3f), ref xenotypeDescScrollPosition, descViewRect);
            Widgets.Label(new Rect(3f, 3f, descViewRect.width - 6f, textHeight), descText);
            Widgets.EndScrollView();

            // Constituent Genes Grid / Scrollable List
            float genesHeaderY = descOuterRect.yMax + 6f;
            Rect genesHeaderRect = new Rect(inner.x, genesHeaderY, inner.width, 22f);
            Text.Font = GameFont.Tiny;
            Widgets.Label(genesHeaderRect, "GeneSynthesis_ConstituentGenes".Translate(validGenes.Count));
            Text.Font = GameFont.Small;

            float genesListY = genesHeaderRect.yMax + 2f;
            float genesListHeight = inner.height - genesListY - 146f;
            Rect genesOuterRect = new Rect(inner.x, genesListY, inner.width, genesListHeight);
            Widgets.DrawMenuSection(genesOuterRect);

            float rowH = 28f;
            float totalGenesHeight = validGenes.Count * rowH;
            Rect genesViewRect = new Rect(0f, 0f, genesOuterRect.width - 16f, Mathf.Max(totalGenesHeight, genesOuterRect.height));

            Widgets.BeginScrollView(genesOuterRect.ContractedBy(3f), ref xenotypeGenesScrollPosition, genesViewRect);
            float geneCurY = 0f;
            for (int j = 0; j < validGenes.Count; j++)
            {
                GeneDef g = validGenes[j];
                Rect gRow = new Rect(0f, geneCurY, genesViewRect.width, rowH);
                if (j % 2 == 1) Widgets.DrawAltRect(gRow);

                // Mini Icon
                Rect gIconRect = new Rect(gRow.x + 2f, gRow.y + 2f, 24f, 24f);
                if (g.Icon != null)
                {
                    GUI.color = g.IconColor;
                    GUI.DrawTexture(gIconRect, g.Icon);
                    GUI.color = Color.white;
                }

                // Label
                Rect gLabelRect = new Rect(gIconRect.xMax + 6f, gRow.y + 4f, 180f, 20f);
                Widgets.Label(gLabelRect, g.LabelCap);

                // Stats: Cpx, Met, Arc
                Rect gCpxRect = new Rect(gRow.xMax - 110f, gRow.y + 4f, 32f, 20f);
                Text.Font = GameFont.Tiny;
                Widgets.Label(gCpxRect, g.biostatCpx.ToStringWithSign());

                Rect gMetRect = new Rect(gRow.xMax - 75f, gRow.y + 4f, 32f, 20f);
                if (g.biostatMet > 0) GUI.color = Color.green;
                else if (g.biostatMet < 0) GUI.color = Color.red;
                Widgets.Label(gMetRect, g.biostatMet.ToStringWithSign());
                GUI.color = Color.white;

                Rect gArcRect = new Rect(gRow.xMax - 40f, gRow.y + 4f, 35f, 20f);
                if (g.biostatArc > 0)
                {
                    GUI.color = new Color(0.4f, 0.8f, 1f);
                    Widgets.Label(gArcRect, $"A:{g.biostatArc}");
                    GUI.color = Color.white;
                }
                Text.Font = GameFont.Small;

                geneCurY += rowH;
            }
            Widgets.EndScrollView();

            // Synthesis Requirements Card
            float reqY = genesOuterRect.yMax + 6f;
            Rect reqBox = new Rect(inner.x, reqY, inner.width, 64f);
            Widgets.DrawOptionBackground(reqBox, false);

            int totalNeutroCost = settings.NeutroamineCost * validGenes.Count;
            float totalWorkAmount = settings.InstantCraft ? 0f : (settings.WorkAmount * validGenes.Count);

            Rect reqTextRect = new Rect(reqBox.x + 8f, reqBox.y + 4f, reqBox.width - 16f, reqBox.height - 8f);
            string costStr = "GeneSynthesis_CostSummary".Translate(totalNeutroCost);
            string workStr = settings.InstantCraft
                ? "GeneSynthesis_WorkInstant".Translate().ToString()
                : "GeneSynthesis_WorkSummary".Translate(totalWorkAmount.ToString("F0"), (totalWorkAmount / 60f).ToString("F0"));

            Widgets.Label(new Rect(reqTextRect.x, reqTextRect.y, reqTextRect.width, 20f), costStr);
            Widgets.Label(new Rect(reqTextRect.x, reqTextRect.y + 20f, reqTextRect.width, 20f), workStr);

            if (totalArc > 0 && settings.RequireArchiteCapsules)
            {
                GUI.color = new Color(1f, 0.6f, 0.2f);
                Widgets.Label(new Rect(reqTextRect.x, reqTextRect.y + 40f, reqTextRect.width, 20f), "GeneSynthesis_RequiresArchiteCapsule".Translate(totalArc));
                GUI.color = Color.white;
            }

            // Action Buttons
            float btnY = reqBox.yMax + 6f;
            Rect btnQueueAllRect = new Rect(inner.x, btnY, inner.width, 32f);
            Rect btnCraftPackRect = new Rect(inner.x, btnQueueAllRect.yMax + 4f, inner.width, 32f);

            // Action 1: Queue all genes individually
            if (Widgets.ButtonText(btnQueueAllRect, "GeneSynthesis_QueueAllGenes".Translate(validGenes.Count)))
            {
                comp.EnqueueBatchGenes(validGenes, settings.NeutroamineCost, settings.InstantCraft ? 0f : settings.WorkAmount);
                Messages.Message("GeneSynthesis_BatchEnqueued".Translate(validGenes.Count, selectedXenotype.LabelCap), MessageTypeDefOf.TaskCompletion, false);
            }

            // Action 2: Craft combined Genepack
            if (Widgets.ButtonText(btnCraftPackRect, "GeneSynthesis_CraftXenotypePack".Translate()))
            {
                comp.EnqueueCombinedXenotype(selectedXenotype, totalNeutroCost, totalWorkAmount);
                Messages.Message("GeneSynthesis_CombinedEnqueued".Translate(selectedXenotype.LabelCap), MessageTypeDefOf.TaskCompletion, false);
            }
        }

        // =========================================================================
        // TAB 3: SYNTHESIS QUEUE
        // =========================================================================
        private void DoQueueTab(Rect inRect)
        {
            List<GeneSynthesisOrder> queue = comp.OrderQueue ?? new List<GeneSynthesisOrder>();

            // Header bar
            float topY = inRect.y;
            Rect titleRect = new Rect(inRect.x, topY, 400f, 30f);
            Text.Font = GameFont.Medium;
            Widgets.Label(titleRect, "GeneSynthesis_QueueTitle".Translate(queue.Count));
            Text.Font = GameFont.Small;

            if (queue.Count > 0)
            {
                Rect clearBtnRect = new Rect(inRect.xMax - 180f, topY, 180f, 30f);
                if (Widgets.ButtonText(clearBtnRect, "GeneSynthesis_ClearQueue".Translate()))
                {
                    comp.ClearQueue();
                }
            }

            Widgets.DrawLineHorizontal(inRect.x, topY + 34f, inRect.width);

            // Queue content
            float mainY = topY + 40f;
            float mainHeight = inRect.height - 40f;
            Rect listRect = new Rect(inRect.x, mainY, inRect.width, mainHeight);
            Widgets.DrawMenuSection(listRect);

            if (queue.Count == 0)
            {
                Rect emptyRect = listRect.ContractedBy(20f);
                Text.Anchor = TextAnchor.MiddleCenter;
                GUI.color = Color.gray;
                Widgets.Label(emptyRect, "GeneSynthesis_QueueEmpty".Translate());
                GUI.color = Color.white;
                Text.Anchor = TextAnchor.UpperLeft;
                return;
            }

            Rect innerRect = listRect.ContractedBy(6f);
            float itemHeight = 60f;
            float totalHeight = queue.Count * (itemHeight + 6f);
            Rect viewRect = new Rect(0f, 0f, innerRect.width - 16f, Mathf.Max(totalHeight, innerRect.height));

            Widgets.BeginScrollView(innerRect, ref queueScrollPosition, viewRect);
            float curY = 0f;

            for (int i = 0; i < queue.Count; i++)
            {
                GeneSynthesisOrder order = queue[i];
                Rect rowRect = new Rect(0f, curY, viewRect.width, itemHeight);

                // Background
                Widgets.DrawOptionBackground(rowRect, false);
                if (i == 0)
                {
                    // Highlight active order
                    Widgets.DrawHighlightSelected(rowRect);
                }
                else
                {
                    Widgets.DrawHighlightIfMouseover(rowRect);
                }

                // Status Tag / Index badge
                Rect badgeRect = new Rect(rowRect.x + 8f, rowRect.y + 8f, 90f, 22f);
                if (i == 0)
                {
                    GUI.color = Color.green;
                    Widgets.Label(badgeRect, $"#1 [{"GeneSynthesis_QueueStatusActive".Translate()}]");
                    GUI.color = Color.white;
                }
                else
                {
                    GUI.color = Color.gray;
                    Widgets.Label(badgeRect, $"#{i + 1} [{"GeneSynthesis_QueueStatusWaiting".Translate()}]");
                    GUI.color = Color.white;
                }

                // Icon
                Rect iconRect = new Rect(badgeRect.xMax + 6f, rowRect.y + 6f, 48f, 48f);
                Texture2D orderIcon = null;
                Color iconColor = Color.white;

                if (order.SourceXenotype?.Icon != null)
                {
                    orderIcon = order.SourceXenotype.Icon;
                }
                else if (order.Genes.Count > 0 && order.Genes[0]?.Icon != null)
                {
                    orderIcon = order.Genes[0].Icon;
                    iconColor = order.Genes[0].IconColor;
                }
                else
                {
                    orderIcon = ContentFinder<Texture2D>.Get("UI/Gizmos/RecombineGenes", false) ?? TexCommand.DesirePower;
                }

                if (orderIcon != null)
                {
                    GUI.color = iconColor;
                    GUI.DrawTexture(iconRect, orderIcon);
                    GUI.color = Color.white;
                }

                // Title and details
                Rect infoRect = new Rect(iconRect.xMax + 10f, rowRect.y + 6f, rowRect.width - (iconRect.xMax + 10f) - 130f, 48f);
                Text.Font = GameFont.Small;
                Widgets.Label(new Rect(infoRect.x, infoRect.y, infoRect.width, 22f), order.Label);

                Text.Font = GameFont.Tiny;
                GUI.color = Color.gray;
                string detailsText = order.IsCombinedPack
                    ? "GeneSynthesis_QueueCombinedDetails".Translate(order.Genes.Count, order.NeutroamineCost).ToString()
                    : "GeneSynthesis_QueueSingleDetails".Translate(order.NeutroamineCost).ToString();
                Widgets.Label(new Rect(infoRect.x, infoRect.y + 24f, infoRect.width, 18f), detailsText);
                GUI.color = Color.white;
                Text.Font = GameFont.Small;

                // Active order live progress
                if (i == 0)
                {
                    Rect progressRect = new Rect(rowRect.xMax - 220f, rowRect.y + 12f, 160f, 36f);
                    if (comp.innerContainer.TotalStackCount < order.NeutroamineCost)
                    {
                        GUI.color = new Color(1f, 0.8f, 0.4f);
                        Widgets.Label(progressRect, "GeneSynthesis_QueueHaulingProgress".Translate(comp.innerContainer.TotalStackCount, order.NeutroamineCost));
                        GUI.color = Color.white;
                    }
                    else
                    {
                        float pct = order.WorkRequired > 0f ? (order.WorkDone / order.WorkRequired) : 1f;
                        GUI.color = Color.cyan;
                        Widgets.Label(progressRect, "GeneSynthesis_QueueWorkProgress".Translate(pct.ToStringPercent()));
                        GUI.color = Color.white;
                    }
                }

                // Delete Button [X]
                Rect delBtnRect = new Rect(rowRect.xMax - 44f, rowRect.y + 14f, 32f, 32f);
                if (Widgets.ButtonText(delBtnRect, "X"))
                {
                    comp.RemoveOrderAt(i);
                }

                curY += itemHeight + 6f;
            }

            Widgets.EndScrollView();
        }
    }
}
