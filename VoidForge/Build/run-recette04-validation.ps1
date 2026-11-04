param([string]$UnityPath='D:/Unity/6000.4.0f1/Editor/Unity.exe')
$ErrorActionPreference='Stop'
$project=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$stamp=Get-Date -Format 'yyyyMMdd-HHmmss'
$oldGradle=$env:GRADLE_USER_HOME
$oldThreads=$env:BEE_BUILD_THREADS
try {
    $env:GRADLE_USER_HOME=Join-Path $project 'Library/GradleUserHome'
    $env:BEE_BUILD_THREADS='4'
    foreach($test in @('play','migration')){
        $method=if($test -eq 'play'){'VoidForge.Editor.Recette04Validation.RunBatch'}else{'VoidForge.Editor.Recette04MigrationValidation.RunBatch'}
        $save=Join-Path $project "Library/SurvivalValidation/recette04-$stamp-$test.json"
        if(Test-Path -LiteralPath $save){throw "Test save already exists: $save"}
        $arguments=@('-batchmode','-projectPath',$project,'-executeMethod',$method,'-vfSurvivalSave',$save,'-logFile',(Join-Path $project "Logs/recette04-$stamp-$test.log"))
        $quoted=$arguments|ForEach-Object{'"'+$_.Replace('"','\"')+'"'}
        $unityProcess=Start-Process -FilePath $UnityPath -ArgumentList $quoted -WindowStyle Hidden -Wait -PassThru
        if($unityProcess.ExitCode -ne 0){throw "$test failed, Unity exit $($unityProcess.ExitCode), Logs/recette04-$stamp-$test.log"}
        Write-Output "$test PASS"
    }
} finally {
    $env:GRADLE_USER_HOME=$oldGradle
    $env:BEE_BUILD_THREADS=$oldThreads
}
