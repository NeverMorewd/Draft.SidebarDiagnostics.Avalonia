param(
    [Parameter(Mandatory = $true)]
    [string] $PublishDirectory
)

$ErrorActionPreference = 'Stop'
$resolvedDirectory = (Resolve-Path -LiteralPath $PublishDirectory).Path
$forbiddenPatterns = @(
    'WinRing0_1_2_0',
    'WinRing0.sys',
    'WinRing0x64.sys',
    'R0LibreHardwareMonitor',
    'OpenLibSys',
    'LibreHardwareMonitor.sys'
)
$candidateExtensions = @('.dll', '.exe', '.sys')
$findings = [System.Collections.Generic.List[object]]::new()

foreach ($file in Get-ChildItem -LiteralPath $resolvedDirectory -Recurse -File) {
    foreach ($pattern in $forbiddenPatterns) {
        if ($file.Name.Contains($pattern, [System.StringComparison]::OrdinalIgnoreCase)) {
            $findings.Add([pscustomobject]@{ File = $file.FullName; Pattern = $pattern; Location = 'FileName' })
        }
    }

    if ($candidateExtensions -notcontains $file.Extension.ToLowerInvariant()) {
        continue
    }

    $content = [System.Text.Encoding]::Latin1.GetString([System.IO.File]::ReadAllBytes($file.FullName))
    foreach ($pattern in $forbiddenPatterns) {
        if ($content.Contains($pattern, [System.StringComparison]::OrdinalIgnoreCase)) {
            $findings.Add([pscustomobject]@{ File = $file.FullName; Pattern = $pattern; Location = 'BinaryContent' })
        }
    }
}

if ($findings.Count -gt 0) {
    $findings | Format-Table -AutoSize
    throw "Release security audit found $($findings.Count) forbidden legacy-driver reference(s)."
}

$hardwareLibrary = Get-ChildItem -LiteralPath $resolvedDirectory -Recurse -File -Filter 'LibreHardwareMonitorLib.dll' | Select-Object -First 1
if ($null -eq $hardwareLibrary) {
    $hardwareLibrary = Get-ChildItem -LiteralPath $resolvedDirectory -Recurse -File -Filter 'SidebarDiagnostics*.exe' |
        Sort-Object Length -Descending |
        Select-Object -First 1
}
if ($null -eq $hardwareLibrary) {
    throw 'Neither LibreHardwareMonitorLib.dll nor the Windows single-file application was found.'
}

$hardwareContent = [System.Text.Encoding]::Latin1.GetString([System.IO.File]::ReadAllBytes($hardwareLibrary.FullName))
if (-not $hardwareContent.Contains('PawnIO', [System.StringComparison]::Ordinal)) {
    throw 'The published hardware library does not contain the expected PawnIO implementation marker.'
}

[pscustomobject]@{
    PublishDirectory = $resolvedDirectory
    FilesScanned = (Get-ChildItem -LiteralPath $resolvedDirectory -Recurse -File).Count
    LegacyDriverReferences = 0
    PawnIoImplementation = $true
} | ConvertTo-Json
