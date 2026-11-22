@echo off
title Publish Script
echo [PUBLISH]
dotnet publish .\src\DoomPcSpeaker\DoomPcSpeaker.csproj -c Release -o .\publish
pause
