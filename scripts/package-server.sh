#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT_DIR"

CONFIGURATION="${CONFIGURATION:-Release}"
PACKAGE_VERSION="${PACKAGE_VERSION:-}"
RUNTIME_IDENTIFIER="${RUNTIME_IDENTIFIER:-}"
SELF_CONTAINED="${SELF_CONTAINED:-false}"
BUILD_WEB_APP="${BUILD_WEB_APP:-true}"
PACKAGE_FORMATS="${PACKAGE_FORMATS:-zip}"
PACKAGE_OUTPUT_ROOT="${PACKAGE_OUTPUT_ROOT:-artifacts/}"

if ! command -v dotnet >/dev/null 2>&1; then
  echo "dotnet SDK is required to package the server" >&2
  exit 1
fi

echo "Restoring NuGet packages..."
dotnet restore GraniteServer.sln

MSBUILD_PROPS=(
  "/p:Configuration=${CONFIGURATION}"
  "/p:BuildWebApp=${BUILD_WEB_APP}"
  "/p:PackageFormats=${PACKAGE_FORMATS}"
  "/p:PackageOutputRoot=${PACKAGE_OUTPUT_ROOT}"
)

if [[ -n "${PACKAGE_VERSION}" ]]; then
  MSBUILD_PROPS+=("/p:PackageVersion=${PACKAGE_VERSION}")
fi

if [[ -n "${RUNTIME_IDENTIFIER}" ]]; then
  MSBUILD_PROPS+=("/p:RuntimeIdentifier=${RUNTIME_IDENTIFIER}")
fi

if [[ "${SELF_CONTAINED}" == "true" ]]; then
  MSBUILD_PROPS+=("/p:SelfContained=true")
fi

echo "Packaging Granite.Server (Configuration=${CONFIGURATION}, BuildWebApp=${BUILD_WEB_APP})..."
dotnet msbuild Granite.Server/Granite.Server.csproj /t:PackageServer "${MSBUILD_PROPS[@]}"

echo "Artifacts available under ${PACKAGE_OUTPUT_ROOT}server"