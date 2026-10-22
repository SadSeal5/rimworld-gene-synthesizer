# 🧬 SadSeal's Gene Synthesizer for RimWorld

[![RimWorld 1.5, 1.6 & 1.7 (Beta)](https://img.shields.io/badge/RimWorld-1.5%20%7C%201.6%20%7C%201.7%20(Beta)-brightgreen.svg)](#)
[![Language](https://img.shields.io/badge/Language-C%23-blue.svg)](#)
[![License](https://img.shields.io/badge/License-MIT-orange.svg)](#)

**SadSeal's Gene Synthesizer** is an in-game gene crafting expansion for RimWorld Biotech. It allows you to synthesize and craft any gene from vanilla Biotech and any loaded mods directly at the Gene Assembler in exchange for Neutroamine.

---

## ✨ Features

* **🔬 Craft Any Gene:** Browse and synthesize any gene from vanilla Biotech or modded xenotypes (Alpha Genes, Biotech Expanded, etc.) without waiting for rare trader RNG.
* **👥 Xenotype Presets & Batch Crafting:**
  * Select any xenotype preset (Sanguophage, Dirtmole, Hussar, Genie, Impid, Neanderthal, etc.) or any modded xenotype.
  * **Batch Queue:** Queue all constituent genes of a xenotype individually with one click.
  * **Combined Genepack:** Craft a single all-in-one genepack containing all genes of the chosen xenotype.
  * Inspect constituent genes, germline/xenogerm status, total complexity, metabolism, and archite costs.
* **📋 Multi-Order Synthesis Queue:**
  * Queue multiple genes or xenotype orders consecutively.
  * Real-time queue tab displaying active progress, waiting orders, and hauling status.
  * Delete specific orders or clear the entire queue at any time.
* **📦 Neutroamine Currency:** Configurable Neutroamine cost per gene from 0 to 1,000 (default: 100).
* **🔍 Search & Filter Dialog:** Real-time text search, category filters, and mod origin filters with biostats (complexity, metabolism, archite capsules) preview.
* **⚙️ In-Depth Mod Settings:**
  * Adjust required Neutroamine cost.
  * Adjust synthesis labor amount (ticks).
  * Toggle instant synthesis (skip pawn work).
  * Toggle Archite gene synthesis permission.
  * Toggle Archite capsule requirements for archite genes.
  * One-click presets: *Free*, *Cheap (25)*, *Standard (100)*, *Hardcore (250)*, and *Reset to Defaults*.
* **👷 Fully Autonomous Colonist Workflow:**
  1. Select a gene or xenotype preset to synthesize at any powered Gene Assembler.
  2. Haulers bring required Neutroamine to the machine.
  3. Researchers work at the assembler until synthesis finishes.
  4. The finished Genepack drops directly beside the assembler!
  5. The machine automatically moves to the next order in the queue!
* **🛡️ Safe Cancellation & Dev Tools:**
  * Cancel current order or clear the entire queue anytime to safely drop all loaded Neutroamine.
  * Dev-mode 1-click instant completion button.
* **🌐 Multilingual:** Complete English and Turkish (*Türkçe*) localization.

---

## 🎮 How to Use

1. Build and power a standard **Gene Assembler** (`Building_GeneAssembler`).
2. Click the assembler and select the gizmo **"Synthesize Gene / Presets" / "Gen / Tür Sentezle"**.
3. Choose from the three navigation tabs:
   * **🧬 Single Genes:** Search and filter individual genes, then start synthesis or add to queue.
   * **👥 Xenotypes & Presets:** Browse vanilla or modded xenotypes, preview all their genes, and either queue all genes individually or craft a combined genepack.
   * **📋 Synthesis Queue:** View active order progress, waiting orders, and manage queued items.
4. Colonists will automatically haul the Neutroamine and synthesize your genepacks sequentially.

---

## 📋 Requirements

* **RimWorld:** `1.5`, `1.6`, or `1.7` (Beta)
* **Biotech DLC** (`Ludeon.RimWorld.Biotech`)
* **Harmony** (`brrainz.harmony`)

---

## 📁 Installation

1. Copy the `Rimworld-Gene-Synthesizer` folder into your RimWorld `Mods/` directory.
2. Enable **Harmony**, **Biotech**, and **SadSeal's Gene Synthesizer** in the Mod Manager.
3. Enjoy tailoring your colonists' genetics!
