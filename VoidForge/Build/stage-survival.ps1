param([string]$ProjectRoot=(Join-Path $PSScriptRoot '..'))
$ErrorActionPreference='Stop'
$project=(Resolve-Path -LiteralPath $ProjectRoot).Path
foreach($relative in @('Assets/VoidForge/Scripts/Economy/EconomyRuntime.cs',
    'Assets/VoidForge/Scripts/UI/Cyberdeck/CyberdeckRuntime.cs',
    'Assets/Resources/VoidForge/Economy/Catalogue.json',
    'Assets/Resources/VoidForge/Economy/Blocks.asset',
    'Assets/Digger/Modules/Core/Sources/DiggerSystem.cs')) {
    if(-not(Test-Path -LiteralPath (Join-Path $project $relative))){throw "Dependance locale absente : $relative. Voir docs/SURVIE.md."}
}
$response=Join-Path $project 'Assets/csc.rsp'
$content=if(Test-Path -LiteralPath $response){[IO.File]::ReadAllText($response)}else{''}
foreach($symbol in @('VF_LOCAL_DIGGER','VF_LOCAL_ECONOMY')) {
    if($content -notmatch "\b$symbol\b"){$content+="`n-define:$symbol`n"}
}
[IO.File]::WriteAllText($response,$content,[Text.UTF8Encoding]::new($false))
& (Join-Path $PSScriptRoot 'correct-astronaut-hands.ps1') -ProjectRoot $project -SourceRoot $project
Write-Output 'Sources productrices et Digger presents ; symboles locaux actives, gants derives prepares.'
