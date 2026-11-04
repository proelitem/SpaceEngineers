param(
    [Parameter(Mandatory=$true)][ValidatePattern('^[A-Za-z0-9._:-]+$')][string]$Serial,
    [string]$Apk=(Join-Path $PSScriptRoot '../Builds/VoidForge-Quest3-integration03-dev.apk'),
    [string]$UnityEditor='D:/Unity/6000.4.0f1/Editor',
    [switch]$Install,
    [switch]$Restart
)
$ErrorActionPreference='Stop'
$projectRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$sdk=Join-Path $UnityEditor 'Data/PlaybackEngines/AndroidPlayer/SDK'
$adb=Join-Path $sdk 'platform-tools/adb.exe'
$aapt=Join-Path $sdk 'build-tools/36.0.0/aapt.exe'
$package='com.proelitem.voidforge'
$stamp=Get-Date -Format 'yyyyMMdd-HHmmss'
$logs=Join-Path $projectRoot 'Logs'
$evidence=Join-Path $projectRoot 'Hermes_Works/Preuves/INTEGRATION_03'
[IO.Directory]::CreateDirectory($logs)|Out-Null
[IO.Directory]::CreateDirectory($evidence)|Out-Null
function Invoke-QuestAdb([string[]]$Arguments) {
    $result=& $adb -s $Serial @Arguments 2>&1
    if($LASTEXITCODE -ne 0){throw "adb $($Arguments -join ' ') : $($result -join "`n")"}
    return ($result -join "`n")
}
function Read-World {
    # The shell can read this app's external files; run-as lacks the FUSE group on Quest.
    $path="/sdcard/Android/data/$package/files/survival-world-v1.json"
    $text=& $adb -s $Serial exec-out cat $path 2>&1
    if($LASTEXITCODE -ne 0){return $null}
    try{return ($text -join "`n")|ConvertFrom-Json}catch{return $null}
}
function Read-SharedLog([string]$Path) {
    $stream=[IO.FileStream]::new($Path,[IO.FileMode]::Open,[IO.FileAccess]::Read,
        ([IO.FileShare]::ReadWrite -bor [IO.FileShare]::Delete))
    $reader=[IO.StreamReader]::new($stream,[Text.Encoding]::UTF8,$true)
    try { return $reader.ReadToEnd() } finally { $reader.Dispose() }
}
function Capture-Startup([string]$Name) {
    $prefix=Join-Path $logs "quest-survival-$stamp-$Name"
    Invoke-QuestAdb @('logcat','-c')|Out-Null
    $logger=Start-Process -FilePath $adb -ArgumentList @('-s',$Serial,'logcat','-v','threadtime','-s','Unity','AndroidRuntime') `
        -WindowStyle Hidden -PassThru -RedirectStandardOutput "$prefix-live.log" -RedirectStandardError "$prefix-logger.log"
    try {
        Invoke-QuestAdb @('shell','input','keyevent','KEYCODE_WAKEUP')|Out-Null
        Invoke-QuestAdb @('shell','am','force-stop',$package)|Out-Null
        $launch=Invoke-QuestAdb @('shell','am','start','-W','-n',"$package/com.unity3d.player.UnityPlayerGameActivity")
        [IO.File]::WriteAllText("$prefix-launch.log",$launch)
        if($launch -notmatch 'Status:\s*ok'){throw "Launch failed: $launch"}
        $appPid=(Invoke-QuestAdb @('shell','pidof',$package)).Trim()
        if($appPid -notmatch '^\d+$'){throw "Expected one application PID, received $appPid"}
        $timer=[Diagnostics.Stopwatch]::StartNew()
        while($timer.Elapsed.TotalSeconds -lt 35){
            Invoke-QuestAdb @('shell','input','keyevent','KEYCODE_WAKEUP')|Out-Null
            Start-Sleep -Milliseconds 2000
        }
        $timer.Stop()
        $dump=Invoke-QuestAdb @('logcat','-d','-v','threadtime','-s','Unity','AndroidRuntime')
        [IO.File]::WriteAllText("$prefix-dump.log",$dump)
        $alive=(Invoke-QuestAdb @('shell','pidof',$package)).Trim() -eq $appPid
    } finally {
        if($logger -and -not $logger.HasExited){Stop-Process -Id $logger.Id;Wait-Process -Id $logger.Id -ErrorAction SilentlyContinue}
    }
    $lines=@(((Read-SharedLog "$prefix-live.log")+"`n"+$dump) -split "`r?`n" |
        Where-Object {$_ -match "^\d\d-\d\d\s+[\d:.]+\s+$appPid\s+"} | Select-Object -Unique)
    [IO.File]::WriteAllLines("$prefix-unity.log",$lines)
    $text=$lines -join "`n"
    $faults=@($lines|Where-Object {$_ -match '(?i)Exception|XR_ERROR|\s[EF]\s+(Unity|AndroidRuntime)\s*:'})
    $markers=@{
        equipment=$text.Contains('XR equipment bound')
        hud=$text.Contains('Five native 2D views attached')
        asteroids=$text.Contains('64 asset-shaped asteroids registered')
        workshop=$text.Contains('Four-block starter base, blueprint/welding loop and unified solo save ready')
        voxel=$text.Contains('Asteroid 0 voxel collider ready')
    }
    $ready=@($markers.Values|Where-Object {-not $_}).Count -eq 0
    return [pscustomobject]@{name=$Name;pid=$appPid;seconds=$timer.Elapsed.TotalSeconds;alive=$alive;markers=$markers;
        exceptionsOrErrors=$faults;success=($alive -and $ready -and $faults.Count -eq 0);log="$prefix-unity.log"}
}
$report=[ordered]@{success=$false;serial=$Serial;headsetWorn=$false;apk=$null;install=$false;captures=@();saveReadable=$false;devicePersistenceVerified=$false}
$restoreBuffer=$null
try {
    Invoke-QuestAdb @('get-state')|Out-Null
    # OpenXR startup diagnostics can overflow the stock 256 KiB ring before the
    # Windows reader catches up. Temporarily enlarge main, then restore its size.
    $buffer=Invoke-QuestAdb @('logcat','-b','main','-g')
    if($buffer -notmatch 'ring buffer is (\d+) (KiB|MiB)'){throw "Unknown log buffer size: $buffer"}
    $restoreBuffer=$Matches[1]+$(if($Matches[2] -eq 'MiB'){'M'}else{'K'})
    Invoke-QuestAdb @('logcat','-b','main','-G','4M')|Out-Null
    $report.logBufferOriginal=$restoreBuffer
    $report.logBufferDuringCapture='4M'
    if($Install){
        $resolved=(Resolve-Path -LiteralPath $Apk).Path
        if(-not $resolved.StartsWith((Join-Path $projectRoot 'Builds')+[IO.Path]::DirectorySeparatorChar,[StringComparison]::OrdinalIgnoreCase)){throw 'APK must be under this project Builds directory.'}
        $badging=& $aapt dump badging $resolved
        if($LASTEXITCODE -ne 0 -or ($badging -join "`n") -notmatch "package: name='$package'"){throw 'APK package does not match VoidForge.'}
        $report.apk=@{path=$resolved;sha256=(Get-FileHash -LiteralPath $resolved -Algorithm SHA256).Hash;bytes=(Get-Item -LiteralPath $resolved).Length;package=($badging|Select-Object -First 1)}
        $installed=Invoke-QuestAdb @('install','-r',$resolved)
        [IO.File]::WriteAllText((Join-Path $logs "quest-survival-$stamp-install.log"),$installed)
        if($installed -notmatch 'Success'){throw "Install failed: $installed"}
        $report.install=$true
    }
    $report.captures+=Capture-Startup 'cold'
    $first=Read-World
    $report.saveReadable=$null -ne $first
    if($Restart){
        $report.captures+=Capture-Startup 'restart'
        $second=Read-World
        if($null -ne $first -and $null -ne $second){
            $one=$first.ledger|ConvertFrom-Json;$two=$second.ledger|ConvertFrom-Json
            $firstStock=$one|Select-Object blocks,backpack,cargo|ConvertTo-Json -Depth 16 -Compress
            $secondStock=$two|Select-Object blocks,backpack,cargo|ConvertTo-Json -Depth 16 -Compress
            $report.devicePersistenceVerified=($firstStock -eq $secondStock -and $one.blocks.Count -eq 8)
            $report.savedBlocks=$two.blocks.Count
            $report.oreProfile=$second.oreProfile
        }
    }
    $report.success=(@($report.captures|Where-Object {-not $_.success}).Count -eq 0) -and
        (-not $Restart -or $report.devicePersistenceVerified)
} catch {
    $report.error=$_.Exception.Message
} finally {
    if($restoreBuffer){
        try {Invoke-QuestAdb @('logcat','-b','main','-G',$restoreBuffer)|Out-Null;$report.logBufferRestored=$true}
        catch {$report.logBufferRestored=$false;$report.success=$false;$report.bufferRestoreError=$_.Exception.Message}
    }
    $json=$report|ConvertTo-Json -Depth 16
    [IO.File]::WriteAllText((Join-Path $evidence 'quest-validation.json'),$json,[Text.UTF8Encoding]::new($false))
    Write-Output $json
}
if(-not $report.success){exit 1}
