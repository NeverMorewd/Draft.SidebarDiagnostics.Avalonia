# Competition screenshot checklist

Screenshots must come from real builds on the named operating system. Do not use mockups, generated screens, editor previews, red annotations, or images from an intermediate build with known defects.

## Required gallery

| File | Platform and scene | Status |
| --- | --- | --- |
| `images/wpf-before-windows.png` | Original WPF sidebar with CPU, RAM, GPU, and drive details | Captured |
| `images/avalonia-after-windows.png` | Current Windows build, sidebar chrome visible, dense metrics | Captured |
| `images/avalonia-after-macos.png` | `.app` bundle running on macOS, sidebar and menu bar visible | Needed |
| `images/avalonia-after-linux.png` | Native Linux build running on a desktop session, sidebar and panel visible | Needed |
| `images/avalonia-settings.png` | Current settings window showing theme and sensor controls | Captured |
| `images/avalonia-chart.png` | Live metric chart with axes, duration selector, pin, and close controls | Needed |

## Capture standard

- Build from the release candidate commit or download the matching pre-release Native AOT artifact.
- Use a standard desktop scale and a clean desktop without personal notifications.
- Wait for at least two metric refreshes before capturing.
- Keep the sidebar at its default 360-pixel width.
- Show the sidebar chrome in one image and the distraction-free pointer-away state in another only when both add information.
- Use real machine readings. External IP display should remain disabled.
- Avoid cropping away all operating-system context in the macOS and Linux images; judges should be able to recognize the platform.
- Use PNG and preserve native resolution. Do not upscale or apply cosmetic filters.
- Verify that host names, IP addresses, MAC addresses, usernames, and unrelated applications reveal nothing sensitive.
