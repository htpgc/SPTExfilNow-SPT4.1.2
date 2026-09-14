@echo off
setlocal
if "%~1"=="" (
  echo Usage: build_spt412.cmd "D:\Path\To\SPT"
  exit /b 1
)
dotnet build -c Release -p:SPTPath="%~1"
endlocal
