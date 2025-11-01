# GitHub Actions Workflows

This directory contains automated CI/CD workflows for the CheapHelpers project.

## Workflows Overview

### 1. NuGet Package CI/CD (`nuget-publish.yml`)

Comprehensive workflow for building, testing, packaging, and publishing NuGet packages.

#### Workflow Stages

**Stage 1: Build and Test**
- Triggers on: All pushes to `master`/`dev`, pull requests to `master`, tags, and manual dispatch
- Actions:
  - Checks out code with full git history
  - Sets up .NET 10.0
  - Restores dependencies
  - Builds solution in Release configuration
  - Runs tests (gracefully handles projects without tests)
  - Uploads build artifacts for inspection

**Stage 2: Pack**
- Triggers on: Push events and manual workflow dispatch (not on PRs)
- Requires: Successful build-and-test stage
- Actions:
  - Creates NuGet packages from all packable projects
  - Stores packages as artifacts for 90 days
  - Packages available for download from workflow run

**Stage 3: Publish**
- Triggers on: Only when a version tag is pushed (e.g., `v1.2.3`)
- Requires: Successful pack stage
- Actions:
  - Downloads packed NuGet packages
  - Publishes to NuGet.org using API key
  - Skips duplicates automatically

#### When Packages Are Built vs Published

| Event | Build & Test | Pack | Publish |
|-------|--------------|------|---------|
| Push to `master` | ✓ | ✓ | ✗ |
| Push to `dev` | ✓ | ✓ | ✗ |
| Pull Request | ✓ | ✗ | ✗ |
| Tag `v*.*.*` | ✓ | ✓ | ✓ |
| Manual Dispatch | ✓ | ✓ | ✗ |

### 2. .NET Build (`dotnet.yml`)

Basic build workflow for continuous integration.

- Triggers on: Pushes and PRs to `master`
- Actions: Restore, build, and test

## Setting Up NuGet Publishing

### Prerequisites

1. **NuGet.org Account**
   - Create account at https://www.nuget.org
   - Verify your email address

2. **Generate NuGet API Key**
   - Go to https://www.nuget.org/account/apikeys
   - Click "Create"
   - Configure settings:
     - **Key Name**: `CheapHelpers-GitHub-Actions`
     - **Expiration**: Choose appropriate duration (365 days recommended)
     - **Scopes**: Select "Push new packages and package versions"
     - **Glob Pattern**: `CheapHelpers.*` (to limit scope to your packages)
   - Click "Create"
   - **IMPORTANT**: Copy the API key immediately (shown only once)

### Configure GitHub Secret

1. Navigate to your GitHub repository
2. Go to **Settings** → **Secrets and variables** → **Actions**
3. Click **New repository secret**
4. Configure:
   - **Name**: `NUGET_API_KEY`
   - **Value**: Paste your NuGet API key
5. Click **Add secret**

### Security Best Practices

- Never commit API keys to source control
- Use scoped API keys (limit to specific packages)
- Set reasonable expiration dates
- Rotate keys regularly
- Use GitHub environment protection rules for production

## Creating Releases

### Semantic Versioning

Follow [Semantic Versioning](https://semver.org/) (MAJOR.MINOR.PATCH):
- **MAJOR**: Breaking changes
- **MINOR**: New features (backwards compatible)
- **PATCH**: Bug fixes

### Release Process

#### Option 1: Command Line

```bash
# Ensure you're on master branch
git checkout master
git pull origin master

# Create and push version tag
git tag v1.2.3
git push origin v1.2.3
```

#### Option 2: Annotated Tags (Recommended)

```bash
# Create annotated tag with message
git tag -a v1.2.3 -m "Release version 1.2.3 - Description of changes"
git push origin v1.2.3
```

#### Option 3: GitHub Releases UI

1. Go to repository **Releases** page
2. Click **Draft a new release**
3. Click **Choose a tag** → Type new tag (e.g., `v1.2.3`)
4. Set **Target** to `master` branch
5. Add release title and description
6. Click **Publish release**

### Pre-Release Versions

For beta/alpha releases:

```bash
git tag v1.2.3-beta.1
git push origin v1.2.3-beta.1
```

Note: Modify workflow if you want different handling for pre-release tags.

## Version Management

### Project File Versioning

Ensure your `.csproj` files have proper version settings:

```xml
<PropertyGroup>
  <Version>1.2.3</Version>
  <PackageVersion>1.2.3</PackageVersion>
  <AssemblyVersion>1.2.3.0</AssemblyVersion>
  <FileVersion>1.2.3.0</FileVersion>
</PropertyGroup>
```

### Automated Versioning (Optional)

Consider using GitVersion or similar tools:

```yaml
# Add to workflow before pack step
- name: Install GitVersion
  uses: gittools/actions/gitversion/setup@v0
  with:
    versionSpec: '5.x'

- name: Determine Version
  uses: gittools/actions/gitversion/execute@v0
  with:
    useConfigFile: true
```

## Monitoring Workflows

### View Workflow Runs

1. Go to **Actions** tab in GitHub
2. Select workflow from left sidebar
3. Click on specific run to view details
4. Expand steps to see logs

### Download Artifacts

1. Navigate to completed workflow run
2. Scroll to **Artifacts** section
3. Download `nuget-packages` to inspect before publishing

### Troubleshooting

**Build Failures**
- Check .NET version compatibility
- Verify all projects restore correctly
- Review build logs for errors

**Pack Failures**
- Ensure projects have `<IsPackable>true</IsPackable>`
- Verify package metadata is complete
- Check for missing dependencies

**Publish Failures**
- Verify `NUGET_API_KEY` secret is set correctly
- Ensure API key has push permissions
- Check if package version already exists (duplicate)
- Verify package ID ownership on NuGet.org

## Manual Workflow Dispatch

You can manually trigger workflows:

1. Go to **Actions** tab
2. Select **NuGet Package CI/CD** workflow
3. Click **Run workflow** dropdown
4. Select branch
5. Click **Run workflow** button

Useful for testing or creating packages without pushing tags.

## Workflow Customization

### Change .NET Version

Edit `env.DOTNET_VERSION` in `nuget-publish.yml`:

```yaml
env:
  DOTNET_VERSION: '9.0.x'  # Change as needed
```

### Add Additional Branches

Modify trigger branches:

```yaml
on:
  push:
    branches: [ master, dev, release/* ]
```

### Publish to Multiple Sources

Add additional push commands:

```yaml
- name: Publish to GitHub Packages
  run: dotnet nuget push "./nupkgs/*.nupkg" --api-key ${{ secrets.GITHUB_TOKEN }} --source https://nuget.pkg.github.com/OWNER/index.json
```

### Add Code Coverage

```yaml
- name: Run tests with coverage
  run: dotnet test --configuration ${{ env.CONFIGURATION }} --no-build --collect:"XPlat Code Coverage"

- name: Upload coverage
  uses: codecov/codecov-action@v3
```

## Additional Resources

- [GitHub Actions Documentation](https://docs.github.com/en/actions)
- [.NET CLI Documentation](https://docs.microsoft.com/en-us/dotnet/core/tools/)
- [NuGet Package Publishing](https://docs.microsoft.com/en-us/nuget/quickstart/create-and-publish-a-package-using-the-dotnet-cli)
- [Semantic Versioning](https://semver.org/)
