# Version Management and CI/CD Integration

This document describes the automatic version and build number management system implemented for the Amendment application's GitHub Actions CI/CD pipeline.

## Overview

The system provides automatic version detection and injection throughout the build process, supporting the client-side version checking and update notification system. It uses Git tags for release versions and GitHub Actions build numbers for traceability.

## Architecture

### Version Sources

1. **Git Tags**: Release versions follow semantic versioning (e.g., `v1.2.3`)
2. **GitHub Actions**: Build numbers from `github.run_number`
3. **Build Date**: UTC timestamp when the build was executed

### Version Format

- **Tag Version**: `1.2.3` (extracted from Git tag `v1.2.3`)
- **Build Number**: GitHub Actions run number (e.g., `456`)
- **Full Version**: `1.2.3.456` (tag version + build number)
- **Build Date**: ISO 8601 UTC format (e.g., `2024-01-15T10:30:45Z`)

## Implementation Components

### 1. GitHub Actions Workflow (`.github/workflows/docker-publish.yml`)

**New Version Extraction Step**:
```yaml
- name: Extract version information
  id: version
  run: |
    # Extract version from tag (remove 'v' prefix)
    VERSION=${GITHUB_REF#refs/tags/v}
    echo "VERSION=$VERSION" >> $GITHUB_OUTPUT
    
    # Use GitHub run number as build number
    BUILD_NUMBER=${{ github.run_number }}
    echo "BUILD_NUMBER=$BUILD_NUMBER" >> $GITHUB_OUTPUT
    
    # Set build date to current UTC time
    BUILD_DATE=$(date -u +"%Y-%m-%dT%H:%M:%SZ")
    echo "BUILD_DATE=$BUILD_DATE" >> $GITHUB_OUTPUT
    
    # Create full version with build number
    FULL_VERSION="${VERSION}.${BUILD_NUMBER}"
    echo "FULL_VERSION=$FULL_VERSION" >> $GITHUB_OUTPUT
```

**Docker Build Arguments**:
```yaml
build-args: |
  VERSION=${{ steps.version.outputs.VERSION }}
  BUILD_NUMBER=${{ steps.version.outputs.BUILD_NUMBER }}
  BUILD_DATE=${{ steps.version.outputs.BUILD_DATE }}
  FULL_VERSION=${{ steps.version.outputs.FULL_VERSION }}
```

### 2. Directory.Build.props

Centralizes version management across all .NET projects:

```xml
<PropertyGroup>
  <!-- Version from CI/CD pipeline -->
  <VersionPrefix Condition="'$(VERSION)' != ''">$(VERSION)</VersionPrefix>
  <VersionPrefix Condition="'$(VERSION)' == ''">1.0.0</VersionPrefix>
  
  <!-- Build number from CI/CD pipeline -->
  <BuildNumber Condition="'$(BUILD_NUMBER)' != ''">$(BUILD_NUMBER)</BuildNumber>
  <BuildNumber Condition="'$(BUILD_NUMBER)' == ''">0</BuildNumber>
  
  <!-- Full version with build number -->
  <Version Condition="'$(FULL_VERSION)' != ''">$(FULL_VERSION)</Version>
  <Version Condition="'$(FULL_VERSION)' == ''">$(VersionPrefix).$(BuildNumber)</Version>
</PropertyGroup>

<!-- Assembly metadata for runtime access -->
<ItemGroup>
  <AssemblyMetadata Include="BuildDate" Value="$(BuildDate)" />
  <AssemblyMetadata Include="BuildNumber" Value="$(BuildNumber)" />
  <AssemblyMetadata Include="Version" Value="$(Version)" />
  <AssemblyMetadata Include="VersionPrefix" Value="$(VersionPrefix)" />
</ItemGroup>
```

### 3. Dockerfile Updates

**Build Arguments**:
```dockerfile
ARG VERSION=1.0.0
ARG BUILD_NUMBER=0
ARG BUILD_DATE
ARG FULL_VERSION
```

**Build Commands**:
```dockerfile
RUN dotnet build "./Amendment.Server.csproj" -c $BUILD_CONFIGURATION -o /app/build \
    -p:VERSION="$VERSION" \
    -p:BUILD_NUMBER="$BUILD_NUMBER" \
    -p:BUILD_DATE="$BUILD_DATE" \
    -p:FULL_VERSION="$FULL_VERSION"
```

**Runtime Environment Variables**:
```dockerfile
ENV VERSION=$VERSION
ENV BUILD_NUMBER=$BUILD_NUMBER
ENV BUILD_DATE=$BUILD_DATE
ENV FULL_VERSION=$FULL_VERSION
```

### 4. Enhanced VersionController

The controller now prioritizes assembly metadata over other sources:

```csharp
// Try to get version from assembly metadata first (injected during build)
var version = GetAssemblyMetadata(assembly, "Version") ?? 
             assembly.GetName().Version?.ToString() ?? 
             "1.0.0.0";

// Get build number from assembly metadata, environment, or assembly version
var buildNumber = GetAssemblyMetadata(assembly, "BuildNumber") ??
                 Environment.GetEnvironmentVariable("BUILD_NUMBER") ?? 
                 assembly.GetName().Version?.Build.ToString() ?? 
                 "0";
```

## Usage

### Creating a Release

1. **Create and push a Git tag**:
   ```bash
   git tag v1.2.3
   git push origin v1.2.3
   ```

2. **GitHub Actions automatically**:
   - Extracts version `1.2.3` from tag `v1.2.3`
   - Uses run number (e.g., `456`) as build number
   - Creates full version `1.2.3.456`
   - Injects version info into all assemblies
   - Builds and publishes Docker image with version metadata

### Version Information Access

**Server-side** (VersionController endpoint `/api/version`):
```json
{
  "version": "1.2.3.456",
  "buildDate": "2024-01-15T10:30:45Z",
  "buildNumber": "456",
  "environment": "Production"
}
```

**Client-side** (VersionCheckService):
- Automatically polls `/api/version` every 5 minutes
- Compares server version with locally stored version
- Triggers update notifications when newer version detected

## Integration with Version Checking System

This version management system directly supports the existing version checking and update notification system:

1. **Automatic Version Detection**: Build pipeline injects accurate version information
2. **Reliable Comparison**: Build dates provide reliable version comparison
3. **Traceability**: GitHub run numbers enable build traceability
4. **Client Notifications**: Version checking service automatically detects updates

## Benefits

- **Automated**: No manual version management required
- **Consistent**: Single source of truth for version information
- **Traceable**: GitHub run numbers link builds to CI/CD runs
- **Reliable**: Build dates provide accurate update detection
- **Integrated**: Works seamlessly with existing version checking system

## Troubleshooting

### Version Not Updating
- Verify Git tag format matches `v*.*.*` pattern
- Check GitHub Actions workflow logs for version extraction
- Ensure Docker build arguments are passed correctly

### Build Failures
- Verify Directory.Build.props syntax
- Check Dockerfile ARG declarations
- Ensure MSBuild properties are correctly formatted

### Runtime Issues
- Check assembly metadata in VersionController
- Verify environment variables in Docker container
- Review application logs for version-related errors
