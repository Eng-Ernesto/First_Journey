param(
    [Parameter(Mandatory=$true)][string]$State,
    [Parameter(Mandatory=$true)][string]$OriginalFile
)
$ErrorActionPreference='Stop'
$taskRoot='C:\Ernesto\unity\assets\Characters\HeroAJ'
$taskManifestPath=Join-Path $taskRoot 'mixamo_selection_v01.json'
$taskManifest=Get-Content -LiteralPath $taskManifestPath -Raw | ConvertFrom-Json
$taskEntry=@($taskManifest.entries | Where-Object state -eq $State)
if ($taskEntry.Count -ne 1) { throw 'Unknown or ambiguous animation state.' }
$taskEntry=$taskEntry[0]
$taskReport=Get-Content -LiteralPath "C:\Ernesto\unity\First_Journey\Docs\HeroAJ_${State}_Import.json" -Raw | ConvertFrom-Json
if (-not $taskReport.valid) { throw 'Unity import validation failed.' }
$taskOriginalHash=(Get-FileHash -LiteralPath $OriginalFile -Algorithm SHA256).Hash
$taskCopy=Join-Path $taskRoot "Animations\AJ_${State}.fbx"
$taskUnityCopy=Join-Path 'C:\Ernesto\unity\First_Journey' $taskReport.asset
if ($taskOriginalHash -ne $taskReport.sha256 -or $taskOriginalHash -ne (Get-FileHash -LiteralPath $taskCopy).Hash -or $taskOriginalHash -ne (Get-FileHash -LiteralPath $taskUnityCopy).Hash) {
    throw 'Source, organised copy and Unity copy must have identical hashes.'
}
$taskEntry.downloaded=$true
$taskEntry | Add-Member -NotePropertyName 'download_method' -NotePropertyValue 'manual_by_user' -Force
$taskEntry | Add-Member -NotePropertyName 'download_source' -NotePropertyValue $OriginalFile.Replace('\','/') -Force
$taskEntry | Add-Member -NotePropertyName 'download_file' -NotePropertyValue "Animations/AJ_${State}.fbx" -Force
$taskEntry | Add-Member -NotePropertyName 'sha256' -NotePropertyValue $taskOriginalHash -Force
$taskEntry | Add-Member -NotePropertyName 'unity_import' -NotePropertyValue $taskReport -Force
$taskDone=@($taskManifest.entries | Where-Object downloaded)
$taskPending=@($taskManifest.entries | Where-Object { -not $_.downloaded })
$taskNext=if ($taskPending.Count -gt 0) { $taskPending[0].state } else { $null }
$taskManifest.status=if ($taskPending.Count -gt 0) { 'manual_downloads_in_progress' } else { 'downloads_complete' }
$taskManifest | Add-Member -NotePropertyName 'downloaded_count' -NotePropertyValue $taskDone.Count -Force
$taskManifest | Add-Member -NotePropertyName 'pending_count' -NotePropertyValue $taskPending.Count -Force
$taskManifest | Add-Member -NotePropertyName 'next_manual_download' -NotePropertyValue $taskNext -Force
$taskEncoding=[System.Text.UTF8Encoding]::new($false)
[System.IO.File]::WriteAllText($taskManifestPath,($taskManifest | ConvertTo-Json -Depth 20),$taskEncoding)
$taskTitles=($taskDone | ForEach-Object title) -join ', '
$taskStatus="$($taskDone.Count) de $($taskManifest.entries.Count) animações descarregadas manualmente e importadas na Unity: $taskTitles. As cópias estão em ``Animations/AJ_<Estado>.fbx``. Faltam $($taskPending.Count) ficheiros."
$taskMdPath=Join-Path $taskRoot 'Selecao_Mixamo_AJ.md'
$taskMd=Get-Content -LiteralPath $taskMdPath -Raw
$taskMd=[regex]::Replace($taskMd,'(?m)^\*\*Estado:\*\*[^\r\n]*',"**Estado:** $taskStatus")
$taskMd=[regex]::Replace($taskMd,'(?m)^O modelo existente[^\r\n]*','O modelo existente em `C:\Users\User\Downloads\explorador.fbx` foi preservado, com cópia em `Source/explorador.fbx`. Os clips recebidos foram importados em `Assets/_FirstJourney/Art/Characters/HeroAJ/Animations/`, com rig Generic e sem malha. A configuração Humanoid, os loops e a ligação ao Avatar ficam para a integração. Ainda não há personagem ou Animator instalado na cena.')
[System.IO.File]::WriteAllText($taskMdPath,$taskMd,$taskEncoding)
$taskHtmlPath=Join-Path $taskRoot 'Selecao_Mixamo_AJ.html'
$taskHtml=Get-Content -LiteralPath $taskHtmlPath -Raw
$taskHtmlStatus="<p class=`"status`"><b>$($taskDone.Count) de $($taskManifest.entries.Count) animações descarregadas e importadas.</b><br>$taskTitles. Faltam $($taskPending.Count) ficheiros; a ligação ao Avatar fica para a integração.</p>"
$taskHtml=[regex]::Replace($taskHtml,'<p class="status">.*?</p>',$taskHtmlStatus)
$taskGroup=[regex]::Escape($taskEntry.group)
$taskState=[regex]::Escape($State)
$taskHtml=[regex]::Replace($taskHtml,"<small>$taskGroup · $taskState(?: · FBX confirmado)?</small>","<small>$($taskEntry.group) · $State · FBX confirmado</small>")
[System.IO.File]::WriteAllText($taskHtmlPath,$taskHtml,$taskEncoding)
[pscustomobject]@{State=$State;Downloaded=$taskDone.Count;Pending=$taskPending.Count;Next=$taskNext;FPS=$taskReport.firstClipFrameRate;Seconds=$taskReport.firstClipSeconds;HashVerified=$true} | ConvertTo-Json
