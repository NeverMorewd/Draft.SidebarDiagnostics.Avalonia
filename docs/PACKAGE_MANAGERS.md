# Package-manager distribution

Every tagged release publishes immutable platform archives, SHA-256 files, Debian packages, and a `SidebarDiagnostics-package-managers.zip` metadata bundle. The metadata bundle contains a WinGet multi-file manifest and a Homebrew Cask generated from the exact release artifacts.

## Windows

After the package is accepted into the WinGet community repository:

```powershell
winget install --id NeverMorewd.SidebarDiagnostics.Avalonia --exact
winget upgrade --id NeverMorewd.SidebarDiagnostics.Avalonia --exact
winget uninstall --id NeverMorewd.SidebarDiagnostics.Avalonia --exact
```

Before community-index publication, download and extract the package-manager metadata from the chosen GitHub Release, enable local manifests once, and install the generated manifest directory:

```powershell
winget settings --enable LocalManifestFiles
winget install --manifest .\winget\<version>
```

The WinGet package is a per-user portable ZIP. WinGet owns its command alias and installation record, so upgrade and uninstall do not require a custom installer or elevation.

## macOS

After the Cask is accepted into Homebrew Cask:

```shell
brew install --cask sidebar-diagnostics-avalonia
brew upgrade --cask sidebar-diagnostics-avalonia
brew uninstall --cask sidebar-diagnostics-avalonia
```

The generated Cask in the metadata bundle can be tested before community publication:

```shell
brew install --cask ./homebrew/Casks/sidebar-diagnostics-avalonia.rb
brew uninstall --cask sidebar-diagnostics-avalonia
```

The current `.app` bundle is not signed or notarized. Its checksummed Cask is generated and testable, but public Homebrew submission should wait for Apple signing and notarization so users do not need to bypass Gatekeeper.

## Debian and Ubuntu

Download the package matching the machine architecture from a tagged GitHub Release, verify the adjacent SHA-256 file, and install it with APT:

```shell
sha256sum --check SidebarDiagnostics-linux-x64.deb.sha256
sudo apt install ./SidebarDiagnostics-linux-x64.deb
sudo apt install ./SidebarDiagnostics-linux-x64.deb
sudo apt remove sidebar-diagnostics
```

Installing a newer `.deb` performs an upgrade. The ARM64 package uses the same commands with `linux-arm64` in the filename. The release also retains portable tarballs for distributions that do not use Debian packages.

Flatpak was not selected because its process and mount namespaces prevent a system monitor from observing the complete host `/proc` and `/sys` data without broad sandbox exceptions. A native `.deb` preserves accurate host metrics and integrates with APT; tarballs remain the distribution-neutral fallback.

## Versioning, validation, and rollback

Package metadata is generated only from a semantic release version and the final artifacts. Generation fails if an artifact is missing, duplicated, or leaves an unresolved placeholder. CI validates metadata generation on all three operating systems, checks Cask Ruby syntax on macOS, and performs Debian install, upgrade, and uninstall on Ubuntu. Release jobs calculate checksums directly from immutable tag artifacts.

To roll back, select an older tagged release and verify its recorded checksum. Use the older local WinGet manifest with `--force`, uninstall and install the older Homebrew Cask, or run `sudo apt install --allow-downgrades ./<older-package>.deb`. Automatic updates are opt-in through the selected package manager; GitHub fallback downloads remain available permanently.

The generated WinGet directory can be submitted to `microsoft/winget-pkgs`, and the generated Cask can be submitted to `Homebrew/homebrew-cask` after the first signed stable release. External community repositories retain their own review and acceptance authority.
