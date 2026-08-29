param(
    [Parameter(Mandatory = $true)] [string] $Version,
    [Parameter(Mandatory = $true)] [string] $ArtifactDirectory,
    [Parameter(Mandatory = $true)] [string] $OutputDirectory,
    [string] $Repository = 'NeverMorewd/SidebarDiagnostics.Avalonia'
)

$ErrorActionPreference = 'Stop'
$normalizedVersion = $Version.TrimStart('v')
if ($normalizedVersion -notmatch '^\d+\.\d+\.\d+([.-][0-9A-Za-z.-]+)?$') {
    throw "Version '$Version' is not a supported release version."
}

$artifactRoot = (Resolve-Path -LiteralPath $ArtifactDirectory).Path
$outputRoot = [System.IO.Path]::GetFullPath($OutputDirectory)
New-Item -ItemType Directory -Force -Path $outputRoot | Out-Null

function Get-Artifact([string] $name) {
    $match = Get-ChildItem -LiteralPath $artifactRoot -Recurse -File -Filter $name
    if ($match.Count -ne 1) {
        throw "Expected exactly one '$name' artifact, found $($match.Count)."
    }
    return $match[0]
}

function Get-Hash([System.IO.FileInfo] $file) {
    return (Get-FileHash -LiteralPath $file.FullName -Algorithm SHA256).Hash.ToLowerInvariant()
}

$windows = Get-Artifact 'SidebarDiagnostics-win-x64.zip'
$macIntel = Get-Artifact 'SidebarDiagnostics-osx-x64.zip'
$macArm = Get-Artifact 'SidebarDiagnostics-osx-arm64.zip'
$linuxX64 = Get-Artifact 'SidebarDiagnostics-linux-x64.deb'
$linuxArm = Get-Artifact 'SidebarDiagnostics-linux-arm64.deb'
$releaseBase = "https://github.com/$Repository/releases/download/v$normalizedVersion"
$identifier = 'NeverMorewd.SidebarDiagnostics.Avalonia'
$wingetDirectory = Join-Path $outputRoot "winget/$normalizedVersion"
$homebrewDirectory = Join-Path $outputRoot 'homebrew/Casks'
New-Item -ItemType Directory -Force -Path $wingetDirectory, $homebrewDirectory | Out-Null

@"
# yaml-language-server: `$schema=https://aka.ms/winget-manifest.version.1.12.0.schema.json
PackageIdentifier: $identifier
PackageVersion: $normalizedVersion
DefaultLocale: en-US
ManifestType: version
ManifestVersion: 1.12.0
"@ | Set-Content -LiteralPath (Join-Path $wingetDirectory "$identifier.yaml") -Encoding utf8NoBOM

@"
# yaml-language-server: `$schema=https://aka.ms/winget-manifest.installer.1.12.0.schema.json
PackageIdentifier: $identifier
PackageVersion: $normalizedVersion
InstallerType: zip
NestedInstallerType: portable
InstallModes:
  - interactive
  - silent
  - silentWithProgress
UpgradeBehavior: install
Installers:
  - Architecture: x64
    InstallerUrl: $releaseBase/$($windows.Name)
    InstallerSha256: $(Get-Hash $windows)
    NestedInstallerFiles:
      - RelativeFilePath: SidebarDiagnostics.App.exe
        PortableCommandAlias: sidebar-diagnostics
ManifestType: installer
ManifestVersion: 1.12.0
"@ | Set-Content -LiteralPath (Join-Path $wingetDirectory "$identifier.installer.yaml") -Encoding utf8NoBOM

@"
# yaml-language-server: `$schema=https://aka.ms/winget-manifest.defaultLocale.1.12.0.schema.json
PackageIdentifier: $identifier
PackageVersion: $normalizedVersion
PackageLocale: en-US
Publisher: NeverMorewd
PublisherUrl: https://github.com/NeverMorewd
PublisherSupportUrl: https://github.com/$Repository/issues
PackageName: Sidebar Diagnostics
PackageUrl: https://github.com/$Repository
License: GPL-3.0-only
LicenseUrl: https://github.com/$Repository/blob/v$normalizedVersion/LICENSE.md
ShortDescription: Cross-platform system monitor built with Avalonia.
Moniker: sidebar-diagnostics
Tags:
  - avalonia
  - hardware
  - monitor
  - system
ManifestType: defaultLocale
ManifestVersion: 1.12.0
"@ | Set-Content -LiteralPath (Join-Path $wingetDirectory "$identifier.locale.en-US.yaml") -Encoding utf8NoBOM

@"
cask "sidebar-diagnostics-avalonia" do
  arch arm: "arm64", intel: "x64"

  version "$normalizedVersion"
  sha256 arm:   "$(Get-Hash $macArm)",
         intel: "$(Get-Hash $macIntel)"

  url "$releaseBase/SidebarDiagnostics-osx-#{arch}.zip"
  name "Sidebar Diagnostics"
  desc "Cross-platform system monitor built with Avalonia"
  homepage "https://github.com/$Repository"

  app "Sidebar Diagnostics.app"

  zap trash: "~/Library/Application Support/SidebarDiagnostics"
end
"@ | Set-Content -LiteralPath (Join-Path $homebrewDirectory 'sidebar-diagnostics-avalonia.rb') -Encoding utf8NoBOM

$metadata = [ordered]@{
    version = $normalizedVersion
    repository = $Repository
    generatedAtUtc = [DateTimeOffset]::UtcNow.ToString('O')
    artifacts = [ordered]@{
        $windows.Name = Get-Hash $windows
        $macIntel.Name = Get-Hash $macIntel
        $macArm.Name = Get-Hash $macArm
        $linuxX64.Name = Get-Hash $linuxX64
        $linuxArm.Name = Get-Hash $linuxArm
    }
}
$metadata | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath (Join-Path $outputRoot 'checksums.json') -Encoding utf8NoBOM

Get-ChildItem -LiteralPath $outputRoot -Recurse -File | ForEach-Object {
    if ((Get-Content -LiteralPath $_.FullName -Raw).Contains('__')) {
        throw "Unresolved placeholder found in $($_.FullName)."
    }
}

$metadata | ConvertTo-Json -Depth 4
