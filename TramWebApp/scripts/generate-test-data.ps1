param(
    [string]$BaseUrl = 'http://localhost',
    [string]$Username = 'admin',
    [string]$Password = 'admin123',
    [ValidateRange(1, 10)][int]$Runs = 2,
    [ValidateRange(1, 300)][int]$Steps = 25,
    [int]$RandomSeed = 42,
    [Guid]$SourceRunId = [Guid]::Empty,
    [switch]$Cleanup
)
$ErrorActionPreference = 'Stop'
function Call-Api([string]$Method, [string]$Path, $Body = $null) {
    $options = @{ Method = $Method; Uri = "$BaseUrl/api$Path"; Headers = $script:ApiHeaders; TimeoutSec = 30 }
    if ($null -ne $Body) {
        $options.ContentType = 'application/json; charset=utf-8'
        $options.Body = [Text.Encoding]::UTF8.GetBytes((ConvertTo-Json -InputObject $Body -Depth 10))
    }
    try { $result = Invoke-RestMethod @options; Write-Output $result }
    catch { throw "$Method $Path failed: $($_.Exception.Message)" }
}
$script:ApiHeaders = @{}
$login = Call-Api POST '/auth/login' @{ username = $Username; password = $Password }
$script:ApiHeaders = @{ Authorization = "Bearer $($login.accessToken)" }
if ($SourceRunId -eq [Guid]::Empty) {
    $source = $null
    foreach ($item in @(Call-Api GET '/tram-runs')) {
        $candidate = Call-Api GET "/tram-runs/$($item.id)"
        if ($candidate.plannedRoute.Count -ge 2) { $source = $candidate; break }
    }
    if ($null -eq $source) { throw 'No accessible route found. Use -SourceRunId with a run containing plannedRoute.' }
    $SourceRunId = $source.id
}
$random = [Random]::new($RandomSeed)
$generatedIds = [Collections.Generic.List[string]]::new()
$results = [Collections.Generic.List[object]]::new()
try {
    for ($runNumber = 1; $runNumber -le $Runs; $runNumber++) {
        $winter = ($runNumber % 2 -eq 1)
        $created = Call-Api POST "/simulation/$SourceRunId/start" @{ initialSpeedKmh = $random.Next(20, 51); winter = $winter }
        $generatedIds.Add($created.runId)
        $details = Call-Api GET "/tram-runs/$($created.runId)"
        Call-Api PUT "/tram-runs/$($created.runId)" @{ name = "Lab3 dataset $RandomSeed run $runNumber"; description = $details.description } | Out-Null
        for ($step = 1; $step -le $Steps; $step++) {
            $phase = [int][Math]::Floor(($step - 1) / 10) % 3
            $handle = @(0.7, 0.0, -0.8)[$phase]
            Call-Api POST "/simulation/$($created.runId)/step" @{ handle = $handle; sequenceNumber = $step } | Out-Null
        }
        $persisted = Call-Api GET "/tram-runs/$($created.runId)"
        if ($persisted.telemetryPointsCount -ne $Steps + 1 -or $persisted.inferencesCount -ne $Steps + 1) { throw 'Stored dataset size mismatch' }
        $results.Add([pscustomobject]@{ runId = $created.runId; telemetryPoints = $persisted.telemetryPointsCount; inferences = $persisted.inferencesCount; winter = $winter; url = "$BaseUrl/runs/$($created.runId)" })
    }
    $results | Format-Table -AutoSize
} finally {
    if ($Cleanup) {
        foreach ($generatedId in $generatedIds) { Call-Api DELETE "/tram-runs/$generatedId" | Out-Null }
        Write-Host 'Removed only the runs created by this invocation.'
    }
}
