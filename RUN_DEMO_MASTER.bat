@echo off
setlocal
cd /d "%~dp0"
echo Running ArIEC103 simulated generic relay master demo with sample user mapping profile...
echo Duration is 45 seconds so pickup, trip, auto reset, and repeat cycle can be observed.
dotnet run --project src\ArIEC103.Cli -- master --simulate --duration 45 --mapping samples\mapping-profiles\example-user-mapping.profile.json --report out\demo-master-evidence.md --json out\demo-master-evidence.json
pause
