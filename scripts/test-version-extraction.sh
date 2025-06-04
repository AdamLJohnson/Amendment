#!/bin/bash

# Test script to validate version extraction logic
# This simulates what happens in the GitHub Actions workflow

echo "=== Version Extraction Test ==="
echo

# Simulate different Git tag scenarios
test_tags=("v1.0.0" "v2.1.3" "v10.20.30" "v1.0.0-beta" "v2.0.0-rc.1")

for tag in "${test_tags[@]}"; do
    echo "Testing tag: $tag"
    
    # Simulate GITHUB_REF
    GITHUB_REF="refs/tags/$tag"
    
    # Extract version (remove 'v' prefix)
    VERSION=${GITHUB_REF#refs/tags/v}
    
    # Simulate GitHub run number
    BUILD_NUMBER=123
    
    # Set build date to current UTC time
    BUILD_DATE=$(date -u +"%Y-%m-%dT%H:%M:%SZ")
    
    # Create full version with build number
    FULL_VERSION="${VERSION}.${BUILD_NUMBER}"
    
    echo "  Extracted version: $VERSION"
    echo "  Build number: $BUILD_NUMBER"
    echo "  Build date: $BUILD_DATE"
    echo "  Full version: $FULL_VERSION"
    echo
done

echo "=== Testing MSBuild Property Format ==="
echo

# Test MSBuild property format
VERSION="1.2.3"
BUILD_NUMBER="456"
BUILD_DATE="2024-01-15T10:30:45Z"
FULL_VERSION="1.2.3.456"

echo "MSBuild properties that would be passed:"
echo "  -p:VERSION=\"$VERSION\""
echo "  -p:BUILD_NUMBER=\"$BUILD_NUMBER\""
echo "  -p:BUILD_DATE=\"$BUILD_DATE\""
echo "  -p:FULL_VERSION=\"$FULL_VERSION\""
echo

echo "=== Docker Build Args Format ==="
echo

echo "Docker build arguments that would be passed:"
echo "  --build-arg VERSION=$VERSION"
echo "  --build-arg BUILD_NUMBER=$BUILD_NUMBER"
echo "  --build-arg BUILD_DATE=$BUILD_DATE"
echo "  --build-arg FULL_VERSION=$FULL_VERSION"
echo

echo "Test completed successfully!"
