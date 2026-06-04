@echo off
setlocal
cd /d %~dp0

echo ArIEC103 IEC-103 Slave Simulator

echo Default behavior: protection pickup random phase, trip after 200 ms, auto reset after 20 s, repeat after 10 s.

dotnet run --project src\ArIEC103.Cli -- slave --port COM2 --baud 9600 --link 1 --ca 1 --duration 300 --initial-fault-delay 3 --trip-delay 200 --auto-reset 20 --fault-repeat-delay 10
