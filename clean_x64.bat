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

"%MSBUILD%" RVis.sln /t:Clean /p:Configuration=Release "/p:Platform=x64" || pause
"%MSBUILD%" RVis.sln /t:Clean /p:Configuration=Debug "/p:Platform=x64" || pause

@if exist rvisx64.zip (
  @del rvisx64.zip
)

@if exist rvisx64 (
  @rmdir /Q /S rvisx64
)
