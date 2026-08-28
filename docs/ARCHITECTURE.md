# Architecture

## Design goals

The port is designed around four constraints: platform parity, predictable resource use, testability, and a presentation layer that does not know which operating system it is running on.

## Runtime flow

1. `PlatformMetricsProviderFactory` selects one native provider at startup.
2. The provider samples system-wide CPU and memory data.
3. `SystemMetricsService` combines native data with portable storage and network statistics.
4. The service returns an immutable `SystemMetricsSnapshot`.
5. `MainViewModel` formats the snapshot and updates bounded histories.
6. Avalonia views render cards, progress states, and sparklines.

## Platform boundary

All operating-system calls live under `Services/Platform`. Code outside that directory consumes `IPlatformMetricsProvider` and does not branch on the current operating system. Adding or replacing a provider therefore does not require presentation changes.

## Data lifetime

Sampling is performed on a fixed interval controlled by user settings. Every metric keeps a bounded 60-sample history. Settings are normalized before use and written through a temporary file followed by an atomic replacement.

## Error handling

Individual transient sampling failures preserve the running application and surface a degraded status. Unsupported operating systems fail immediately with an explicit message. Invalid settings fall back to safe defaults.
