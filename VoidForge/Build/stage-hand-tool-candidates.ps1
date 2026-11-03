# Prepare des candidats visuels locaux, sans selection runtime ni copie de scripts tiers.
param(
    [string] $LibraryRoot = 'G:\UnityAssets',
    [string] $MyrinoxRoot = 'D:\Projets\Trement\MyrinoxPrime',
    [string] $ProjectRoot = (Join-Path $PSScriptRoot '..')
)
$ErrorActionPreference = 'Stop'
$project = (Resolve-Path -LiteralPath $ProjectRoot).Path
if (-not (Test-Path -LiteralPath (Join-Path $project 'Packages\manifest.json'))) { throw 'Projet Unity attendu.' }
$targetRoot = Join-Path $project 'Assets\ThirdParty\ToolAssets\Candidates'
$pickaxe = Join-Path $LibraryRoot 'asset_manager\store\Survival_Template_PRO\Files\PolymindGames\STP\Art\Models\Wieldables\SteelPickaxe'
$saw = Join-Path $LibraryRoot 'asset_manager\store\Simple_Apocalypse_-_Cartoon_Assets\Files\SimpleApocalypse'
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
foreach ($file in Get-ChildItem -LiteralPath $pickaxe -Recurse -File) {
    $relative = $file.FullName.Substring($pickaxe.Length + 1)
    $entries += Add-VisualFile $file (Join-Path $targetRoot "MiningPickaxe\$relative")
}
foreach ($relative in @('Models\Weapons\SA_Wep_CircularSaw.fbx', 'Models\Weapons\SA_Wep_CircularSaw.fbx.meta',
                        'Prefabs\Weapons\SA_Wep_CircularSaw.prefab', 'Prefabs\Weapons\SA_Wep_CircularSaw.prefab.meta')) {
    $entries += Add-VisualFile (Get-Item -LiteralPath (Join-Path $saw $relative)) (Join-Path $targetRoot "CuttingSaw\$relative")
}
foreach ($relative in @('Materials\SimpleApocalypse.mat', 'Materials\SimpleApocalypse.mat.meta',
                        'Textures\SimpleApocalypse_Texture.png', 'Textures\SimpleApocalypse_Texture.png.meta',
                        'Textures\SimpleApocalypse_Texture_2048.png', 'Textures\SimpleApocalypse_Texture_2048.png.meta')) {
    $entries += Add-VisualFile (Get-Item -LiteralPath (Join-Path $saw $relative)) (Join-Path $targetRoot "CuttingSaw\$relative")
}
foreach ($file in Get-ChildItem -LiteralPath $welder -File | Where-Object Name -Like "$taskId*") {
    $entries += Add-VisualFile $file (Join-Path $targetRoot "WelderHead\$($file.Name)")
}
if (-not @($entries | Where-Object Source -Like "*$taskId.fbx").Count) { throw 'FBX soudeuse absent.' }
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
$entries | ConvertTo-Json -Depth 3 | Set-Content -LiteralPath (Join-Path $targetRoot 'copy-report.json') -Encoding utf8
Write-Output "Candidats : $($entries.Count) fichiers verifies. Pioche, scie de decoupe et tete de soudeuse ; adaptation aux mains/URP a faire."
