param(
    [Parameter(Mandatory = $true)]
    [string] $Executable,
    [int] $DurationMinutes = 60,
    [int] $SampleIntervalSeconds = 10,
    [int] $MaximumWorkingSetGrowthMb = 150,
    [int] $MaximumThreadGrowth = 10,
    [int] $MaximumHandleGrowth = 100
)

$ErrorActionPreference = 'Stop'
if ($DurationMinutes -lt 1 -or $SampleIntervalSeconds -lt 1) {
    throw 'DurationMinutes and SampleIntervalSeconds must be positive.'
}

$process = Start-Process -FilePath (Resolve-Path -LiteralPath $Executable).Path -PassThru
$samples = [System.Collections.Generic.List[object]]::new()
$deadline = [DateTimeOffset]::UtcNow.AddMinutes($DurationMinutes)

try {
    Start-Sleep -Seconds ([Math]::Min(5, $SampleIntervalSeconds))
    while ([DateTimeOffset]::UtcNow -lt $deadline) {
        $process.Refresh()
        if ($process.HasExited) {
            throw "Application exited unexpectedly with code $($process.ExitCode)."
        }

        $samples.Add([pscustomobject]@{
            Timestamp = [DateTimeOffset]::UtcNow
            WorkingSetBytes = $process.WorkingSet64
            ThreadCount = $process.Threads.Count
            HandleCount = if ($IsWindows) { $process.HandleCount } else { $null }
        })
        Start-Sleep -Seconds $SampleIntervalSeconds
    }
}
finally {
    if (-not $process.HasExited) {
        $process.CloseMainWindow() | Out-Null
        if (-not $process.WaitForExit(5000)) {
            $process.Kill($true)
            $process.WaitForExit()
        }
    }
}

if ($samples.Count -lt 2) {
    throw 'The soak test did not collect enough samples.'
}

$first = $samples[0]
$workingSetGrowth = (($samples | Measure-Object WorkingSetBytes -Maximum).Maximum - $first.WorkingSetBytes)
$threadGrowth = (($samples | Measure-Object ThreadCount -Maximum).Maximum - $first.ThreadCount)
$handleGrowth = if ($IsWindows) { (($samples | Measure-Object HandleCount -Maximum).Maximum - $first.HandleCount) } else { 0 }
$report = [pscustomobject]@{
    DurationMinutes = $DurationMinutes
    SampleCount = $samples.Count
    WorkingSetGrowthBytes = $workingSetGrowth
    ThreadGrowth = $threadGrowth
    HandleGrowth = $handleGrowth
    Passed = $workingSetGrowth -le ($MaximumWorkingSetGrowthMb * 1MB) -and $threadGrowth -le $MaximumThreadGrowth -and $handleGrowth -le $MaximumHandleGrowth
}
$report | ConvertTo-Json

if (-not $report.Passed) {
    throw 'The soak test exceeded one or more configured growth limits.'
}
