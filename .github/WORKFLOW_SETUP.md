# Quick Start: NuGet Publishing Workflow

This guide will get you publishing NuGet packages in 5 minutes.

## Step 1: Get NuGet API Key

1. Go to https://www.nuget.org/account/apikeys
2. Click **Create**
3. Settings:
   - Key Name: `CheapHelpers-GitHub-Actions`
   - Expiration: 365 days
   - Scopes: **Push new packages and package versions**
   - Glob Pattern: `CheapHelpers.*`
4. Click **Create** and copy the key immediately

## Step 2: Add Secret to GitHub

1. Go to GitHub repository settings
2. Navigate: **Settings** → **Secrets and variables** → **Actions**
3. Click **New repository secret**
4. Enter:
   - Name: `NUGET_API_KEY`
   - Value: [paste your key]
5. Click **Add secret**

## Step 3: Publish a Package

### Update Version in .csproj

```xml
<PropertyGroup>
  <Version>1.0.1</Version>
  <PackageVersion>1.0.1</PackageVersion>
</PropertyGroup>
```

### Create and Push Tag

```bash
git checkout master
git pull origin master
git tag -a v1.0.1 -m "Release version 1.0.1"
git push origin v1.0.1
```

### Monitor Workflow

1. Go to **Actions** tab in GitHub
2. Watch **NuGet Package CI/CD** workflow
3. Wait for all jobs to complete
4. Check NuGet.org for your package

## That's It!

Your package is now published automatically.

## Workflow Triggers Summary

| Action | Build | Pack | Publish |
|--------|-------|------|---------|
| Push to master/dev | ✓ | ✓ | ✗ |
| Pull Request | ✓ | ✗ | ✗ |
| Push tag v*.*.* | ✓ | ✓ | ✓ |

## Common Commands

**Test locally before tagging:**
```bash
dotnet build --configuration Release
dotnet pack --configuration Release --output ./nupkgs
```

**Create annotated tag:**
```bash
git tag -a v1.0.1 -m "Release notes here"
git push origin v1.0.1
```

**Delete tag (if mistake):**
```bash
git tag -d v1.0.1
git push origin :refs/tags/v1.0.1
```

## Troubleshooting

**Workflow fails on publish:**
- Check `NUGET_API_KEY` secret is set correctly
- Verify API key has push permissions
- Ensure version doesn't already exist

**Package not appearing:**
- NuGet.org indexing takes 5-10 minutes
- Check workflow logs for errors
- Verify package ID ownership

## Next Steps

- See `.github/workflows/README.md` for detailed documentation
- See `.github/CONTRIBUTING.md` for contribution guidelines
- Create GitHub releases for version history

## Support

Open an issue if you encounter problems with the workflow.
