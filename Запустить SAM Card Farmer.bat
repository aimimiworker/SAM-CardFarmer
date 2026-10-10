@echo off
chcp 65001 >nul
cd /d "%~dp0"
title SAM Card Farmer Launcher

:: Снятие метки SmartScreen (Zone.Identifier) со всех файлов
powershell -NoProfile -Command "Get-ChildItem -Path '%~dp0' -Recurse -ErrorAction SilentlyContinue | Unblock-File -ErrorAction SilentlyContinue" >nul 2>&1

if exist "%~dp0dist\SAM.CardFarmer.exe" (
    start "" "%~dp0dist\SAM.CardFarmer.exe"
) else if exist "%~dp0SAM.CardFarmer.exe" (
    start "" "%~dp0SAM.CardFarmer.exe"
) else (
    echo [ОШИБКА] SAM.CardFarmer.exe не найден!
    pause
)
