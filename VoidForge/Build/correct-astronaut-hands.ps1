param(
    [string]$ProjectRoot = (Join-Path $PSScriptRoot '..'),
    [string]$SourceRoot = $ProjectRoot,
    [string]$BlenderPath = 'C:/Program Files/Blender Foundation/Blender 5.2/blender.exe'
)
$ErrorActionPreference = 'Stop'
$project = (Resolve-Path -LiteralPath $ProjectRoot).Path
$source = (Resolve-Path -LiteralPath $SourceRoot).Path
$cache = Join-Path $project 'Library/AnatomicalHandsBlender'
$paths = @{
    BLENDER_USER_RESOURCES = "$cache/resources"
    BLENDER_USER_CONFIG = "$cache/resources/config"
    BLENDER_USER_SCRIPTS = "$cache/resources/scripts"
    BLENDER_USER_EXTENSIONS = "$cache/resources/extensions"
    TEMP = "$cache/temp"
    TMP = "$cache/temp"
}
$previous = @{}
try {
    foreach ($name in $paths.Keys) {
        $previous[$name] = [Environment]::GetEnvironmentVariable($name, 'Process')
        [IO.Directory]::CreateDirectory($paths[$name]) | Out-Null
        [Environment]::SetEnvironmentVariable($name, $paths[$name], 'Process')
    }
    [IO.Directory]::CreateDirectory("$project/Logs") | Out-Null
    $arguments = @('--background','--factory-startup','--offline-mode','--disable-autoexec','-t','4',
        '--python-exit-code','1','--python',"`"$PSScriptRoot/correct-astronaut-hands.py`"",'--',
        '--project-root',"`"$project`"",'--source-root',"`"$source`"")
    $process = Start-Process -FilePath $BlenderPath -ArgumentList $arguments -WindowStyle Hidden -PassThru -Wait `
        -RedirectStandardOutput "$project/Logs/anatomical-hands.log" -RedirectStandardError "$project/Logs/anatomical-hands-error.log"
    if ($process.ExitCode -ne 0) { throw "Correction locale des gants : code $($process.ExitCode), voir Logs/anatomical-hands.log." }
} finally {
    foreach ($name in $previous.Keys) { [Environment]::SetEnvironmentVariable($name, $previous[$name], 'Process') }
}
