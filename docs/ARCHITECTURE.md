# Architecture

## Design goals

The port is designed around four constraints: platform parity, predictable resource use, testability, and a presentation layer that does not know which operating system it is running on.

## Runtime flow

1. `PlatformMetricsProviderFactory` selects one native provider at startup.
2. The provider samples system-wide CPU and memory data.
3. `SystemMetricsService` combines native data with portable storage and network statistics.
4. The service returns an immutable `SystemMetricsSnapshot`.
5. `MainViewModel` merges visible hardware and external readings into diagnostic sections.
6. `MetricSeriesCatalog` maintains bounded histories for graphable rows.
7. Avalonia views render responsive sections and open dedicated live chart windows on demand.

## Platform boundary

All operating-system calls live under `Services/Platform`. Code outside that directory consumes `IPlatformMetricsProvider` and does not branch on the current operating system. Adding or replacing a provider therefore does not require presentation changes.

## Data lifetime

Sampling is performed on a fixed interval controlled by user settings. Every graphable metric keeps bounded history. Remote external-IP lookup runs outside the core sampling path and uses a cached result. Settings are normalized before use and written through a temporary file followed by an atomic replacement.

## Theme boundary

Views and custom drawing controls depend only on Sidebar Diagnostics semantic resources. `ApplicationThemeService` activates exactly one base control theme and adds an adapter palette when necessary. The default appearance combines Fluent control templates with the Sidebar palette; the Pip-Boy appearance combines Pipboy.Avalonia templates with a monochromatic mapping of the same semantic resources. Dynamic resource references update existing windows in place, while unsaved previews are reverted when the settings dialog closes.

## Deployment model

Release artifacts are trimmed Native AOT applications built on runners matching each target architecture. Runtime JSON serialization uses source-generated metadata, and view construction is explicit so release builds do not depend on reflection-discovered application types. The Windows hardware provider preserves LibreHardwareMonitor as an explicit compatibility boundary because portions of that third-party library are not annotated for trimming or Native AOT.

## Error handling

Individual transient sampling failures preserve the running application and surface a degraded status. Unsupported operating systems fail immediately with an explicit message. Invalid settings fall back to safe defaults.
