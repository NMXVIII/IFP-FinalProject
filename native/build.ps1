param([string]$Compiler = 'gcc')
$ErrorActionPreference = 'Stop'
$taskCompiler = Get-Command $Compiler -ErrorAction SilentlyContinue
if (-not $taskCompiler -and (Test-Path -LiteralPath 'C:\msys64\ucrt64\bin\gcc.exe')) {
    $taskCompiler = Get-Command 'C:\msys64\ucrt64\bin\gcc.exe'
}
if (-not $taskCompiler) { throw 'Install MSYS2 UCRT64 GCC, or pass -Compiler with the full path to gcc.exe.' }
$taskTarget = & $taskCompiler.Source -dumpmachine
if ($LASTEXITCODE -ne 0 -or $taskTarget -ne 'x86_64-w64-mingw32') {
    throw 'This build supports the Windows x64 GCC target x86_64-w64-mingw32.'
}
$taskRoot = Split-Path -Parent $PSScriptRoot
$taskOutput = Join-Path $taskRoot 'runtimes\win-x64\native\boardgame_native.dll'
New-Item -ItemType Directory -Path (Split-Path -Parent $taskOutput) -Force | Out-Null
# Relative arguments also work when the project parent has Cyrillic characters.
Push-Location -LiteralPath $taskRoot
try {
    & $taskCompiler.Source -std=c11 -O2 -Wall -Wextra -Werror -shared -o 'runtimes/win-x64/native/boardgame_native.dll' 'native/boardgame_native.c'
    if ($LASTEXITCODE -ne 0) { throw 'Native compilation failed.' }
}
finally { Pop-Location }
Write-Output "Built $taskOutput"
