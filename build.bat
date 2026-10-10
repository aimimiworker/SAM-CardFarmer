@echo off
setlocal
cd /d "%~dp0"

echo ========================================================
echo        SAM Card Farmer Build
echo ========================================================

set "OUT_DIR=%~dp0dist"
if not exist "%OUT_DIR%" mkdir "%OUT_DIR%"

set "CSC=%SystemRoot%\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
if not exist "%CSC%" set "CSC=%SystemRoot%\Microsoft.NET\Framework\v4.0.30319\csc.exe"

if exist "%CSC%" goto build_csc
where dotnet >nul 2>&1
if %ERRORLEVEL% equ 0 goto build_dotnet

echo [ERROR] Neither csc.exe nor dotnet SDK was found.
exit /b 1

:build_csc
echo [*] Compiling via csc.exe...
"%CSC%" /nologo /codepage:65001 /target:winexe /platform:anycpu /optimize+ /win32manifest:"%~dp0app.manifest" /win32icon:"%~dp0app.ico" /out:"%OUT_DIR%\SAM.CardFarmer.exe" /reference:System.dll,System.Drawing.dll,System.Windows.Forms.dll,System.Xml.dll,System.Core.dll "src\SAMCardFarmer.cs"
if errorlevel 1 (
    echo [ERROR] csc.exe compilation failed.
    exit /b 1
)
echo [OK] Successfully compiled!
goto done

:build_dotnet
echo [*] Compiling via dotnet...
dotnet build "src\SAMCardFarmer.csproj" -c Release -o "%OUT_DIR%"
if errorlevel 1 (
    echo [ERROR] dotnet build failed.
    exit /b 1
)
echo [OK] Successfully built!
goto done

:done
echo [OK] Executable ready: "%OUT_DIR%\SAM.CardFarmer.exe"
echo ========================================================
echo Done!
