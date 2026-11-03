# Prepare la piste futuriste existante, sans selection runtime ni copie de scripts tiers.
# Pierre a ecarte la pioche et la scie le 3 novembre 2026 : ne plus les importer.
param(
    [string] $LibraryRoot = 'G:\UnityAssets',
    [string] $MyrinoxRoot = 'D:\Projets\Trement\MyrinoxPrime',
    [string] $ProjectRoot = (Join-Path $PSScriptRoot '..')
)
$ErrorActionPreference = 'Stop'
$project = (Resolve-Path -LiteralPath $ProjectRoot).Path
if (-not (Test-Path -LiteralPath (Join-Path $project 'Packages\manifest.json'))) { throw 'Projet Unity attendu.' }
$targetRoot = Join-Path $project 'Assets\ThirdParty\ToolAssets\Candidates'
$toon = Join-Path $LibraryRoot 'asset_manager\store\Toon_Soldiers\Files\Toon_Soldiers\ToonSoldiers_2'
$welder = Join-Path $MyrinoxRoot 'docs\production\ModularFleetV1\motion-parts\UT-04\Meshy'
$taskId = '01a122ef-3166-71d3-8e51-1c936c4509cd'
$entries = @()
function Add-VisualFile([IO.FileInfo] $File, [string] $Target) {
    if ($File.Extension -notin @('.fbx', '.png', '.jpg', '.tga', '.mat', '.prefab', '.meta')) {
        throw "Fichier non visuel : $($File.FullName)"
    }
    $absolute = [IO.Path]::GetFullPath($Target)
    if (-not $absolute.StartsWith($project.TrimEnd('\') + '\', [StringComparison]::OrdinalIgnoreCase)) {
        throw 'Destination hors projet.'
    }
    [pscustomobject]@{ Source = $File.FullName; Destination = $absolute; Sha256 = (Get-FileHash -LiteralPath $File.FullName).Hash }
}
foreach ($file in Get-ChildItem -LiteralPath $welder -File | Where-Object Name -Like "$taskId*") {
    $entries += Add-VisualFile $file (Join-Path $targetRoot "WelderHead\$($file.Name)")
}
if (-not @($entries | Where-Object Source -Like "*$taskId.fbx").Count) { throw 'FBX soudeuse absent.' }
foreach ($relative in @('models\weapons\weapon_welder.FBX', 'models\weapons\weapon_welder.FBX.meta',
                        'models\Materials\ToonSoldiers_weapons.mat', 'models\Materials\ToonSoldiers_weapons.mat.meta',
                        'models\Materials\textures\TS_weapons_texture.tga', 'models\Materials\textures\TS_weapons_texture.tga.meta')) {
    $entries += Add-VisualFile (Get-Item -LiteralPath (Join-Path $toon $relative)) (Join-Path $targetRoot "ToonWelder\$relative")
}
foreach ($entry in $entries) {
    if ((Test-Path -LiteralPath $entry.Destination) -and (Get-FileHash -LiteralPath $entry.Destination).Hash -ne $entry.Sha256) {
        throw "Copie locale differente, ne pas ecraser : $($entry.Destination)"
    }
}
foreach ($entry in $entries) {
    [IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($entry.Destination)) | Out-Null
    if (-not (Test-Path -LiteralPath $entry.Destination)) { Copy-Item -LiteralPath $entry.Source -Destination $entry.Destination }
    if ((Get-FileHash -LiteralPath $entry.Destination).Hash -ne $entry.Sha256) { throw "Copie incorrecte : $($entry.Source)" }
}
$entries | ConvertTo-Json -Depth 3 | Set-Content -LiteralPath (Join-Path $targetRoot 'futuristic-copy-report.json') -Encoding utf8
Write-Output "Pistes de soudage : $($entries.Count) fichiers verifies. UT-04-WELDER et Toon Soldiers ; adaptation aux mains/URP a faire. Minage a main et meuleuse restent a trouver."
