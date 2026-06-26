# Changelog

All notable changes to this package are documented here. This project adheres to
[Semantic Versioning](https://semver.org/).

## [1.0.0] - 2026-06-26

### Added
- Shader-specific material controller generation from the GameObject menu and from
  the `Renderer` / `Graphic` component header context menus.
- `MaterialControllerBase` with target auto-detection (Renderer/Graphic), optional
  explicit target, and runtime material instancing.
- Generated controllers expose every shader property (Color, Float, Range, Vector,
  Texture, Int) as named, animatable fields.
