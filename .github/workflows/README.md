# GitHub Actions Workflows

This directory contains the continuous integration workflows for the CheapHelpers project.

## Workflows Overview

### 1. .NET Build (`dotnet.yml`)

Basic build workflow for continuous integration.

- Triggers on: Pushes and PRs to `master`
- Actions: Restore, build, and test `CheapHelpers.CI.slnx` (the CI solution excludes MAUI and MediaProcessing)

### 2. CodeQL Security Scanning (`codeql.yml`)

- Triggers on: Pushes and PRs to `master`/`dev`, weekly on Monday at 8:00 UTC, and manual dispatch
- Actions: Builds `CheapHelpers.CI.slnx` in Release and runs the C# `security-and-quality` queries

### 3. Dependency Review (`dependency-review.yml`)

- Triggers on: PRs to `master`/`dev`
- Actions: Fails on dependencies with moderate or worse vulnerabilities or a license outside the allow list, and posts a summary on the PR

## Versions

Package versions come from the release tag. No `.csproj` file sets a version; a local `dotnet pack` without a version yields `0.0.0-local`.

Follow [Semantic Versioning](https://semver.org/) (MAJOR.MINOR.PATCH) when choosing a tag:
- **MAJOR**: Breaking changes
- **MINOR**: New features (backwards compatible)
- **PATCH**: Bug fixes

## Monitoring Workflows

### View Workflow Runs

1. Go to **Actions** tab in GitHub
2. Select workflow from left sidebar
3. Click on specific run to view details
4. Expand steps to see logs

### Troubleshooting

**Build Failures**
- Check .NET version compatibility
- Verify all projects restore correctly
- Review build logs for errors

## Additional Resources

- [GitHub Actions Documentation](https://docs.github.com/en/actions)
- [.NET CLI Documentation](https://docs.microsoft.com/en-us/dotnet/core/tools/)
- [Semantic Versioning](https://semver.org/)
