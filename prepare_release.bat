@setlocal enabledelayedexpansion

@set version=%1

@if "%version%" == "" (
  @echo Version needed^^!
  @exit /b 1
)

@set artifactStagingDirectory=%2

@if not exist %artifactStagingDirectory% (
  @echo Staging directory needed^^!
  @exit /b 1
)

@if not exist %artifactStagingDirectory%\win-x86\RVis_v%version% (
  @echo Expecting published x86^^!
  @exit /b 1
)

@if not exist %artifactStagingDirectory%\win-x64\RVis_v%version% (
  @echo Expecting published x64^^!
  @exit /b 1
)

@mkdir %artifactStagingDirectory%\win-x86\RVis_v%version%\module

set moduleDirectory="%artifactStagingDirectory%\win-x86\RVis_v%version%\module"

@mkdir %moduleDirectory%\estimation
@copy UI\module\Estimation\bin\Release\net5.0-windows\win-x86\Estimation.dll %moduleDirectory%\estimation\ >nul

@mkdir %moduleDirectory%\evidence
@copy UI\module\Evidence\bin\Release\net5.0-windows\win-x86\Evidence.dll %moduleDirectory%\evidence\ >nul

@mkdir %moduleDirectory%\plot
@copy UI\module\Plot\bin\Release\net5.0-windows\win-x86\Plot.dll %moduleDirectory%\plot\ >nul

@mkdir %moduleDirectory%\sampling
@copy UI\module\Sampling\bin\Release\net5.0-windows\win-x86\Sampling.dll %moduleDirectory%\sampling\ >nul

@mkdir %moduleDirectory%\sensitivity
@copy UI\module\Sensitivity\bin\Release\net5.0-windows\win-x86\Sensitivity.dll %moduleDirectory%\sensitivity\ >nul

@mkdir %artifactStagingDirectory%\win-x64\RVis_v%version%\module

set moduleDirectory="%artifactStagingDirectory%\win-x64\RVis_v%version%\module"

@mkdir %moduleDirectory%\estimation
@copy UI\module\Estimation\bin\Release\net5.0-windows\win-x64\Estimation.dll %moduleDirectory%\estimation\ >nul

@mkdir %moduleDirectory%\evidence
@copy UI\module\Evidence\bin\Release\net5.0-windows\win-x64\Evidence.dll %moduleDirectory%\evidence\ >nul

@mkdir %moduleDirectory%\plot
@copy UI\module\Plot\bin\Release\net5.0-windows\win-x64\Plot.dll %moduleDirectory%\plot\ >nul

@mkdir %moduleDirectory%\sampling
@copy UI\module\Sampling\bin\Release\net5.0-windows\win-x64\Sampling.dll %moduleDirectory%\sampling\ >nul

@mkdir %moduleDirectory%\sensitivity
@copy UI\module\Sensitivity\bin\Release\net5.0-windows\win-x64\Sensitivity.dll %moduleDirectory%\sensitivity\ >nul

@endlocal

@exit /b 0
