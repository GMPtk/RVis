$pathToDLL = "$((Get-Location).Path)\RVis.Base\bin\Release\net5.0\RVis.Base.dll"
$fileVersionInfo = [System.Diagnostics.FileVersionInfo]::GetVersionInfo($pathToDLL)
Write-Host "##vso[task.setvariable variable=AppVer]$($fileVersionInfo.FileVersion)"
Write-Host "##vso[task.setvariable variable=AppVerMajor]$($fileVersionInfo.FileMajorPart)"
Write-Host "##vso[task.setvariable variable=AppVerMinor]$($fileVersionInfo.FileMinorPart)"
Write-Host "##vso[task.setvariable variable=AppVerBuild]$($fileVersionInfo.FileBuildPart)"
Write-Host "##vso[task.setvariable variable=AppVerPrivate]$($fileVersionInfo.FilePrivatePart)"
