# Changelog

All notable changes to this package are documented here. This project adheres to
[Semantic Versioning](https://semver.org/).

## [1.1.0] - 2026-06-27

### Added
- **Direct Material target mode**: bind a `Material` reference directly so the
  controller works with any graphic component, including ones with non-standard
  material exposure (e.g. a plain `MonoBehaviour` with a `public Material` field,
  as in some VMG components). When instancing, the runtime clone is reassigned
  back onto whichever component references the bound material (standard
  `Renderer`/`Graphic` and custom `Material` fields/properties via reflection).
- Controller script generation from a **Material asset** (Project window
  right-click) and from the **Material Inspector header** context menu. These
  generate the script only (no GameObject to attach to).

### Changed
- `MaterialControllerBase` now exposes a `Target Mode` (Auto / Direct Material)
  and a `Target Material` field.

## [1.0.0] - 2026-06-26

### Added
- Shader-specific material controller generation from the GameObject menu and from
  the `Renderer` / `Graphic` component header context menus.
- `MaterialControllerBase` with target auto-detection (Renderer/Graphic), optional
  explicit target, and runtime material instancing.
- Generated controllers expose every shader property (Color, Float, Range, Vector,
  Texture, Int) as named, animatable fields.
