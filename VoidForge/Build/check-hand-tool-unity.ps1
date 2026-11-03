# Importe seulement les outils et ce validateur dans un projet Unity jetable sous Library.
param(
    [string] $UnityPath = 'D:\Unity\6000.4.0f1\Editor\Unity.exe',
    [string] $ProjectRoot = (Join-Path $PSScriptRoot '..')
)
$ErrorActionPreference = 'Stop'
$project = (Resolve-Path -LiteralPath $ProjectRoot).Path
$validation = Join-Path $project 'Library\HandToolUnityValidation'
$source = Join-Path $project 'Assets\VoidForge\Art\HandTools'
$target = Join-Path $validation 'Assets\VoidForge\Art\HandTools'
$report = Join-Path $project 'Art\HandTools\Validation\unity-import.json'
function Get-NormalizedMeta([string] $Path) {
    [regex]::Replace([IO.File]::ReadAllText($Path).Replace("`r`n", "`n"), '(?m)[ \t]+$', '')
}
if (-not (Test-Path -LiteralPath $UnityPath)) { throw "Unity introuvable : $UnityPath" }
foreach ($dir in @('Assets\Editor','Packages','ProjectSettings')) {
    [IO.Directory]::CreateDirectory((Join-Path $validation $dir)) | Out-Null
}
Copy-Item -LiteralPath (Join-Path $project 'ProjectSettings\ProjectVersion.txt') -Destination (Join-Path $validation 'ProjectSettings\ProjectVersion.txt')
@{ dependencies = @{ 'com.unity.modules.animation'='1.0.0'; 'com.unity.modules.jsonserialize'='1.0.0' } } |
    ConvertTo-Json | Set-Content -LiteralPath (Join-Path $validation 'Packages\manifest.json') -Encoding utf8
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'HandToolImportCheck.cs') -Destination (Join-Path $validation 'Assets\Editor\HandToolImportCheck.cs')
foreach ($file in Get-ChildItem -LiteralPath $source -Recurse -File | Where-Object Extension -In @('.fbx','.glb','.meta')) {
    $destination = Join-Path $target $file.FullName.Substring($source.Length + 1)
    [IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($destination)) | Out-Null
    Copy-Item -LiteralPath $file.FullName -Destination $destination
}
[IO.Directory]::CreateDirectory((Join-Path $project 'Logs')) | Out-Null
[IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($report)) | Out-Null
$variables = @{
    BEE_BUILD_THREADS='4'
    UPM_CACHE_ROOT=(Join-Path $validation 'UpmCache')
    GRADLE_USER_HOME=(Join-Path $project 'Library\GradleUserHome')
}
$previous = @{}
try {
    foreach ($name in $variables.Keys) {
        $previous[$name] = [Environment]::GetEnvironmentVariable($name, 'Process')
        [Environment]::SetEnvironmentVariable($name, $variables[$name], 'Process')
    }
    $arguments = @('-batchmode','-nographics','-quit','-projectPath',"`"$validation`"",
        '-executeMethod','HandToolImportCheck.Run','-vfHandToolReport',"`"$report`"",
        '-logFile',"`"$(Join-Path $project 'Logs\hand-tool-unity.log')`"")
    $process = Start-Process -FilePath $UnityPath -ArgumentList $arguments -WindowStyle Hidden -PassThru -Wait
    if ($process.ExitCode -ne 0) { throw "Import Unity echoue (code $($process.ExitCode)), voir Logs/hand-tool-unity.log" }
    $result = Get-Content -LiteralPath $report -Raw | ConvertFrom-Json
    if (-not $result.success) { throw "Import Unity incomplet : $report" }
    # Preserve existing GUIDs. Never replace a different metadata file.
    $metaFiles = @(Get-ChildItem -LiteralPath $target -Recurse -File -Filter '*.meta')
    $metaFiles += Get-Item -LiteralPath "$target.meta",(Join-Path $validation 'Assets\VoidForge\Art.meta')
    $entries = @($metaFiles | ForEach-Object {
        $relative = $_.FullName.Substring((Join-Path $validation 'Assets').Length + 1)
        [pscustomobject]@{ Source=$_.FullName; Destination=(Join-Path $project "Assets\$relative") }
    })
    foreach ($entry in $entries) {
        if ((Test-Path -LiteralPath $entry.Destination) -and
            (Get-NormalizedMeta $entry.Source) -cne (Get-NormalizedMeta $entry.Destination)) {
            throw "Meta local different, ne pas ecraser : $($entry.Destination)"
        }
    }
    foreach ($entry in $entries) {
        if (-not (Test-Path -LiteralPath $entry.Destination)) {
            [IO.File]::WriteAllText($entry.Destination, (Get-NormalizedMeta $entry.Source))
        }
    }
    Write-Output "Import Unity reussi : 3 FBX, $($entries.Count) fichiers .meta conserves. $report"
} finally {
    foreach ($name in $previous.Keys) { [Environment]::SetEnvironmentVariable($name, $previous[$name], 'Process') }
}
