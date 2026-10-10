@echo off
cd /d "%~dp0"
echo ========================================================
echo        Pushing SAM Card Farmer to GitHub
echo ========================================================
echo Target: https://github.com/aimimiworker/links.git
echo.
git push -u origin main
echo.
if errorlevel 1 (
    echo [ERROR] Push failed. If it asked for login, please sign in.
) else (
    echo [OK] Code successfully pushed to GitHub!
)
echo.
pause
