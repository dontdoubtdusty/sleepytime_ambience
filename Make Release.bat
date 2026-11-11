@echo off
rem Builds the shareable zip in the release folder (see tools\make-release.ps1).
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0tools\make-release.ps1"
pause
