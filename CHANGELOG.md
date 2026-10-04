# Changelog

All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.0.0] - 2026-10-04

### Added
- **Core Formula Architecture**:
  - `Formula`: Polymorphic formula container powered by `[SerializeReference]`, eliminating forced ScriptableObject asset dependencies while supporting instant preset cloning.
  - `FormulaBase`: Base class for polymorphic evaluation, parameter fingerprint hashing, and zero-allocation cloning.
  - `FormulaRegistry`: Reflection-based automatic strategy registry supporting priority ordering and decoupling base containers from concrete formulas.
  - `FormulaTemplateSO`: ScriptableObject template preset asset for project-wide formula library management.
- **7 Built-in Progression Models**:
  - `LinearFormula`: Base point + slope linear scaling ($y = y_0 + k \cdot (x - x_0)$).
  - `ExponentialFormula`: Compounding geometric scaling ($y = y_0 \cdot r^{(x - x_0)}$).
  - `PowerFormula`: Polynomial power curve ($y = y_0 + a \cdot (x - x_0)^p$) supporting diminishing or aggressive acceleration.
  - `AsymptoticFormula`: Diminishing returns approaching a soft cap ($y = y_0 + (MaxValue - y_0) \cdot \frac{\Delta x}{\Delta x + K}$).
  - `SegmentedFormula`: Multi-stage composite progression with linear, power, exponential, and animation curve stages.
  - `CurveFormula`: Native Unity `AnimationCurve` mapping with offset and scaling.
  - `ConstantFormula`: Constant baseline value evaluation.
- **Editor Tooling & Visual Experience**:
  - `FormulaDrawer`: Card-style Inspector drawer featuring foldable headers, strategy drop-down selection, copy/paste/reset operations, and native Object Picker preset selection.
  - `FormulaPreviewPopup`: Hover-activated floating curve preview window with 120-point dense anti-aliased rendering, dynamic origin alignment, pin support, and live crosshair coordinate sampling.
  - `SegmentDrawer`: Visual segment list editor for composite segmented formulas.
- **Performance**:
  - Fully allocation-free (0-GC) runtime evaluation for all formula models.
