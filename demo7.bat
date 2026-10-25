@echo off
cd /d "%~dp0"
dotnet run --project src/BoardGameRental.App -- --demo7
set "result=%errorlevel%"
pause
exit /b %result%
