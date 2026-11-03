# Relit les six exports dans un Blender distinct avec un profil entierement isole.
param(
    [string] $BlenderPath = 'C:\Program Files\Blender Foundation\Blender 5.2\blender.exe',
    [string] $ProjectRoot = (Join-Path $PSScriptRoot '..')
)
$ErrorActionPreference = 'Stop'
$project = (Resolve-Path -LiteralPath $ProjectRoot).Path
if (-not (Test-Path -LiteralPath (Join-Path $project 'Packages\manifest.json'))) {
    throw 'Projet Unity VoidForge attendu.'
}
if (-not (Test-Path -LiteralPath $BlenderPath)) { throw "Blender introuvable : $BlenderPath" }
$cache = Join-Path $project 'Library\HandToolExportValidation'
$logs = Join-Path $project 'Logs'
$report = Join-Path $project 'Art\HandTools\Validation\export-readback.json'
$blenderProfilePaths = @{
    BLENDER_USER_RESOURCES = (Join-Path $cache 'resources')
    BLENDER_USER_CONFIG = (Join-Path $cache 'resources\config')
    BLENDER_USER_SCRIPTS = (Join-Path $cache 'resources\scripts')
    BLENDER_USER_EXTENSIONS = (Join-Path $cache 'resources\extensions')
    TEMP = (Join-Path $cache 'temp')
    TMP = (Join-Path $cache 'temp')
}
$previous = @{}
try {
    foreach ($name in $blenderProfilePaths.Keys) {
        $previous[$name] = [Environment]::GetEnvironmentVariable($name, 'Process')
        [IO.Directory]::CreateDirectory($blenderProfilePaths[$name]) | Out-Null
        [Environment]::SetEnvironmentVariable($name, $blenderProfilePaths[$name], 'Process')
    }
    [IO.Directory]::CreateDirectory($logs) | Out-Null
    $script = Join-Path $PSScriptRoot 'check-hand-tool-exports.py'
    $arguments = @('--background', '--factory-startup', '--offline-mode', '--disable-autoexec',
        '-t', '4', '--python-exit-code', '1', '--python', "`"$script`"", '--',
        '--project-root', "`"$project`"", '--report', "`"$report`"")
    $process = Start-Process -FilePath $BlenderPath -ArgumentList $arguments -WindowStyle Hidden `
        -PassThru -Wait -RedirectStandardOutput (Join-Path $logs 'hand-tool-readback.log') `
        -RedirectStandardError (Join-Path $logs 'hand-tool-readback-error.log')
    if ($process.ExitCode -ne 0) {
        throw "Reimportation echouee (code $($process.ExitCode)) : Logs/hand-tool-readback.log et $report"
    }
    $result = Get-Content -LiteralPath $report -Raw | ConvertFrom-Json
    if (-not $result.success) { throw "Verification echouee : $report" }
    Write-Output "Six exports relus : $report"
} finally {
    foreach ($name in $previous.Keys) {
        [Environment]::SetEnvironmentVariable($name, $previous[$name], 'Process')
    }
}
