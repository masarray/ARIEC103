REM Copyright 2026 Ari Sulistiono
REM SPDX-License-Identifier: Apache-2.0
@echo off
setlocal
cd /d "%~dp0"
echo Starting ArIEC103 WPF Master Tester...
dotnet run --project src\ArIEC103.Desktop\ArIEC103.Desktop.csproj
pause
