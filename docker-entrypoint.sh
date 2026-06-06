#!/bin/sh
set -e

export ASPNETCORE_URLS="http://+:${PORT:-8080}"

echo "Applying database migrations..."
dotnet MySociety.Api.dll --migrate-only

echo "Starting web server..."
exec dotnet MySociety.Api.dll --skip-migration
