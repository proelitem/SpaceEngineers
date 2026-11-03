# Importe uniquement le paquet visuel autonome DRILL-R de MyrinoxPrime.
# Les sources restent intactes ; la copie locale sous Assets/ThirdParty est ignoree par Git.
param(
    [string] $MyrinoxProject = 'D:\Projets\Trement\MyrinoxPrime\ArgosWorkshop',
    [string] $ProjectRoot = (Join-Path $PSScriptRoot '..')
)
$ErrorActionPreference = 'Stop'
$project = (Resolve-Path -LiteralPath $ProjectRoot).Path
if (-not (Test-Path -LiteralPath (Join-Path $project 'AGENTS.md')) -or
    -not (Test-Path -LiteralPath (Join-Path $project 'Packages\manifest.json'))) {
    throw 'Projet VoidForge attendu.'
}
$source = Join-Path $MyrinoxProject 'Assets\MyrinoxPrime\Resources\Campaign\Argos\QuaiDrillR'
if (-not (Test-Path -LiteralPath $source -PathType Container)) { throw "Source DRILL-R absente : $source" }
$target = [IO.Path]::GetFullPath((Join-Path $project 'Assets\ThirdParty\ToolAssets\Ship\DRILL-R'))
if (-not $target.StartsWith($project.TrimEnd('\') + '\', [StringComparison]::OrdinalIgnoreCase)) {
    throw 'Destination hors projet.'
}
$files = @(Get-ChildItem -LiteralPath $source -File)
if (@($files | Where-Object { $_.Extension -notin @('.asset', '.mat', '.prefab', '.png', '.meta', '.json') }).Count) {
    throw 'Le paquet contient des fichiers non visuels inattendus.'
}
foreach ($name in @('Drill-R.prefab', 'Drill-R-PBR.mat', 'Drill-R-LOD0.asset', 'Drill-R-LOD1.asset',
                    'Drill-R-LOD2.asset', 'Drill-R-albedo.png', 'Drill-R-normal.png', 'Drill-R-emission.png')) {
    if (-not (Test-Path -LiteralPath (Join-Path $source $name)) -or
        -not (Test-Path -LiteralPath (Join-Path $source ($name + '.meta')))) {
        throw "Dependance visuelle manquante : $name"
    }
}
$guids = [Collections.Generic.HashSet[string]]::new()
foreach ($file in $files | Where-Object Extension -eq '.meta') {
    $match = [regex]::Match([IO.File]::ReadAllText($file.FullName), '(?m)^guid: ([0-9a-f]{32})')
    if (-not $match.Success -or -not $guids.Add($match.Groups[1].Value)) { throw "GUID invalide ou duplique : $($file.Name)" }
}
# URP/Lit et AssetVersion editor : verifies dans le package URP 17.4 de VoidForge.
$externalGuids = @('933532a4fcc9baf4fa0491de14d08ed7', 'd0353a89b1f911e48b9e16bdc9f2e058')
foreach ($file in $files | Where-Object { $_.Extension -in @('.prefab', '.mat', '.asset') }) {
    $content = [IO.File]::ReadAllText($file.FullName)
    if ($file.Extension -eq '.prefab' -and $content -match '(?m)^--- !u!114 ') {
        throw 'Le prefab contient des scripts : ne pas importer les dependances runtime MyrinoxPrime.'
    }
    foreach ($match in [regex]::Matches($content, 'guid: ([0-9a-f]{32})')) {
        $guid = $match.Groups[1].Value
        if ($guid -ne ('0' * 32) -and -not $guids.Contains($guid) -and $guid -notin $externalGuids) {
            throw "Reference externe non resolue : $guid dans $($file.Name)"
        }
    }
}
$entries = @()
foreach ($file in $files) {
    $destination = Join-Path $target $file.Name
    $hash = (Get-FileHash -LiteralPath $file.FullName -Algorithm SHA256).Hash
    if ((Test-Path -LiteralPath $destination) -and
        (Get-FileHash -LiteralPath $destination -Algorithm SHA256).Hash -ne $hash) {
        throw "Copie locale differente, ne pas ecraser : $destination"
    }
    $entries += [pscustomobject]@{ File = $file.Name; Source = $file.FullName; Destination = $destination; Sha256 = $hash }
}
New-Item -ItemType Directory -Force -Path $target | Out-Null
foreach ($entry in $entries) {
    if (-not (Test-Path -LiteralPath $entry.Destination)) {
        Copy-Item -LiteralPath $entry.Source -Destination $entry.Destination
    }
    if ((Get-FileHash -LiteralPath $entry.Destination -Algorithm SHA256).Hash -ne $entry.Sha256) {
        throw "Copie incorrecte : $($entry.File)"
    }
}
$entries | ConvertTo-Json -Depth 3 | Set-Content -LiteralPath (Join-Path $target 'voidforge-copy-report.json') -Encoding utf8
Write-Output "DRILL-R : $($entries.Count) fichiers verifies ; prefab, 3 LOD, materiau URP et textures PBR."
Write-Output "Destination : $target"
