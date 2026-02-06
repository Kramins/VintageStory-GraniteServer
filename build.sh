#!/bin/bash
set -e

# Detect if running inside a container
if [ -f /.dockerenv ]; then
    # Already in container, run cake directly
    exec dotnet cake.cs "$@"
else
    # Not in container, try to relaunch in devcontainer via docker-compose
    if [ -f .devcontainer/docker-compose.yml ]; then
        # Start/ensure devcontainer is running
        docker compose -f .devcontainer/docker-compose.yml up -d > /dev/null 2>&1
        
        # Get the first service name from docker-compose.yml (under 'services:' section)
        SERVICE_NAME=$(awk '/^services:/{flag=1; next} /^[^ ]/ && flag{exit} flag && /^  [a-z]/{gsub(/:$/,"",$1); print $1; exit}' .devcontainer/docker-compose.yml)
        SERVICE_NAME=${SERVICE_NAME:-vintage-mod-dev}
        
        # Re-run in the container
        exec docker compose -f .devcontainer/docker-compose.yml exec "$SERVICE_NAME" dotnet cake.cs "$@"
    else
        # docker-compose.yml not found, error with helpful message
        echo "Error: Not running in a container and .devcontainer/docker-compose.yml not found."
        echo ""
        echo "Options to fix this:"
        echo "  1. Set up the devcontainer:"
        echo "     ./devcontainer-shell.sh"
        echo ""
        echo "  2. Or run directly in Docker:"
        echo "     docker run --rm -v \$(pwd):/workspace -w /workspace mcr.microsoft.com/dotnet/sdk:9.0 dotnet cake.cs $@"
        echo ""
        exit 1
    fi
fi
