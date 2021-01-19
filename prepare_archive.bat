@setlocal enabledelayedexpansion

@set rid=%1

@if "%rid%" == "" (
  @echo runtime identifier needed^^!
  @exit /b 1
)

@set "DOTNET=%ProgramFiles%\dotnet\dotnet.exe"

@if not exist "%DOTNET%" (
  @echo Failed to find dotnet command
  @exit /b 1
)

"%DOTNET%" build .\UI\RVisUI\RVisUI.csproj /p:Configuration=Release -r %rid%
@if %ERRORLEVEL% neq 0 (
  @pause
  @exit /b 1
)

"%DOTNET%" build .\UI\Module\Estimation\Estimation.csproj /p:Configuration=Release -r %rid%
@if %ERRORLEVEL% neq 0 (
  @pause
  @exit /b 1
)

"%DOTNET%" build .\UI\Module\Evidence\Evidence.csproj /p:Configuration=Release -r %rid%
@if %ERRORLEVEL% neq 0 (
  @pause
  @exit /b 1
)

"%DOTNET%" build .\UI\Module\Plot\Plot.csproj /p:Configuration=Release -r %rid%
@if %ERRORLEVEL% neq 0 (
  @pause
  @exit /b 1
)

"%DOTNET%" build .\UI\Module\Sampling\Sampling.csproj /p:Configuration=Release -r %rid%
@if %ERRORLEVEL% neq 0 (
  @pause
  @exit /b 1
)

"%DOTNET%" build .\UI\Module\Sensitivity\Sensitivity.csproj /p:Configuration=Release -r %rid%
@if %ERRORLEVEL% neq 0 (
  @pause
  @exit /b 1
)

"%DOTNET%" build .\R\RVis.Server\RVis.Server.csproj /p:Configuration=Release -r %rid%
@if %ERRORLEVEL% neq 0 (
  @pause
  @exit /b 1
)

@if exist rvis_%rid% (
  @rmdir /Q /S rvis_%rid%
)

"%DOTNET%" publish .\UI\RVisUI\RVisUI.csproj /p:Configuration=Release /p:SelfContained=true -r %rid% -o .\rvis_%rid%\RVis
@if %ERRORLEVEL% neq 0 (
  @pause
  @exit /b 1
)

"%DOTNET%" publish .\R\RVis.Server\RVis.Server.csproj /p:Configuration=Release /p:SelfContained=true -r %rid% -o .\rvis_%rid%\RVis\R
@if %ERRORLEVEL% neq 0 (
  @pause
  @exit /b 1
)

@mkdir .\rvis_%rid%\RVis\module\estimation
@copy UI\module\Estimation\bin\Release\net5.0-windows\%rid%\Estimation.dll .\rvis_%rid%\RVis\module\estimation\ >nul

@mkdir .\rvis_%rid%\RVis\module\evidence
@copy UI\module\Evidence\bin\Release\net5.0-windows\%rid%\Evidence.dll .\rvis_%rid%\RVis\module\evidence\ >nul

@mkdir .\rvis_%rid%\RVis\module\plot
@copy UI\module\Plot\bin\Release\net5.0-windows\%rid%\Plot.dll .\rvis_%rid%\RVis\module\plot\ >nul

@mkdir .\rvis_%rid%\RVis\module\sampling
@copy UI\module\Sampling\bin\Release\net5.0-windows\%rid%\Sampling.dll .\rvis_%rid%\RVis\module\sampling\ >nul

@mkdir .\rvis_%rid%\RVis\module\sensitivity
@copy UI\module\Sensitivity\bin\Release\net5.0-windows\%rid%\Sensitivity.dll .\rvis_%rid%\RVis\module\sensitivity\ >nul

@if exist RVis_%rid%.zip (
  @del RVis_%rid%.zip
)

@if not exist "%ProgramFiles%\7-zip\7z.exe" (
  @echo Failed to find 7-zip
  @exit /b 1
)

"%ProgramFiles%\7-zip\7z.exe" a RVis_%rid%.zip %cd%\rvis_%rid%\RVis\

@rmdir /Q /S rvis_%rid%

@exit /b 0
