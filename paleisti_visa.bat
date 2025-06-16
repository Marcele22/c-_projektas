@echo off
cd /d %~dp0

start cmd /k "cd Meistras && dotnet run"
timeout /t 1 >nul
start cmd /k "cd Skaneris && dotnet run AgentA\tekstai agent1"
timeout /t 1 >nul
start cmd /k "cd Skaneris && dotnet run AgentB\tekstai agent2"
