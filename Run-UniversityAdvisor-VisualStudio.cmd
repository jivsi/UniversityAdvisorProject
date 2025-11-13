@echo off
setlocal ENABLEDELAYEDEXPANSION

cd /d "%~dp0"

set "SLN=UniversityAdvisor.sln"
if not exist "%SLN%" (
  echo Solution file not found: %SLN%
  pause
  exit /b 1
)

set "VSWHERE=%ProgramFiles(x86)%\Microsoft Visual Studio\Installer\vswhere.exe"
set "DEVENV="

if exist "%VSWHERE%" (
  for /f "usebackq tokens=*" %%I in (`"%VSWHERE%" -latest -products * -requires Microsoft.Component.MSBuild -property productPath`) do (
    set "DEVENV=%%I"
  )
)

if not defined DEVENV (
  rem Fallback common install locations
  if exist "%ProgramFiles%\Microsoft Visual Studio\2022\Community\Common7\IDE\devenv.exe" set "DEVENV=%ProgramFiles%\Microsoft Visual Studio\2022\Community\Common7\IDE\devenv.exe"
  if not defined DEVENV if exist "%ProgramFiles%\Microsoft Visual Studio\2022\Professional\Common7\IDE\devenv.exe" set "DEVENV=%ProgramFiles%\Microsoft Visual Studio\2022\Professional\Common7\IDE\devenv.exe"
  if not defined DEVENV if exist "%ProgramFiles%\Microsoft Visual Studio\2022\Enterprise\Common7\IDE\devenv.exe" set "DEVENV=%ProgramFiles%\Microsoft Visual Studio\2022\Enterprise\Common7\IDE\devenv.exe"
)

if not defined DEVENV (
  rem Try PATH
  where devenv >nul 2>nul && set "DEVENV=devenv"
)

if not defined DEVENV (
  echo Could not locate Visual Studio (devenv.exe). Please install Visual Studio 2022 or add devenv to PATH.
  pause
  exit /b 1
)

echo Launching Visual Studio: "%DEVENV%" "%SLN%" and starting Debug...
start "" "%DEVENV%" "%SLN%" /Command "Debug.Start"

endlocal
exit /b 0


