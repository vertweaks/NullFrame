@echo off
title NULLFRAME — Launcher
color 0C
setlocal

:: ── Self-elevate if not already Administrator ──────────────────────────────
net session >nul 2>&1
if %errorlevel% neq 0 (
    echo Requesting Administrator privileges...
    powershell -NoProfile -Command ^
      "Start-Process -FilePath '%~f0' -Verb RunAs"
    exit /b
)

:: ── Locate Python (tries py launcher first, then python, then python3) ──────
set PYTHON=
where py >nul 2>&1 && set PYTHON=py
if "%PYTHON%"=="" (
    where python >nul 2>&1 && set PYTHON=python
)
if "%PYTHON%"=="" (
    where python3 >nul 2>&1 && set PYTHON=python3
)

if "%PYTHON%"=="" (
    echo.
    echo  [ERROR] Python not found in PATH.
    echo  Make sure you checked "Add Python to PATH" during install.
    echo  Download: https://www.python.org/downloads/
    echo.
    pause
    exit /b 1
)

echo  [OK] Using: %PYTHON%
%PYTHON% --version

:: ── Check if Microsoft Store stub is intercepting ──────────────────────────
%PYTHON% -c "import sys; exit(0 if sys.executable else 1)" >nul 2>&1
if %errorlevel% neq 0 (
    echo.
    echo  [ERROR] Python found but appears to be the Microsoft Store stub.
    echo  Go to: Settings - Apps - App execution aliases
    echo  and turn OFF both "python.exe" and "python3.exe" aliases.
    echo.
    pause
    exit /b 1
)

:: ── Install / update dependencies ─────────────────────────────────────────
echo.
echo  Checking dependencies...
%PYTHON% -m pip install -q customtkinter
if %errorlevel% neq 0 (
    echo.
    echo  [ERROR] pip install failed. Try running: %PYTHON% -m pip install customtkinter
    echo.
    pause
    exit /b 1
)
echo  [OK] Dependencies ready.

:: ── Launch the app ─────────────────────────────────────────────────────────
echo.
echo  Launching NULLFRAME...
echo.
cd /d "%~dp0"
%PYTHON% nullframe.py
if %errorlevel% neq 0 (
    echo.
    echo  [ERROR] App exited with an error (code %errorlevel%).
    echo  Check the output above for details.
    echo.
    pause
) else (
    echo.
    echo  [INFO] App closed with exit code 0.
    pause
)
