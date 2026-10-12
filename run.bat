@echo off
cd /d "%~dp0"
dotnet run --project src/BoardGameRental.App
set "result=%errorlevel%"
pause
exit /b %result%
