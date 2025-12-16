# Release Process

This document describes how to create and publish releases of the Azure DevOps Pipelines Local Runner.

## Overview

Releases are automated through GitHub Actions. When you push a version tag, the workflow will:

1. Build the application for Windows, Linux, and macOS
2. Create platform-specific archives
3. Create a GitHub Release
4. Upload the archives as release assets

## Creating a Release

### Prerequisites

- Ensure all tests are passing
- Update the version number in `src/AdoPipelinesLocalRunner.csproj`
- Update the CHANGELOG (if you have one)
- Commit all changes to main branch

### Steps

1. **Create and push a version tag:**

   ```bash
   # Format: v{major}.{minor}.{patch}
   git tag v1.0.0
   git push origin v1.0.0
   ```

2. **Monitor the workflow:**

   - Go to the **Actions** tab in GitHub
   - Watch the "Release" workflow progress
   - The workflow creates builds for:
     - Windows (win-x64)
     - Linux (linux-x64)
     - macOS (osx-x64)

3. **Verify the release:**

   - Navigate to **Releases** page
   - Check that the release was created
   - Verify all three platform archives are attached
   - Test download and extraction

### Manual Release (Optional)

You can also trigger a release manually:

1. Go to **Actions** tab
2. Select "Release" workflow
3. Click "Run workflow"
4. Enter the tag name (e.g., `v1.0.0`)
5. Click "Run workflow" button

## Release Artifacts

Each release includes three downloadable archives:

| Platform | File | Contents |
|----------|------|----------|
| Windows | `azp-local-win-x64.zip` | Self-contained Windows executable |
| Linux | `azp-local-linux-x64.tar.gz` | Self-contained Linux executable |
| macOS | `azp-local-osx-x64.tar.gz` | Self-contained macOS executable |

All builds are:

- **Self-contained**: Include the .NET runtime (no separate .NET installation required)
- **Single-file**: Published as a single executable file
- **Platform-specific**: Optimized for each target platform

## Version Numbering

Follow [Semantic Versioning](https://semver.org/):

- **MAJOR** version: Incompatible API changes
- **MINOR** version: Add functionality (backwards-compatible)
- **PATCH** version: Bug fixes (backwards-compatible)

Example: `v1.2.3`

## Updating Version Numbers

Edit `src/AdoPipelinesLocalRunner.csproj`:

```xml
<PropertyGroup>
  <Version>1.0.0</Version>
  <AssemblyVersion>1.0.0.0</AssemblyVersion>
  <FileVersion>1.0.0.0</FileVersion>
</PropertyGroup>
```

## Pre-release Versions

For beta or release candidate versions:

```bash
# Beta release
git tag v1.0.0-beta.1
git push origin v1.0.0-beta.1

# Release candidate
git tag v1.0.0-rc.1
git push origin v1.0.0-rc.1
```

Mark as pre-release in the workflow by editing the release body or manually in GitHub UI.

## Troubleshooting

### Build Fails

- Check the Actions workflow logs
- Ensure all tests pass locally: `dotnet test`
- Verify the project compiles: `dotnet build`

### Tag Already Exists

If you need to recreate a tag:

```bash
# Delete local tag
git tag -d v1.0.0

# Delete remote tag
git push origin :refs/tags/v1.0.0

# Create new tag
git tag v1.0.0
git push origin v1.0.0
```

### Missing Release Assets

The workflow builds sequentially. If one platform fails, others may still succeed. Check the workflow logs for the specific platform that failed.

## User Download Instructions

Include these instructions in your README for end users:

### Installation

1. **Download the latest release:**
   - Go to [Releases](../../releases)
   - Download the appropriate file for your platform:
     - Windows: `azp-local-win-x64.zip`
     - Linux: `azp-local-linux-x64.tar.gz`
     - macOS: `azp-local-osx-x64.tar.gz`

2. **Extract the archive:**

   **Windows:**

   ```powershell
   Expand-Archive -Path azp-local-win-x64.zip -DestinationPath azp-local
   ```

   **Linux/macOS:**

   ```bash
   tar -xzf azp-local-linux-x64.tar.gz
   # or
   tar -xzf azp-local-osx-x64.tar.gz
   ```

3. **Run the tool:**

   **Windows:**

   ```powershell
   .\azp-local\azp-local.exe --help
   ```

   **Linux/macOS:**

   ```bash
   chmod +x azp-local
   ./azp-local --help
   ```

4. **(Optional) Add to PATH:**

   Add the extracted directory to your system PATH for easier access.

## Release Checklist

Before creating a release:

- [ ] All tests passing
- [ ] Code coverage meets threshold (90%)
- [ ] Version number updated in `.csproj`
- [ ] CHANGELOG updated (if applicable)
- [ ] README documentation is current
- [ ] All changes committed to main branch
- [ ] Tag created with correct version format
- [ ] GitHub Actions workflow completes successfully
- [ ] All three platform archives are present in release
- [ ] Manual smoke test on at least one platform

## Automation Details

The release workflow (`release.yml`) is triggered by:

- **Tag push**: Any tag matching `v*.*.*` pattern
- **Manual dispatch**: Via GitHub Actions UI

The workflow:

1. Creates a GitHub Release with the tag
2. Builds for three platforms in parallel
3. Creates compressed archives for each platform
4. Uploads archives as release assets
5. Verifies all artifacts were created successfully

Build configuration:

- Configuration: Release
- Runtime: Self-contained
- SingleFile: true
- PublishTrimmed: false (for better compatibility)
