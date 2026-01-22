#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT_DIR"

IMAGE_TAG="${1:-granite-server:latest}"
DOCKERFILE="${2:-Granite.Server/Dockerfile}"

if ! command -v docker >/dev/null 2>&1; then
  echo "docker CLI is required to build the server image" >&2
  exit 1
fi

echo "Building Docker image: ${IMAGE_TAG}"
echo "Using Dockerfile: ${DOCKERFILE}"
docker build -f "${DOCKERFILE}" -t "${IMAGE_TAG}" .

echo "Docker image built successfully: ${IMAGE_TAG}"
