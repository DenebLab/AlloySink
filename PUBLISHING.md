# Publishing Guide

This guide explains how to publish AlloySink to NuGet using GitHub Actions.

## Prerequisites

1. **GitHub Repository Secrets**:
   - `NUGET_API_KEY`: Your NuGet API key (get from https://www.nuget.org/account/apikeys)
   - `CODECOV_TOKEN`: (Optional) Codecov token for code coverage

2. **GitHub Environment**:
   - Create a `nuget-production` environment in your GitHub repository settings
   - Add the `NUGET_API_KEY` secret to this environment for additional security

## Publishing Process

### 1. Production Branch Push (Recommended)

1. **Push to Production Branch**:
   ```bash
   git push origin production
   ```

2. **GitHub Actions will automatically**:
   - Install AbcVersion tool for semantic versioning
   - Calculate version using `abcversion -p semversion` 
   - Build and test the library (54 tests ✅)
   - Package the NuGet package with semantic version as `Deneblab.AlloySink`
   - Publish to NuGet.org
   - All automated with zero manual intervention

### 2. Manual Workflow Dispatch

You can also trigger the publishing workflow manually:

1. Go to your repository's Actions tab
2. Select "Publish NuGet Package" workflow  
3. Click "Run workflow"
4. Version will be automatically determined by AbcVersion
5. Click "Run workflow"

## Version Management

The library uses semantic versioning (SemVer):
- **Major.Minor.Patch** (e.g., 1.0.0)
- For pre-release versions, append a suffix (e.g., 1.0.0-beta.1)

### Automatic Versioning with AbcVersion

- **AbcVersion primary**: Uses `abcversion -p semversion` for semantic versioning
- **Environment override**: `PACKAGE_VERSION` environment variable takes priority
- **Fallback**: Default to `1.0.0` if AbcVersion unavailable
- **Cached calculation**: Single version resolution per build for performance
- **No manual tags required**: Semantic versioning based on commit history and .abcversion.json

## Workflows

### 1. CI Workflow (`.github/workflows/ci.yml`)
- Triggers on push to main/develop branches and pull requests
- Builds, tests, and validates the package
- Uploads build artifacts
- Runs code coverage analysis

### 2. NuGet Publish Workflow (`.github/workflows/nuget-publish.yml`)
- Triggers on push to `production` branch
- Installs AbcVersion tool automatically
- Uses semantic versioning for package versions
- Builds, tests, and publishes to NuGet
- Only publishes on push events (not PRs)

### 3. Release Workflow (`.github/workflows/release.yml`)  
- Triggers on git tags starting with 'v'
- Creates GitHub release with automated release notes
- Builds and publishes NuGet package
- Uploads package as release asset

## Testing the Workflow

1. **Test locally first**:
   ```bash
   ./build.sh
   ```

2. **Create a test release**:
   ```bash
   git tag v1.0.0-test
   git push origin v1.0.0-test
   ```

3. **Monitor the GitHub Actions**:
   - Check the Actions tab for workflow execution
   - Verify the package appears on NuGet.org
   - Test the GitHub release creation

## Package Configuration

The NuGet package is configured in `src/AlloySink/AlloySink.csproj`:

- **Package ID**: `Deneblab.AlloySink`
- **Authors**: `Piotr Kudrel`
- **Company**: `DenebLab`
- **License**: `MIT`
- **Repository**: `https://github.com/DenebLab/AlloySink`

## Security Considerations

1. **API Key Security**: Store the NuGet API key in GitHub Secrets
2. **Environment Protection**: Use GitHub Environments for production releases
3. **No Assembly Signing**: Currently disabled for simplicity
4. **Source Link**: Configured for debugging support

## Troubleshooting

### Common Issues

1. **Build Fails**: Check the build logs in GitHub Actions
2. **NuGet Push Fails**: Verify the API key and package version
3. **Version Conflicts**: Ensure the version doesn't already exist on NuGet

### Debug Steps

1. Check the workflow logs in GitHub Actions
2. Verify the package builds locally with `./build.sh`
3. Test the package version extraction
4. Validate the NuGet API key permissions

## Package Validation

After publishing, verify:

1. **NuGet.org**: Package appears at https://www.nuget.org/packages/Deneblab.AlloySink/
2. **Installation**: `dotnet add package Deneblab.AlloySink --version X.X.X`
3. **Functionality**: Test basic usage in a sample project

## Best Practices

1. **Test thoroughly** before creating release tags
2. **Use semantic versioning** consistently
3. **Update release notes** in GitHub releases
4. **Monitor package downloads** and issues
5. **Keep dependencies updated** regularly