param([string]$BaseUrl = 'http://localhost')
$ErrorActionPreference = 'Stop'

function Assert-True($Condition, [string]$Message) {
    if (-not $Condition) { throw $Message }
}
function Request([string]$Method, [string]$Path, $Body = $null, $Headers = @{}) {
    $parameters = @{ Method = $Method; Uri = "$BaseUrl/api$Path"; Headers = $Headers; TimeoutSec = 30 }
    if ($null -ne $Body) {
        $parameters.ContentType = 'application/json; charset=utf-8'
        $parameters.Body = [Text.Encoding]::UTF8.GetBytes((ConvertTo-Json -InputObject $Body -Depth 10))
    }
    try { $result = Invoke-RestMethod @parameters }
    catch { throw "$Method $Path failed: $($_.Exception.Message)" }
    Write-Output $result
}
function Assert-Status([string]$Path, [int]$Expected, $Headers = @{}) {
    try {
        $response = Invoke-WebRequest -UseBasicParsing -Uri "$BaseUrl/api$Path" -Headers $Headers
        $actual = [int]$response.StatusCode
    } catch {
        if ($null -eq $_.Exception.Response) { throw }
        $actual = [int]$_.Exception.Response.StatusCode
    }
    Assert-True ($actual -eq $Expected) "$Path returned $actual, expected $Expected"
}

Assert-Status '/tram-runs' 401
Assert-Status '/auth/me' 401
$login = Request POST '/auth/login' @{ username = 'admin'; password = 'admin123' }
Assert-True ($login.user.role -eq 'Admin') 'Admin role must serialize as a string'
$headers = @{ Authorization = "Bearer $($login.accessToken)" }
$operator = Request POST '/auth/login' @{ username = 'operator'; password = 'user123' }
Assert-True ($operator.user.role -eq 'User') 'Operator login failed'
$profile = Request GET '/auth/me' $null $headers
Assert-True ($profile.username -eq 'admin') 'Profile mismatch'
$runs = @(Request GET '/tram-runs' $null $headers)
Assert-True ($runs.Count -gt 0) 'No runs available for integration checks'
$demo = $runs | Where-Object { $_.telemetryPointsCount -gt 0 } | Select-Object -First 1
$telemetry = @(Request GET "/tram-runs/$($demo.id)/telemetry" $null $headers)
Assert-True ($telemetry.Count -gt 0) 'Demo telemetry is missing'
$demoRoute = Request GET "/tram-runs/$($demo.id)" $null $headers
Assert-True ($demoRoute.plannedRoute.Count -gt 20) 'Planned rail geometry is missing'
$status = Request GET '/model/status'
Assert-True $status.isLoaded 'Model is not loaded'

$created = Request POST '/tram-runs' @{ name = 'Smoke test'; description = 'Temporary integration test' } $headers
try {
    $updated = Request PUT "/tram-runs/$($created.id)" @{ name = 'Smoke test updated'; description = 'Updated' } $headers
    Assert-True ($updated.name -eq 'Smoke test updated') 'Run update failed'
    $input = @{ controllerHandlePosition = 0.5; frontBogieSpeed = 10.0; rearBogieSpeed = 10.0; timestamp = 2.0 }
    $point = $input.Clone(); $point.sequenceNumber = 0
    $point2 = $input.Clone(); $point2.sequenceNumber = 1; $point2.timestamp = 3.0
    $added = @(Request POST "/tram-runs/$($created.id)/telemetry/batch" @($point, $point2) $headers)
    Assert-True ($added.Count -eq 2) 'Telemetry batch failed'
    $loadedPoint = Request GET "/tram-runs/$($created.id)/telemetry/1" $null $headers
    Assert-True ($loadedPoint.timestamp -eq 3) 'Telemetry lookup failed'
    Assert-True ($loadedPoint.PSObject.Properties.Name -contains 'gpsSpeed') 'Absent GPS must serialize explicitly'
    $batch = @(Request POST '/model/predict/batch' @($input, $point2) $headers)
    Assert-True ($batch[0].predictedPosition -eq 0 -and [Math]::Abs($batch[1].predictedPosition - 10) -lt 0.001) 'Batch distance must accumulate from the first sample'
    $output = Request POST '/model/predict' $input $headers
    Assert-True ([Math]::Abs($output.predictedSpeed - 10) -lt 0.001) 'Prediction value mismatch'
    $metrics = Request POST '/model/metrics' @{ input = $input; output = $output; actualSpeed = 9; actualPosition = 19 } $headers
    Assert-True ($metrics.speedError -eq 1) 'Metrics calculation failed'
    $inference = Request POST '/model/inference' @{ tramRunId = $created.id; sequenceNumber = 0; input = $input } $headers
    $detail = Request GET "/model/inferences/$($inference.id)" $null $headers
    Assert-True ($detail.id -eq $inference.id) 'Inference detail failed'
    $history = @(Request GET "/model/inferences/run/$($created.id)" $null $headers)
    Assert-True ($history.Count -eq 1) 'Run inference history failed'
    $mine = @(Request GET '/model/inferences/user/me' $null $headers)
    Assert-True ($mine.id -contains $inference.id) 'User inference history failed'
    $adjusted = Request PUT "/model/inferences/$($inference.id)/intervene" @{ adjustedSpeed = 9; adjustedPosition = 19 } $headers
    Assert-True $adjusted.isUserIntervention 'Intervention failed'
    $actual = Request PUT "/model/inferences/$($inference.id)/actual" @{ actualSpeed = 9; actualPosition = 19 } $headers
    Assert-True ($actual.errorSpeed -eq 0) 'Actual values update failed'
} finally {
    Request DELETE "/tram-runs/$($created.id)" $null $headers | Out-Null
}
Assert-Status "/tram-runs/$($created.id)" 404 $headers
Write-Host 'PASS: authentication, roles, profile, demo data, CRUD, telemetry, prediction, metrics, inference, history, intervention and cleanup.'
