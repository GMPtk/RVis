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

"%MSBUILD%" RVis.sln /p:Configuration=Release "/p:Platform=Any CPU"

@if %ERRORLEVEL% neq 0 (
  @pause
  @exit /b 1
)

@if not exist "%ProgramFiles%\7-zip\7z.exe" (
	@echo Install 7-zip!
	@exit /b 1
)

@if exist rvis.zip (
  @del rvis.zip
)

@if exist rvis (
  @rmdir /Q /S rvis
)

@mkdir rvis
@mkdir rvis\bin

@copy UI\RVisUI\bin\Release\RVisUI.exe rvis\ >nul
@copy UI\RVisUI\bin\Release\RVisUI.exe.config rvis\ >nul
@copy UI\RVisUI\bin\Release\*.dll rvis\bin\ >nul

@copy WinR\RVis.Server\bin\Release\RVis.Server.exe rvis\bin\ >nul
@copy WinR\RVis.Server\bin\Release\RVis.Server.exe.config rvis\bin\ >nul
@copy /Y WinR\RVis.Server\bin\Release\*.dll rvis\bin\ >nul

@mkdir rvis\module

@mkdir rvis\module\estimation
@copy UI\module\Estimation\bin\Release\Estimation.dll rvis\module\estimation\ >nul

@mkdir rvis\module\evidence
@copy UI\module\Evidence\bin\Release\Evidence.dll rvis\module\evidence\ >nul

@mkdir rvis\module\plot
@copy UI\module\Plot\bin\Release\Plot.dll rvis\module\plot\ >nul

@mkdir rvis\module\sampling
@copy UI\module\Sampling\bin\Release\Sampling.dll rvis\module\sampling\ >nul

@mkdir rvis\module\sensitivity
@copy UI\module\Sensitivity\bin\Release\Sensitivity.dll rvis\module\sensitivity\ >nul

"%ProgramFiles%\7-zip\7z.exe" a rvis.zip %cd%\rvis\

@rmdir /Q /S rvis

@exit /b 0
