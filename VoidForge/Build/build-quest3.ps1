# Build de l'APK Quest 3 en ligne de commande (Windows PowerShell).
# Prérequis : Unity 6.3 LTS avec le module Android (SDK, NDK, OpenJDK) installé par Unity Hub.
# Usage : .\Build\build-quest3.ps1 -UnityPath "C:\Program Files\Unity\Hub\Editor\6000.3.0f1\Editor\Unity.exe" [-Dev] [-Output Builds\VoidForge-Quest3.apk]
param(
    [Parameter(Mandatory = $true)] [string] $UnityPath,
    [switch] $Dev,
    [string] $Output = "Builds\VoidForge-Quest3.apk"
)

$ErrorActionPreference = "Stop"
$ProjectDir = Resolve-Path (Join-Path $PSScriptRoot "..")
New-Item -ItemType Directory -Force -Path (Join-Path $ProjectDir "Logs") | Out-Null

if ($Dev -and $Output -eq "Builds\VoidForge-Quest3.apk") { $Output = "Builds\VoidForge-Quest3-dev.apk" }

$args = @(
    "-batchmode", "-nographics", "-quit",
    "-projectPath", "$ProjectDir",
    "-buildTarget", "Android",
    "-executeMethod", "VoidForge.Editor.QuestBuildPipeline.BuildBatch",
    "-vfOutput", $Output,
    "-logFile", (Join-Path $ProjectDir "Logs\build-quest3.log")
)
if ($Dev) { $args += "-vfDev" }

& $UnityPath @args
if ($LASTEXITCODE -ne 0) { throw "Build Unity en échec (code $LASTEXITCODE). Voir Logs\build-quest3.log." }
Write-Host "APK : $(Join-Path $ProjectDir $Output)"
