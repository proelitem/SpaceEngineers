# Build de l'APK Quest 3 en ligne de commande (Windows PowerShell).
# Prérequis : Unity 6000.4.0f1 avec le module Android (SDK, NDK, OpenJDK) installé par Unity Hub.
# Usage : .\Build\build-quest3.ps1 -UnityPath "D:\Unity\6000.4.0f1\Editor\Unity.exe" [-Dev] [-Output Builds\VoidForge-Quest3.apk]
param(
    [Parameter(Mandatory = $true)] [string] $UnityPath,
    [switch] $Dev,
    [switch] $IntegrateAssets,
    [string] $Output = "Builds\VoidForge-Quest3.apk"
)

$ErrorActionPreference = "Stop"
$ProjectDir = Resolve-Path (Join-Path $PSScriptRoot "..")
New-Item -ItemType Directory -Force -Path (Join-Path $ProjectDir "Logs") | Out-Null
$libraryRoot = if ($env:VF_UNITY_ASSETS_ROOT) { $env:VF_UNITY_ASSETS_ROOT } else { 'G:\UnityAssets' }
if (-not (Test-Path -LiteralPath (Join-Path $ProjectDir 'Assets\csc.rsp')) -and
    (Test-Path -LiteralPath (Join-Path $libraryRoot 'asset_manager\store\Digger_PRO_Voxel_Terrain_Sculpting'))) {
    & (Join-Path $PSScriptRoot 'import-local-assets.ps1') -LibraryRoot $libraryRoot
}

if ($Dev -and $Output -eq "Builds\VoidForge-Quest3.apk") { $Output = "Builds\VoidForge-Quest3-dev.apk" }
$buildMethod = if ($IntegrateAssets) { "VoidForge.Editor.IntegrationWorkshop.BuildQuestBatch" } else { "VoidForge.Editor.QuestBuildPipeline.BuildBatch" }

$args = @(
    "-batchmode", "-nographics", "-quit",
    "-projectPath", "$ProjectDir",
    "-buildTarget", "Android",
    "-executeMethod", $buildMethod,
    "-vfOutput", $Output,
    "-logFile", (Join-Path $ProjectDir "Logs\build-quest3.log")
)
if ($Dev) { $args += "-vfDev" }

# Évite d'hériter du cache Gradle d'un autre projet présent dans l'environnement.
$previousGradleUserHome = $env:GRADLE_USER_HOME
$previousBeeBuildThreads = $env:BEE_BUILD_THREADS
try {
    $env:GRADLE_USER_HOME = Join-Path $ProjectDir "Library\GradleUserHome"
    $env:BEE_BUILD_THREADS = '4'
    # Unity.exe est une application graphique : attendre explicitement sa fin sous PowerShell.
    $quotedArgs = $args | ForEach-Object { '"' + $_.Replace('"', '\"') + '"' }
    $unityProcess = Start-Process -FilePath $UnityPath -ArgumentList $quotedArgs -WindowStyle Hidden -Wait -PassThru
    $unityExitCode = $unityProcess.ExitCode
}
finally {
    $env:GRADLE_USER_HOME = $previousGradleUserHome
    $env:BEE_BUILD_THREADS = $previousBeeBuildThreads
}
if ($unityExitCode -ne 0) { throw "Build Unity en échec (code $unityExitCode). Voir Logs\build-quest3.log." }
Write-Host "APK : $(Join-Path $ProjectDir $Output)"
