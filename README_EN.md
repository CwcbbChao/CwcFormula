# CwcFormula - Visual Progression Formula & Curve Engine for Unity

[![Unity](https://img.shields.io/badge/Unity-2021.3+-blue.svg)](https://unity.com/)
[![License](https://img.shields.io/badge/License-CWC_Tools_1.0-orange.svg)](#license-terms)
[![GC-Alloc](https://img.shields.io/badge/GC--Alloc-0_Bytes-brightgreen.svg)](#performance)

An intuitive, practical, and out-of-the-box mathematical formula toolset for Unity. Specially built for RPGs, action, strategy, and idle progression systems, helping you eliminate the frustrations of **"blind parameter tuning without seeing the curve"**, **"clunky formula refactoring in code"**, and **"asset bloat with hundreds of tiny ScriptableObjects"**.

---

## Game Development Pain Points: Why CwcFormula?

| Common Pain Point | Traditional Frustrations | CwcFormula's Solution |
| :--- | :--- | :--- |
| **Blind Curve Tuning** | Adjusting parameters without knowing how the curve looks; forced to enter PlayMode or plot in Excel. | Hover your mouse over the **`[Curve]`** button in Inspector to instantly view a smooth anti-aliased graph. Move the cursor for live crosshair readings at any level without typing. |
| **Painful Formula Swapping** | Hardcoding linear growth into game logic; when designers want power curves or multi-stage tiers, code must be rewritten. | Unified `Formula` container. Switch between Linear, Exponential, Power, Asymptotic, and Segmented from an Inspector drop-down at any time; code simply calls `Evaluate(level)`. |
| **ScriptableObject Asset Bloat** | Every weapon or skill tweaking its base slope forces creating a separate `.asset` file, cluttering the Project window. | Formula data is directly embedded and serialized in the component via `[SerializeReference]`. Edit independently, or clone from/save to `FormulaTemplateSO` presets whenever needed. |
| **Runtime GC Spikes & Framerate Drops** | Dynamic expression tree parsing or string parsing triggers frequent GC allocations during combat. | Pure mathematical analytical calculation under the hood. Single evaluation takes nanoseconds with **guaranteed 0 heap allocation (0-GC)**. |

---

## 3-Step Quick Start

### 1. Declare Formula Fields in Any Script
```csharp
using UnityEngine;
using Cwcbb.Tools.CwcFormula;

public class HeroStats : MonoBehaviour
{
    // Base Attack scaling: defaults to linear growth
    [SerializeField] private Formula _attackFormula = new Formula(new LinearFormula(new Vector2(1, 10), 2.5f));

    // Armor damage reduction: uses an asymptotic curve with a soft cap (base 10%, theoretical maximum 75%)
    [SerializeField] private Formula _armorReductionFormula = new Formula(new AsymptoticFormula(1, 0.1f, 0.75f, 100f));
}
```

### 2. Runtime Evaluation (One Line)
```csharp
public float GetAttackAtLevel(int level)
{
    // Nanosecond evaluation, zero GC
    return _attackFormula.Evaluate(level);
}

public float GetDamageReduction(float armor)
{
    return _armorReductionFormula.Evaluate(armor);
}
```

### 3. Visual Inspector Tuning & Preview
- Add the component to any GameObject; the Inspector displays a card-style drawer.
- Click the strategy dropdown to switch between **Linear**, **Exponential**, **Power**, **Segmented**, and more.
- **Hover over `[Curve]`**: Instantly pops up a smooth curve graph. Move your cursor to see exact numbers at Level 10 or check if Level 100 explodes. Click **Pin** in the corner to lock the popup open.

---

## 7 Built-in Progression Models

No need to write complex math functions; comes with the 7 most common progression formulas used in game design:

| Formula Type | Plain Description & Game Scenario | Key Parameters |
| :--- | :--- | :--- |
| **Linear** | Steady per-level progression (e.g. +5 Attack per level, +10 Armor per upgrade). | `StartPoint` (base input and base value), `Slope` (delta per unit) |
| **Exponential** | Compounding geometric growth for idle/clicker games (e.g. attributes multiply by $1.08\times$ each level). | `StartPoint` (anchor), `GrowthFactor` (multiplier base, e.g. 1.05 for +5% per level) |
| **Power** | Ideal for monster HP scaling and diminishing returns. $p < 1$ grows fast early and flattens later; $p > 1$ accelerates sharply. | `StartPoint`, `Coefficient`, `Exponent` (power exponent like 0.6 or 1.5) |
| **Asymptotic** | Armor damage reduction, crit resistance, movement slow caps that **must never exceed 100%**. Diminishing returns approaching a soft ceiling. | `StartPoint`, `MaxValue` (ceiling, e.g. 0.75), `HalfPointK` (speed to half benefit) |
| **Segmented** | Sophisticated RPG tiering: Levels 1~10 linear onboarding, 11~30 power curve, 31+ exponential endgame, continuous and smooth. | Multiple independent stages (combining Linear, Power, Exponential, and Curves) |
| **Curve** | Handcrafted curves via Unity `AnimationCurve` with offset and scale controls. | `Curve` (AnimationCurve), `Scale` (multiplier) |
| **Constant** | Guaranteed baseline floor value unaffected by inputs. | `Value` (constant output) |

---

## Key Editor Features

### 1. Hover Anti-Aliased Curve Popup
- **Instant Glimpse**: Floats smoothly into view when hovering over the `[Curve]` button, dismissing when cursor leaves.
- **Dense 120-Point Sampling**: Smooth anti-aliased curve rendering with smart origin spatial reference.
- **Live Crosshair Sampling**: Move the cursor across the graph for precise real-time `(X, Y)` coordinate readouts without keyboard input.
- **Compile & Reload Safety**: Three-tier lifecycle protection cleanly handles script recompilation and domain reloads without leaving ghost windows.

### 2. Best of Both Worlds: Component Embedded & Global Presets
- **Zero Asset Bloat**: Standard formulas live directly inside your Monobehaviours and ScriptableObjects without generating loose asset files in the Project folder.
- **Template Presets (`FormulaTemplateSO`)**:
  - Right-click in Project view to create reusable template presets (e.g. `Warrior_HP_Curve`).
  - One-click copy preset into an instance, or export customized settings as a new template preset.

### 3. Practical Utilities
- Single-click **Copy / Paste** for entire formula setups via the context menu.
- One-click **Reset to Default** to restore baseline settings.

---

## Directory Structure

```text
Assets/CwcPlugins/CwcFormula/
├── Runtime/
│   ├── Formula.cs                  # General polymorphic formula wrapper
│   ├── FormulaBase.cs              # Pure C# polymorphic base class
│   ├── IFormula.cs                 # Base evaluation interface
│   ├── FormulaRegistry.cs          # Strategy registry & decoupling factory
│   ├── FormulaStrategyAttribute.cs # Attribute tagging for formula discovery
│   ├── FormulaTemplateSO.cs        # Reusable ScriptableObject template asset
│   ├── FormulaTestComponent.cs     # Ready-to-use testing component
│   └── Types/                      # 7 built-in math formula models
│       ├── ConstantFormula.cs
│       ├── LinearFormula.cs
│       ├── ExponentialFormula.cs
│       ├── PowerFormula.cs
│       ├── AsymptoticFormula.cs
│       ├── SegmentedFormula.cs
│       └── CurveFormula.cs
├── Editor/
│   ├── FormulaDrawer.cs            # Card-style Inspector property drawer
│   ├── FormulaPreviewPopup.cs      # Hover anti-aliased dynamic preview popup
│   └── SegmentDrawer.cs            # Visual drawer for segmented formulas
├── CHANGELOG.md
├── LICENSE
├── package.json
├── README.md
└── README_EN.md
```

---

## Performance

- **Memory Allocations**: All formula `Evaluate(float)` implementations rely on pure mathematical analytical equations. **0 bytes GC allocation per call**.
- **Computation Overhead**: Single evaluations run in nanoseconds, perfectly safe for tight update loops and projectile hit calculations.

---

## License Terms

This project is licensed under the **CWC Tools Public License v1.0**:
- **Free for End Products**: You are granted royalty-free permission to integrate and distribute this package in any commercial or non-commercial compiled games (PC, Mobile, Consoles).
- **Source Protection**: Redistributing, sublicensing, or publishing the raw source code or package as a standalone development tool or asset pack on any public marketplace or registry (including Unity Asset Store, GitHub, GitLab, etc.) is strictly prohibited.
