param(
    [string]$ProjectPath = (Split-Path -Parent $PSScriptRoot),
    [string]$UnityCli = "$env:LOCALAPPDATA\Unity\bin\unity.exe"
)
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$ProjectPath = (Resolve-Path -LiteralPath $ProjectPath).Path
if (-not (Test-Path -LiteralPath $UnityCli)) { throw "Unity CLI not found: $UnityCli" }
$taskResults = Join-Path $ProjectPath 'TestResults'
New-Item -ItemType Directory -Path $taskResults -Force | Out-Null
$taskStarted = [DateTime]::UtcNow
$taskSteps = [System.Collections.Generic.List[object]]::new()
$taskReport = Join-Path $ProjectPath 'Docs/Regressao_Ultima_Execucao.json'
$taskPlaying = $false
$taskFailure = $null

function Invoke-FJCommand {
    param([string[]]$CommandArgs, [int]$TimeoutSeconds = 60)
    $taskRaw = & $UnityCli command @CommandArgs --timeout $TimeoutSeconds --caller plugin --skill unity-cli --project-path $ProjectPath --format json 2> (Join-Path $taskResults 'cli-stderr.txt')
    $taskExit = $LASTEXITCODE
    $taskJson = $taskRaw -join "`n"
    $taskJson | Set-Content -LiteralPath (Join-Path $taskResults 'last-command.json') -Encoding utf8
    if ($taskExit -ne 0) { throw "Unity CLI exited ${taskExit}: $taskJson" }
    $taskReply = $taskJson | ConvertFrom-Json
    if (-not $taskReply.success) { throw "Unity command failed: $taskJson" }
    $taskPayload = $taskReply.data.result
    if ($null -ne $taskPayload -and $taskPayload.PSObject.Properties.Name -contains 'success') {
        if (-not $taskPayload.success) { throw "Unity script failed: $taskJson" }
        return $taskPayload.result
    }
    return $taskPayload
}

function Invoke-FJScenario {
    param([string]$Name, [string]$File, [string]$Entry, [int]$Seconds = 60)
    Write-Host "Running: $Name"
    $taskData = Invoke-FJCommand -CommandArgs @('run_script','--file',$File,'--entry',$Entry,'--timeout_ms',([string]($Seconds*1000))) -TimeoutSeconds ($Seconds+20)
    if ($taskData.PSObject.Properties.Name -contains 'passed' -and -not $taskData.passed) { throw "$Name returned passed=false" }
    $taskSteps.Add([pscustomobject]@{name=$Name;passed=$true;result=$taskData})
    $taskData | ConvertTo-Json -Depth 30 | Set-Content -LiteralPath (Join-Path $taskResults "$Name.json") -Encoding utf8
    Write-Host "Passed: $Name"
}

Push-Location -LiteralPath $ProjectPath
try {
    # Do not preserve transient test/Play state as the authored scene state.
    Invoke-FJCommand -CommandArgs @('editor_stop') | Out-Null
    Invoke-FJScenario 'SaveProject' 'AgentScripts/SessionClosure.cs' 'SessionClosure.Save'
    Invoke-FJScenario 'Humanoid17Clips' 'AgentScripts/YoungPilotAnimationValidation.cs' 'YoungPilotAnimationValidation.ValidateAll'
    Invoke-FJScenario 'ColdStart' 'AgentScripts/SharedExteriorPlaytest.cs' 'SharedExteriorPlaytest.ColdStart'
    Invoke-FJCommand -CommandArgs @('editor_play') | Out-Null
    $taskPlaying = $true
    # Discovery can change ports during the Play domain reload. Retry readiness only;
    # never dispatch a scenario a second time after a timeout.
    $taskReady = $false
    $taskDeadline = [DateTime]::UtcNow.AddSeconds(60)
    while (-not $taskReady -and [DateTime]::UtcNow -lt $taskDeadline) {
        try {
            $taskReady = [bool](Invoke-FJCommand -CommandArgs @('eval','--code','return UnityEngine.Application.isPlaying;') -TimeoutSeconds 10)
        } catch { $taskReady = $false }
        if (-not $taskReady) { Start-Sleep -Milliseconds 500 }
    }
    if (-not $taskReady) { throw 'Editor did not enter a ready Play session within 60 seconds.' }
    Invoke-FJScenario 'PreparePlay' 'AgentScripts/SessionClosure.cs' 'SessionClosure.PreparePlay'
    Invoke-FJScenario 'PlayFramesAdvance' 'AgentScripts/AutomationReadinessTest.cs' 'AutomationReadinessTest.FramesAdvance'
    Invoke-FJScenario 'AircraftJourney' 'AgentScripts/AircraftPlaytest.cs' 'AircraftPlaytest.Journey' 300
    Invoke-FJScenario 'ThreeReturnDoors' 'AgentScripts/SharedExteriorPlaytest.cs' 'SharedExteriorPlaytest.RoundTrips' 180
    Invoke-FJCommand -CommandArgs @('editor_stop') | Out-Null
    $taskPlaying = $false
    Invoke-FJScenario 'SavedSceneIntegrity' 'AgentScripts/AircraftReview.cs' 'AircraftReview.ValidateSaved'
} catch {
    $taskFailure = $_.Exception.Message
    Write-Warning $taskFailure
} finally {
    if ($taskPlaying) {
        try { Invoke-FJCommand -CommandArgs @('editor_stop') | Out-Null } catch { Write-Warning "Could not stop Play: $_" }
    }
    $taskSummary = [ordered]@{
        passed=($null -eq $taskFailure)
        startedUtc=$taskStarted.ToString('o')
        finishedUtc=[DateTime]::UtcNow.ToString('o')
        unityEditor='6000.3.23f1'
        execution='Unity CLI/Pipeline against the open Editor'
        physicalControllerTested=$false
        standaloneBuildTested=$false
        scenarios=$taskSteps.ToArray()
        error=$taskFailure
    }
    $taskSummary | ConvertTo-Json -Depth 40 | Set-Content -LiteralPath $taskReport -Encoding utf8
    Pop-Location
}
if ($null -ne $taskFailure) { throw "First Journey regression failed. See ${taskReport}. $taskFailure" }
Write-Host "First Journey regression passed. Report: $taskReport"
