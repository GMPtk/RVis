$pathToExe = "$((Get-Location).Path)\UI\RVisUI\bin\Release\RVisUI.exe"
$fileVersionInfo = [System.Diagnostics.FileVersionInfo]::GetVersionInfo($pathToExe)
Write-Host "##vso[task.setvariable variable=AppVer]$($fileVersionInfo.FileVersion)"
Write-Host "##vso[task.setvariable variable=AppVerMajor]$($fileVersionInfo.FileMajorPart)"
Write-Host "##vso[task.setvariable variable=AppVerMinor]$($fileVersionInfo.FileMinorPart)"
Write-Host "##vso[task.setvariable variable=AppVerBuild]$($fileVersionInfo.FileBuildPart)"
Write-Host "##vso[task.setvariable variable=AppVerPrivate]$($fileVersionInfo.FilePrivatePart)"
