# GitHub Actions CI/CD Documentation

## Overview

The Granite Server project uses three separate GitHub Actions workflows for building and releasing the Server, Mod, and ClientApp components. Each workflow runs on branch pushes/PRs for testing and on tag pushes for releases.

## Workflows

### 1. Build Mod ([build-mod.yml](.github/workflows/build-mod.yml))

**Container:** `ghcr.io/kramins/vintagestory-devcontainer:1.21` (required for VintageStory mod build)

**Triggers:**
- Push to `main`, `feature/**` branches
- Pull requests to `main`
- Tags matching `v*` (e.g., `v0.0.9`)

**Jobs:**
- `build-mod`: Builds the Granite.Mod project
  - Caches NuGet and npm dependencies
  - Builds in Debug mode for branches/PRs
  - Builds in Release mode for tags with version injection
  - Uploads artifact: `granite-mod-{sha}.zip` or `granite-mod-v{version}-{sha}.zip`
  
- `release`: Creates GitHub Release (only on tags)
  - Downloads the mod artifact
  - Attaches to GitHub Release
  - Marks as prerelease if tag contains `-rc`, `-RC`, `-beta`, or `-alpha`

**Artifact Location:** `Granite.Mod/bin/mods/*.zip`

### 2. Build Server ([build-server.yml](.github/workflows/build-server.yml))

**Container:** `ghcr.io/kramins/vintagestory-devcontainer:1.21`

**Triggers:**
- Push to `main`, `feature/**` branches
- Pull requests to `main`
- Tags matching `v*`

**Jobs:**
- `build-server`: Builds and tests the Granite.Server project
  - Caches NuGet packages
  - Builds in Debug mode for branches/PRs
  - Builds in Release mode for tags
  - Runs `dotnet test` on the entire solution
  - Publishes server to `./publish/server`
  - Creates tar.gz archive
  - Uploads artifact: `granite-server-{sha}.tar.gz` or `granite-server-v{version}-{sha}.tar.gz`

- `release`: Creates GitHub Release (only on tags)
  - Downloads the server artifact
  - Attaches to GitHub Release

**Test Output:** Tests run on every build but only fail the build if errors are found

### 3. Build Client ([build-client.yml](.github/workflows/build-client.yml))

**Container:** Native ubuntu-latest (no special container needed)

**Triggers:**
- Push to `main`, `feature/**` branches
- Pull requests to `main`
- Tags matching `v*`

**Jobs:**
- `build-client`: Builds the React/TypeScript ClientApp
  - Uses Node.js 20
  - Caches npm dependencies via `actions/setup-node`
  - Runs `npm ci` for clean install
  - Runs `npm run lint`
  - Runs `npm run build` (Vite)
  - Creates tar.gz archive of `dist/` folder
  - Uploads artifact: `granite-client-{sha}.tar.gz` or `granite-client-v{version}-{sha}.tar.gz`

- `release`: Creates GitHub Release (only on tags)
  - Downloads the client artifact
  - Attaches to GitHub Release

**Build Output:** `Granite.Server/ClientApp/dist/`

## Artifact Naming Convention

### Branch/PR Builds
- Mod: `granite-mod-{sha}.zip`
- Server: `granite-server-{sha}.tar.gz`
- Client: `granite-client-{sha}.tar.gz`

Where `{sha}` is the first 7 characters of the commit SHA.

### Tag Builds (Releases)
- Mod: `granite-mod-v{version}-{sha}.zip`
- Server: `granite-server-v{version}-{sha}.tar.gz`
- Client: `granite-client-v{version}-{sha}.tar.gz`

Where `{version}` is extracted from the git tag (e.g., tag `v0.0.9` → version `0.0.9`).

## Release Process

1. Create and push a tag:
   ```bash
   git tag v0.0.9
   git push origin v0.0.9
   ```

2. All three workflows will trigger automatically

3. Each workflow builds its component and uploads artifacts

4. Each workflow creates/updates the same GitHub Release with its artifacts

5. The release is marked as a prerelease if the tag contains:
   - `-rc` or `-RC` (release candidate)
   - `-beta`
   - `-alpha`

## Local Development

You can run the same build commands locally:

### Mod Build
```bash
# Debug build
dotnet build Granite.Mod/Granite.Mod.csproj -c Debug /p:BuildWebApp=true

# Release build with version
dotnet build Granite.Mod/Granite.Mod.csproj -c Release /p:BuildWebApp=true /p:modinfoVersion=0.0.9
```

### Server Build and Test
```bash
# Build
dotnet build Granite.Server/Granite.Server.csproj -c Debug

# Test
dotnet test GraniteServer.sln --configuration Debug

# Publish
dotnet publish Granite.Server/Granite.Server.csproj -c Release -o ./publish/server
```

### Client Build
```bash
cd Granite.Server/ClientApp

# Install dependencies
npm ci

# Lint
npm run lint

# Build
npm run build
```

## Caching Strategy

- **NuGet**: Caches `~/.nuget/packages` with key based on `**/*.csproj` file hashes
- **npm**: Uses built-in caching from `actions/setup-node` based on `package-lock.json`

## Notes

- Database migrations are NOT created in CI (use `./scripts/ef-migration.sh` locally)
- The VintageStory dev container is required for mod builds due to game API dependencies
- Test failures will fail the build but do not block artifact creation on branches
- All workflows use `actions/upload-artifact@v4` and `actions/download-artifact@v4`
- Releases use `softprops/action-gh-release@v2` with automatic release notes generation

## Troubleshooting

### Mod Build Fails
- Ensure the VintageStory dev container is accessible
- Check that `VINTAGE_STORY` environment variable is set in the container
- Verify `Granite.Server/ClientApp` has been built (mod includes the webapp)

### Server Tests Fail
- Review test output in the Actions log
- Tests can be run locally with `dotnet test`
- Some tests may be environment-specific

### Client Build Fails
- Check for npm dependency issues
- Run `npm ci` locally to reproduce
- Check linting errors with `npm run lint`
- Ensure Node.js 20 is used

## Migration from Old Workflow

The previous `ci-cd.yml` workflow has been replaced by these three separate workflows. Key differences:

- **Separation of Concerns**: Each component (Mod, Server, Client) has its own workflow
- **Parallel Builds**: All three can run simultaneously on the same trigger
- **Better Artifact Naming**: Clear naming with version and commit SHA
- **Improved Caching**: More granular cache keys
- **Branch Builds**: Now builds and tests on all branches, not just PRs to main
- **Consistent Release Process**: All three workflows contribute to the same release
