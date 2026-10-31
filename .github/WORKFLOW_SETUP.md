# Quick Start: GitHub Workflows

The GitHub workflows build and check the code; they do not publish packages.

## Workflows

| Workflow | Runs on |
|----------|---------|
| .NET Build (`dotnet.yml`) | Pushes and PRs to `master` |
| CodeQL (`codeql.yml`) | Pushes and PRs to `master`/`dev`, weekly, manual |
| Dependency Review (`dependency-review.yml`) | PRs to `master`/`dev` |

## Versions

Package versions come from the release tag. No `.csproj` file sets a version, so a local pack yields `0.0.0-local`.

## Common Commands

**Build and test locally:**
```bash
dotnet build CheapHelpers.CI.slnx --configuration Release
dotnet test CheapHelpers.CI.slnx --configuration Release --no-build
```

## Next Steps

- See `.github/workflows/README.md` for detailed documentation
- See `.github/CONTRIBUTING.md` for contribution guidelines
