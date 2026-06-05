#!/bin/bash
# Azure App Service (Linux) startup — ensures dotnet runs and leaves a trace in LogFiles.
set -e
MARKER="/home/LogFiles/startup-marker.txt"
echo "$(date -u +%Y-%m-%dT%H:%M:%SZ) startup.sh invoked; pwd=$(pwd)" >> "$MARKER" 2>/dev/null || true
cd /home/site/wwwroot
if [ ! -f MySociety.Api.dll ]; then
  echo "$(date -u +%Y-%m-%dT%H:%M:%SZ) ERROR: MySociety.Api.dll missing in wwwroot" >> "$MARKER" 2>/dev/null || true
  ls -la >> "$MARKER" 2>/dev/null || true
  exit 1
fi
echo "$(date -u +%Y-%m-%dT%H:%M:%SZ) launching dotnet MySociety.Api.dll" >> "$MARKER" 2>/dev/null || true
exec dotnet MySociety.Api.dll
