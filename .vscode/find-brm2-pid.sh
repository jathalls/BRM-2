#!/bin/bash
# Find the PID of the BRM-2 dotnet process
# Usage: source this script or run it to get the PID

# Look for dotnet process running BRM-2 project
PID=$(pgrep -f "BRM-2.csproj" | head -n1)

if [ -z "$PID" ]; then
  # Try alternative: look for process with BRM-2.dll
  PID=$(pgrep -f "BRM-2.dll" | head -n1)
fi

if [ -z "$PID" ]; then
  # Last resort: look for any dotnet process matching run command
  PID=$(pgrep -f "dotnet run.*BRM-2" | head -n1)
fi

if [ -z "$PID" ]; then
  echo "ERROR: Could not find BRM-2 process. Make sure the app is running." >&2
  exit 1
fi

echo "$PID"
