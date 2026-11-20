@echo off
title Rebuild Solution Script
echo [BUILD DEBUG]
dotnet build DoomPcSpeaker.sln -c Debug
echo.
echo [BUILD RELEASE]
dotnet build DoomPcSpeaker.sln -c Release
pause
