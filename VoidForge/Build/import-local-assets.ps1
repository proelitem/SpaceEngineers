# Copie locale des ressources sous licence. Aucune écriture dans la bibliothèque source.
param([string] $LibraryRoot = 'G:\UnityAssets')
$ErrorActionPreference = 'Stop'
$projectRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$sourceRoot = Join-Path $LibraryRoot 'asset_manager\store\Digger_PRO_Voxel_Terrain_Sculpting\Files\Digger'
$targetRoot = Join-Path $projectRoot 'Assets\Digger'
if (-not (Test-Path -LiteralPath $sourceRoot -PathType Container)) { throw "Digger absent : $sourceRoot" }
if (Test-Path -LiteralPath (Join-Path $projectRoot 'Temp\UnityLockfile')) { throw 'Fermer Unity avant la copie des scripts Digger.' }
New-Item -ItemType Directory -Force -Path $targetRoot | Out-Null
Get-ChildItem -LiteralPath $sourceRoot -Recurse -File | ForEach-Object {
    $relative = $_.FullName.Substring($sourceRoot.Length + 1)
    $targetFile = Join-Path $targetRoot $relative
    [IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($targetFile)) | Out-Null
    if (-not (Test-Path -LiteralPath $targetFile)) { Copy-Item -LiteralPath $_.FullName -Destination $targetFile }
}
# Intégrations optionnelles absentes du projet URP ; adaptation des références locales uniquement.
Get-ChildItem -LiteralPath $targetRoot -Recurse -Filter '*.asmdef' | ForEach-Object {
    $definition = Get-Content -LiteralPath $_.FullName -Raw | ConvertFrom-Json
    $references = @($definition.references | Where-Object { $_ -notmatch '^JBooth\.MicroSplat\.|^Unity\.RenderPipelines\.HighDefinition\.' })
    if ($references.Count -ne $definition.references.Count) {
        $definition.references = $references
        $definition | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $_.FullName -Encoding utf8
    }
}
Write-Output 'Digger 8.1 copié sous Assets/Digger (ignoré par Git). Importer les astéroïdes puis construire le prototype dans Unity.'
$responsePath = Join-Path $projectRoot 'Assets\csc.rsp'
if ((Test-Path -LiteralPath $responsePath) -and
    -not ((Get-Content -LiteralPath $responsePath -Raw).Contains('-define:VF_LOCAL_DIGGER'))) {
    throw 'Assets/csc.rsp existe et ne provient pas de cet import ; ne pas l''écraser.'
}
if (-not (Test-Path -LiteralPath $responsePath)) {
    '-define:VF_LOCAL_DIGGER' | Set-Content -LiteralPath $responsePath -Encoding utf8
}
