param([string]$Report='Hermes_Works/Preuves/INTEGRATION_04/core-validation.json')
$ErrorActionPreference='Stop'
$projectRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$testRoot=Join-Path $projectRoot 'Library/EconomyCoreTests'
New-Item -ItemType Directory -Path $testRoot -Force | Out-Null
$model=[Security.SecurityElement]::Escape((Join-Path $projectRoot 'Assets/VoidForge/Scripts/Economy/EconomyModel.cs'))
$tests=[Security.SecurityElement]::Escape((Join-Path $PSScriptRoot 'EconomyTests/Program.cs'))
@"
<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net9.0</TargetFramework><EnableDefaultCompileItems>false</EnableDefaultCompileItems></PropertyGroup><ItemGroup><Compile Include="$model"/><Compile Include="$tests"/></ItemGroup></Project>
"@ | Set-Content -LiteralPath (Join-Path $testRoot 'EconomyTests.csproj') -Encoding utf8
'<configuration><packageSources><clear /></packageSources></configuration>' | Set-Content -LiteralPath (Join-Path $testRoot 'NuGet.Config') -Encoding utf8
$env:DOTNET_CLI_HOME=Join-Path $projectRoot 'Library/EconomyDotnet'
$env:DOTNET_CLI_TELEMETRY_OPTOUT='1'
$env:DOTNET_GENERATE_ASPNET_CERTIFICATE='false'
$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE='1'
& dotnet run --project (Join-Path $testRoot 'EconomyTests.csproj') -- (Join-Path $projectRoot 'Assets/Resources/VoidForge/Economy/Catalogue.json') (Join-Path $projectRoot $Report)
exit $LASTEXITCODE
