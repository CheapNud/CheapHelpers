# Contributing to CheapHelpers

Thank you for considering contributing to CheapHelpers! This document provides guidelines for contributing to the project.

## Table of Contents

- [Code of Conduct](#code-of-conduct)
- [Getting Started](#getting-started)
- [Development Workflow](#development-workflow)
- [Pull Request Process](#pull-request-process)
- [Package Publishing](#package-publishing)
- [Coding Standards](#coding-standards)
- [Testing Guidelines](#testing-guidelines)
- [Documentation](#documentation)

## Code of Conduct

- Be respectful and constructive in all interactions
- Welcome newcomers and help them get started
- Focus on what is best for the community
- Show empathy towards other community members

## Getting Started

### Prerequisites

- .NET 10.0 SDK or later
- Visual Studio 2022 or Visual Studio Code
- Git for version control
- GitHub account

### Setting Up Development Environment

1. **Fork the repository** on GitHub

2. **Clone your fork**:
   ```bash
   git clone https://github.com/YOUR-USERNAME/CheapHelpers.git
   cd CheapHelpers
   ```

3. **Add upstream remote**:
   ```bash
   git remote add upstream https://github.com/CheapNud/CheapHelpers.git
   ```

4. **Restore dependencies**:
   ```bash
   dotnet restore
   ```

5. **Build the solution**:
   ```bash
   dotnet build
   ```

6. **Run tests**:
   ```bash
   dotnet test
   ```

## Development Workflow

### Branch Strategy

- **`master`**: Stable production code
- **`dev`**: Development branch (default target for PRs)
- **Feature branches**: `feature/your-feature-name`
- **Bug fix branches**: `fix/bug-description`

### Creating a Feature Branch

```bash
# Ensure dev is up to date
git checkout dev
git pull upstream dev

# Create feature branch
git checkout -b feature/your-feature-name
```

### Making Changes

1. Make your changes in logical commits
2. Write clear commit messages
3. Follow existing code style
4. Add tests for new functionality
5. Update documentation as needed

### Commit Message Guidelines

Follow conventional commit format:

```
<type>(<scope>): <subject>

<body>

<footer>
```

**Types**:
- `feat`: New feature
- `fix`: Bug fix
- `docs`: Documentation changes
- `style`: Code style changes (formatting, missing semi-colons, etc.)
- `refactor`: Code refactoring
- `test`: Adding or updating tests
- `chore`: Maintenance tasks

**Examples**:
```
feat(Blazor): Add new MudBlazor dialog helper

Add DialogService extension methods for simplified dialog creation
with common configurations.

Closes #123
```

```
fix(EF): Resolve null reference in repository pattern

Add null checks in generic repository GetById method to prevent
NullReferenceException when entity doesn't exist.
```

### Keeping Your Fork Updated

```bash
# Fetch upstream changes
git fetch upstream

# Merge upstream dev into your local dev
git checkout dev
git merge upstream/dev

# Update your fork on GitHub
git push origin dev
```

## Pull Request Process

### Before Submitting

- [ ] Code builds without errors
- [ ] All tests pass
- [ ] New tests added for new functionality
- [ ] Documentation updated
- [ ] Code follows project style guidelines
- [ ] No unnecessary dependencies added
- [ ] Commits are logical and well-described

### Submitting a Pull Request

1. **Push your branch** to your fork:
   ```bash
   git push origin feature/your-feature-name
   ```

2. **Create Pull Request** on GitHub:
   - Go to the original CheapHelpers repository
   - Click "New Pull Request"
   - Select your fork and branch
   - Target branch: `dev` (usually)

3. **Fill out PR template**:
   - Clear description of changes
   - Reference related issues
   - List breaking changes (if any)
   - Add screenshots/examples if relevant

4. **Wait for review**:
   - Address reviewer feedback
   - Make requested changes
   - Push updates to your branch

### Pull Request Template

```markdown
## Description
Brief description of what this PR does

## Type of Change
- [ ] Bug fix (non-breaking change fixing an issue)
- [ ] New feature (non-breaking change adding functionality)
- [ ] Breaking change (fix or feature causing existing functionality to break)
- [ ] Documentation update

## Related Issues
Fixes #(issue number)

## Changes Made
- List key changes
- One per line
- Be specific

## Testing
Describe how you tested these changes

## Checklist
- [ ] Code builds without errors
- [ ] Tests pass
- [ ] New tests added
- [ ] Documentation updated
- [ ] Follows coding standards
```

## Package Publishing

### Who Can Publish

Only repository maintainers with access to NuGet API keys can publish packages to NuGet.org.

### Publishing Process

Publishing is **fully automated** via GitHub Actions. Manual publishing is not required.

#### For Maintainers

1. **Merge approved PRs** to `master` branch

2. **Update version numbers** in `.csproj` files:
   ```xml
   <PropertyGroup>
     <Version>1.2.3</Version>
     <PackageVersion>1.2.3</PackageVersion>
   </PropertyGroup>
   ```

3. **Create and push version tag**:
   ```bash
   git checkout master
   git pull origin master
   git tag -a v1.2.3 -m "Release version 1.2.3"
   git push origin v1.2.3
   ```

4. **GitHub Actions handles**:
   - Building all projects
   - Running tests
   - Packing NuGet packages
   - Publishing to NuGet.org

5. **Monitor workflow** in Actions tab

6. **Create GitHub Release**:
   - Go to Releases page
   - Draft new release
   - Use existing tag
   - Write release notes
   - Publish

### Versioning Guidelines

Follow [Semantic Versioning 2.0.0](https://semver.org/):

**Given version MAJOR.MINOR.PATCH**:

- **MAJOR**: Incompatible API changes
- **MINOR**: Backwards-compatible new features
- **PATCH**: Backwards-compatible bug fixes

**Examples**:
- `v1.0.0` → `v1.0.1`: Bug fix
- `v1.0.1` → `v1.1.0`: New feature
- `v1.1.0` → `v2.0.0`: Breaking change

### Pre-Release Versions

For testing before official release:

```bash
git tag v1.2.3-beta.1
git push origin v1.2.3-beta.1
```

Modify workflow to publish pre-releases if needed.

### Package Metadata

Ensure all packable projects have complete metadata:

```xml
<PropertyGroup>
  <TargetFramework>net10.0</TargetFramework>
  <IsPackable>true</IsPackable>
  <Version>1.2.3</Version>
  <PackageId>CheapHelpers.Blazor</PackageId>
  <Title>CheapHelpers Blazor Utilities</Title>
  <Authors>CheapNud</Authors>
  <Company>CheapNud</Company>
  <Description>Blazor and MudBlazor utility extensions</Description>
  <PackageProjectUrl>https://github.com/CheapNud/CheapHelpers</PackageProjectUrl>
  <RepositoryUrl>https://github.com/CheapNud/CheapHelpers.git</RepositoryUrl>
  <RepositoryType>git</RepositoryType>
  <PackageLicenseFile>LICENSE.txt</PackageLicenseFile>
  <PackageReadmeFile>README.md</PackageReadmeFile>
  <PackageTags>blazor;mudblazor;helpers;utilities</PackageTags>
  <PackageReleaseNotes>See https://github.com/CheapNud/CheapHelpers/releases</PackageReleaseNotes>
</PropertyGroup>
```

## Coding Standards

### General Guidelines

- Write clean, readable, maintainable code
- Follow existing code patterns
- Use meaningful variable and method names
- Add XML documentation comments for public APIs
- Keep methods focused and small
- Avoid deep nesting

### C# Specific

- **Target Framework**: .NET 10.0
- **Language Version**: C# 13
- **Nullable Reference Types**: Enabled
- **Primary Constructors**: Preferred for simple cases
- **Collection Expressions**: Use for initialization
- **Async/Await**: Use consistently, avoid `async void`
- **Constants**: Extract hardcoded values
- **Naming**: Follow .NET naming conventions

### Variable Naming

Avoid generic platform names:
- ❌ `value`, `code`, `context`, `data`, `result`
- ✓ `userName`, `errorCode`, `httpContext`, `customerData`, `queryResult`

### Blazor/MudBlazor Specific

- Prefer MudBlazor components over HTML tags
- Always specify `T` parameter for `MudList`, `MudSwitch`, etc.
- Use MudBlazor styling system (no inline CSS)
- Never use `<script>` tags in `.razor` components
- Avoid `UserManager`/`RoleManager` in Blazor Server
- Follow MudBlazor 8.10.0 syntax

### Example Code Style

```csharp
namespace CheapHelpers.Blazor.Services;

/// <summary>
/// Provides helper methods for dialog management in Blazor applications.
/// </summary>
public class DialogHelper(IDialogService dialogService)
{
    private readonly IDialogService _dialogService = dialogService;

    /// <summary>
    /// Shows a confirmation dialog with customizable options.
    /// </summary>
    /// <param name="title">The dialog title.</param>
    /// <param name="message">The confirmation message.</param>
    /// <returns>True if user confirmed, false otherwise.</returns>
    public async Task<bool> ShowConfirmationAsync(string title, string message)
    {
        var parameters = new DialogParameters
        {
            { "Message", message }
        };

        var dialogReference = await _dialogService.ShowAsync<ConfirmationDialog>(
            title,
            parameters);

        var dialogResult = await dialogReference.Result;
        return !dialogResult.Canceled;
    }
}
```

## Testing Guidelines

### Test Framework

- **Unit Tests**: xUnit
- **Blazor Components**: BUnit
- **Mocking**: Moq

### Test Structure

Follow AAA pattern:
```csharp
[Fact]
public void MethodName_Scenario_ExpectedBehavior()
{
    // Arrange
    var sut = new SystemUnderTest();
    var expectedValue = 42;

    // Act
    var actualValue = sut.CalculateValue();

    // Assert
    Assert.Equal(expectedValue, actualValue);
}
```

### Coverage Goals

- Aim for high coverage of public APIs
- Focus on business logic
- Test edge cases and error conditions
- Don't test framework code

### Example BUnit Test

```csharp
public class MyComponentTests : TestContext
{
    [Fact]
    public void MyComponent_RendersCorrectly()
    {
        // Arrange
        var expectedText = "Hello World";

        // Act
        var cut = RenderComponent<MyComponent>(parameters => parameters
            .Add(p => p.Text, expectedText));

        // Assert
        cut.Find("p").TextContent.Should().Be(expectedText);
    }
}
```

## Documentation

### XML Documentation

Add XML comments for all public APIs:

```csharp
/// <summary>
/// Converts a string to title case.
/// </summary>
/// <param name="input">The input string to convert.</param>
/// <returns>The string in title case.</returns>
/// <exception cref="ArgumentNullException">Thrown when input is null.</exception>
public static string ToTitleCase(string input)
{
    // Implementation
}
```

### README Updates

Update README.md when:
- Adding new packages
- Adding significant features
- Changing usage patterns
- Updating dependencies

### Code Comments

- Explain **why**, not **what**
- Document complex algorithms
- Note any workarounds or limitations
- Keep comments up-to-date with code

### Examples

Provide usage examples for new features:

```csharp
// Example: Using the DialogHelper
var confirmed = await dialogHelper.ShowConfirmationAsync(
    "Delete Item",
    "Are you sure you want to delete this item?");

if (confirmed)
{
    await DeleteItemAsync();
}
```

## Questions?

- Open an issue for bugs or feature requests
- Start a discussion for questions or ideas
- Contact maintainers for sensitive matters

## Recognition

Contributors will be recognized in:
- Release notes
- Contributors section (when added)
- Commit history

Thank you for contributing to CheapHelpers!
