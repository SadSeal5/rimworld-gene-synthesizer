# 🧬 SadSeal's Gene Synthesizer for RimWorld

[![RimWorld 1.5, 1.6 & 1.7 (Beta)](https://img.shields.io/badge/RimWorld-1.5%20%7C%201.6%20%7C%201.7%20(Beta)-brightgreen.svg)](#)
[![Language](https://img.shields.io/badge/Language-C%23-blue.svg)](#)
[![License](https://img.shields.io/badge/License-MIT-orange.svg)](#)

**SadSeal's Gene Synthesizer** is an in-game gene crafting expansion for RimWorld Biotech. It allows you to synthesize and craft any gene from vanilla Biotech and any loaded mods directly at the Gene Assembler in exchange for Neutroamine.

---

## ✨ Features

* **🔬 Craft Any Gene:** Browse and synthesize any gene from vanilla Biotech or modded xenotypes (Alpha Genes, Biotech Expanded, etc.) without waiting for rare trader RNG.
* **📦 Neutroamine Currency:** Default cost of 100 Neutroamine per gene, fully configurable from 0 to 1,000.
* **🔍 Search & Filter Dialog:** Real-time text search, category filters, and mod origin filters with biostats (complexity, metabolism, archite capsules) preview.
* **⚙️ In-Depth Mod Settings:**
  * Adjust required Neutroamine cost.
  * Adjust synthesis labor amount (ticks).
  * Toggle instant synthesis (skip pawn work).
  * Toggle Archite gene synthesis permission.
  * Toggle Archite capsule requirements for archite genes.
  * One-click presets: *Free*, *Cheap (25)*, *Standard (100)*, *Hardcore (250)*, and *Reset to Defaults*.
* **👷 Fully Autonomous Colonist Workflow:**
  1. Select a gene to synthesize at any powered Gene Assembler.
  2. Haulers bring required Neutroamine to the machine.
  3. Researchers work at the assembler until synthesis finishes.
  4. The finished Genepack drops directly beside the assembler!
* **🛡️ Safe Cancellation & Dev Tools:**
  * Cancel synthesis anytime to safely drop all loaded Neutroamine.
  * Dev-mode 1-click instant completion button.
* **🌐 Multilingual:** Complete English and Turkish (*Türkçe*) localization.

---

## 🎮 How to Use

1. Build and power a standard **Gene Assembler** (`Building_GeneAssembler`).
2. Click the assembler and select the gizmo **"Synthesize Gene" / "Gen Sentezle"**.
3. Use the dialog to search or filter genes by mod or category.
4. Review the stats and click **"Start Synthesis"**.
5. Colonists will automatically haul the Neutroamine and synthesize your genepack.

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
