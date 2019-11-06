@setlocal enabledelayedexpansion

@set version=%1

@if "%version%" == "" (
  @echo Version needed^^!
  @exit /b 1
)

@set dirName="RVis_v%version%"

@if exist %dirName% (
  @rmdir /Q /S %dirName%
)

@mkdir %dirName%
@mkdir %dirName%\bin

@set release="UI\RVisUI\bin\Release"

@copy %release%\RVisUI.exe %dirName%\
@copy %release%\RVisUI.exe.config %dirName%\
@copy %release%\*.dll %dirName%\bin\

@set release="WinR\RVis.Server\bin\Release"

@copy %release%\RVis.Server.exe %dirName%\bin\
@copy %release%\RVis.Server.exe.config %dirName%\bin\
@copy /Y %release%\*.dll %dirName%\bin\

@mkdir %dirName%\module

@mkdir %dirName%\module\estimation
@copy UI\module\Estimation\bin\Release\Estimation.dll %dirName%\module\estimation\

@mkdir %dirName%\module\evidence
@copy UI\module\Evidence\bin\Release\Evidence.dll %dirName%\module\evidence\

@mkdir %dirName%\module\plot
@copy UI\module\Plot\bin\Release\Plot.dll %dirName%\module\plot\

@mkdir %dirName%\module\sampling
@copy UI\module\Sampling\bin\Release\Sampling.dll %dirName%\module\sampling\

@mkdir %dirName%\module\sensitivity
@copy UI\module\Sensitivity\bin\Release\Sensitivity.dll %dirName%\module\sensitivity\

@echo.
@echo Prepared portable %dirName%
@echo.

@dir %dirName% /B /S

@echo.

@set dirNamex64="RVis_v%version%_x64"

@if exist %dirNamex64% (
  @rmdir /Q /S %dirNamex64%
)

@mkdir %dirNamex64%
@mkdir %dirNamex64%\bin

@set release="UI\RVisUI\bin\x64\Release"

@copy %release%\RVisUI.exe %dirNamex64%\
@copy %release%\RVisUI.exe.config %dirNamex64%\
@copy %release%\*.dll %dirNamex64%\bin\

@set release="WinR\RVis.Server\bin\x64\Release"

@copy %release%\RVis.Server.exe %dirNamex64%\bin\
@copy %release%\RVis.Server.exe.config %dirNamex64%\bin\
@copy /Y %release%\*.dll %dirNamex64%\bin\

@mkdir %dirNamex64%\module

@mkdir %dirNamex64%\module\estimation
@copy UI\module\Estimation\bin\x64\Release\Estimation.dll %dirNamex64%\module\estimation\

@mkdir %dirNamex64%\module\evidence
@copy UI\module\Evidence\bin\x64\Release\Evidence.dll %dirNamex64%\module\evidence\

@mkdir %dirNamex64%\module\plot
@copy UI\module\Plot\bin\x64\Release\Plot.dll %dirNamex64%\module\plot\

@mkdir %dirNamex64%\module\sampling
@copy UI\module\Sampling\bin\x64\Release\Sampling.dll %dirNamex64%\module\sampling\

@mkdir %dirNamex64%\module\sensitivity
@copy UI\module\Sensitivity\bin\x64\Release\Sensitivity.dll %dirNamex64%\module\sensitivity\

@echo.
@echo Prepared portable %dirNamex64%
@echo.

@dir %dirNamex64% /B /S

@echo.

@endlocal

@exit /b 0