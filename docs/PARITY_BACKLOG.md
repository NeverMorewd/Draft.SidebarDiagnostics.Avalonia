# Remaining migration work

## First increment

- Sensor display names, pinning and ordering are applied after metric construction without changing provider identity.
- Same-name sensor rows are retained and view models remain stable across renames.
- Hiding sensors no longer removes the CPU model used to construct metadata.
- Network activity uses reported link speed and the busiest direction. Unknown speed produces no utilization alert. This estimates link activity, not Internet bandwidth saturation.
- The preferred GPU is shown first while retaining other GPU devices.
- English and Simplified Chinese UI resources establish the localization layer.

## Next increments

1. Resolve Windows AppBar popup placement with an upstream-compatible approach, backed by the Avalonia/WPF reproduction. No arbitrary popup offsets.
2. Add unified category/device/metric visibility and ordering, manual multi-interface selection, and per-volume I/O. Keep collection identity independent of translated labels.
3. Separate multiple CPU devices instead of displaying all their sensor rows under one CPU model.
4. Add CPU/GPU temperature thresholds and independent network direction thresholds with explicit units.
5. Add fifteen-minute and multi-series charts with point inspection while preserving on-demand recording and cleanup.
6. Add compact density, font sizing, and background-only opacity as distinct presentation options.
7. Extend Linux GPU/VRAM and custom mount handling; investigate supported macOS GPU/sensor APIs without claiming unsupported metrics are available.
8. Complete language coverage for provider diagnostics and chart relabeling, remaining hotkey actions, unit preferences, and update experience.

Validate Linux and macOS behavior on actual desktops. Windows build success is not cross-platform runtime acceptance. Keep `PARITY.md` aligned with verified capabilities.
