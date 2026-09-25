# WPF Migration Parity

This audit compares the Avalonia port with the public features and settings of the original WPF application. It distinguishes portable behavior from Windows shell integration.

## Monitoring

| Original capability | Port status | Notes |
| --- | --- | --- |
| CPU utilization | Complete | Native provider on every platform. |
| Memory utilization and used memory | Complete | Native provider on every platform. |
| Logical drive utilization | Partial | Selected mounted volumes show capacity and utilization. Custom Unix mount paths can be filtered out; per-volume read/write throughput is not implemented. |
| Network download and upload | Partial | One automatically selected interface. Manual multi-interface selection is not implemented. Activity uses the busiest direction relative to reported link speed, not Internet subscription bandwidth. |
| Hardware sensors | Platform-limited | Broad LibreHardwareMonitor support on Windows; standard hwmon sensors on Linux; no stable public macOS equivalent. |
| Live graphs | Complete | Every primary metric has bounded history. |
| Dedicated configurable graph window | Partial | Single-series draggable, pinnable windows with up to five minutes of history, recorded only while open. WPF multi-series charts, fifteen-minute duration, and point inspection remain pending. |
| External and local IP display | Complete | Local addresses are shown automatically. External address lookup is opt-in and cached. |

## Presentation and settings

| Original capability | Port status | Notes |
| --- | --- | --- |
| Compact sidebar | Complete | Responsive Avalonia layout. |
| Machine name, clock, date, and 12/24-hour format | Complete | Date visibility and culture-aware month/day, short, and long formats are configurable. |
| Alert thresholds | Partial | Usage thresholds color sections. WPF temperature alerts, separate upload/download thresholds, and optional blinking are not implemented. |
| Celsius and Fahrenheit | Complete | Applied to temperature values. |
| Width, opacity, always-on-top, start minimized | Partial | Persisted settings. Opacity affects the entire window rather than just the background; width is restricted to 320–640 DIP. |
| UI scale, fonts, alignment, offsets, and arbitrary colors | Redesigned | The port uses a curated accessible design system. |
| Per-monitor and per-sensor configuration | Partial | Sensor visibility, pinning, naming, and ordering reach the sidebar through stable identities. Full category/device configuration and multi-CPU presentation remain pending. |
| Localization | Partial | English fallback and Simplified Chinese resources cover window controls, tray menu, common metric labels and key statuses. Provider diagnostics and additional WPF languages remain pending. |

## Desktop integration

| Original capability | Port status | Notes |
| --- | --- | --- |
| Tray show, hide, and exit | Complete | Avalonia desktop integration. |
| Launch at login | Complete | Native implementation on all three platforms. |
| Windows AppBar reserved work area | Known issue | Area reservation works, but Avalonia popups are displaced outside the reserved strip. Reproduced with SimpleTheme; equivalent WPF sample behaves correctly. |
| Edge docking and multi-monitor repositioning | Complete | Placement follows the selected display and recovers from topology changes. |
| Click-through and Alt-Tab/tool-window modes | Complete on Windows | The sidebar stays out of the taskbar on every platform; native pointer pass-through is available on Windows. |
| Global hotkeys | Partial | Show, hide, and toggle. WPF reload, exit, cycle-screen, and cycle-edge actions remain pending. Native hooks are subject to platform/session permissions. |
| Automatic application updates | Replaced | GitHub Releases provide immutable checksummed packages. |

## Acceptance baseline

The cross-platform scope covers monitoring, persistence, alerts, charts, tray behavior, and startup integration on each supported operating system. Windows-shell-only behavior is tracked explicitly and is never represented as portable functionality.
