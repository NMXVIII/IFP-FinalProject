@echo off
cd /d "%~dp0"
dotnet run --project tests/BoardGameRental.Tests
set "result=%errorlevel%"
pause
exit /b %result%
