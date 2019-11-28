@setlocal enabledelayedexpansion
@set MSBUILD=

@for /D %%M in ("%ProgramFiles(x86)%\Microsoft Visual Studio\2019"\*) do (
  @if exist "%%M\MSBuild\Current\Bin\MSBuild.exe" (
    @set "MSBUILD=%%M\MSBuild\Current\Bin\MSBuild.exe"
  )
)

@if ["%MSBUILD%"] == [""] (
  @for /D %%M in ("%ProgramFiles(x86)%\Microsoft Visual Studio\2017"\*) do (
    @if exist "%%M\MSBuild\15.0\Bin\MSBuild.exe" (
      @set "MSBUILD=%%M\MSBuild\15.0\Bin\MSBuild.exe"
    )
  )
)

@if "%MSBUILD%" == "" (
  @echo Failed to find VS2017 or VS2019 MSBuild
  @exit /b 1
)

"%MSBUILD%" RVis.sln /p:Configuration=Release "/p:Platform=x64"

@if %ERRORLEVEL% neq 0 (
  @pause
  @exit /b 1
)

@if not exist "%ProgramFiles%\7-zip\7z.exe" (
	@echo Install 7-zip!
	@exit /b 1
)

@if exist rvisx64.zip (
  @del rvisx64.zip
)

@if exist rvisx64 (
  @rmdir /Q /S rvisx64
)

@mkdir rvisx64
@mkdir rvisx64\bin

@copy UI\RVisUI\bin\x64\Release\RVisUI.exe rvisx64\ >nul
@copy UI\RVisUI\bin\x64\Release\RVisUI.exe.config rvisx64\ >nul
@copy UI\RVisUI\bin\x64\Release\*.dll rvisx64\bin\ >nul

@copy WinR\RVis.Server\bin\x64\Release\RVis.Server.exe rvisx64\bin\ >nul
@copy WinR\RVis.Server\bin\x64\Release\RVis.Server.exe.config rvisx64\bin\ >nul
@copy /Y WinR\RVis.Server\bin\x64\Release\*.dll rvisx64\bin\ >nul

@mkdir rvisx64\module

@mkdir rvisx64\module\estimation
@copy UI\module\Estimation\bin\x64\Release\Estimation.dll rvisx64\module\estimation\ >nul

@mkdir rvisx64\module\evidence
@copy UI\module\Evidence\bin\x64\Release\Evidence.dll rvisx64\module\evidence\ >nul

@mkdir rvisx64\module\plot
@copy UI\module\Plot\bin\x64\Release\Plot.dll rvisx64\module\plot\ >nul

@mkdir rvisx64\module\sampling
@copy UI\module\Sampling\bin\x64\Release\Sampling.dll rvisx64\module\sampling\ >nul

@mkdir rvisx64\module\sensitivity
@copy UI\module\Sensitivity\bin\x64\Release\Sensitivity.dll rvisx64\module\sensitivity\ >nul

"%ProgramFiles%\7-zip\7z.exe" a rvisx64.zip %cd%\rvisx64\

@rmdir /Q /S rvisx64

@exit /b 0
