# FileSetFingerprint

Records the SHA-256 of every file in a directory tree and later checks the tree against that record. Use it to confirm a deploy or install is complete and unmodified, independent of any version number.

Namespace: `CheapHelpers.Helpers.Files`

## Methods

- `BuildAsync(rootDirectory, include = null, cancellationToken)` hashes every file under `rootDirectory` (recursive). `include` receives the relative path and filters files; `null` means all files.
- `BuildAsync(rootDirectory, relativePaths, cancellationToken)` hashes exactly the listed paths. A missing file throws `FileNotFoundException`, and a path outside the root throws `ArgumentException`.
- `VerifyAsync(rootDirectory, recorded, include = null, cancellationToken)` hashes the files on disk and returns a `ManifestDrift` with `Changed`, `Missing` and `Extra` path lists plus `HasDrift`. Missing files are reported, not thrown. `include` limits which on-disk files count as `Extra`.

Files are hashed as streams, so large binaries are not loaded into memory.

## Manifest

`FileManifest.Files` maps relative paths (forward slashes, case-sensitive) to lowercase SHA-256 hex, sorted by path. `FileManifest.Fingerprint` is one SHA-256 over `path\nhash\n` lines in that order, for a quick equality check before a full verify.

`ToJson()` and `FileManifest.FromJson(json)` use this shape, so other languages can write or read it:

```json
{
  "files": {
    "app.dll": "8ed3f6ad685b959ead7022518e1af76cd816f8e8ec7ccdda1ed4018e8f2223f8",
    "runtimes/win-x64/native.dll": "b9dd960c1753459a78115d3cb845a57d924b6877e805b08bd01086ccdf34433c"
  }
}
```

## Example

```csharp
using CheapHelpers.Helpers.Files;

// At publish time
var manifest = await FileSetFingerprint.BuildAsync(publishDir);
await File.WriteAllTextAsync("release.manifest.json", manifest.ToJson());

// At install time
var recorded = FileManifest.FromJson(await File.ReadAllTextAsync("release.manifest.json"));
var drift = await FileSetFingerprint.VerifyAsync(installDir, recorded);
if (drift.HasDrift)
    throw new InvalidOperationException(
        $"Install drift. Changed: {string.Join(", ", drift.Changed)}; missing: {string.Join(", ", drift.Missing)}; extra: {string.Join(", ", drift.Extra)}");
```

Keep the manifest file outside the hashed directory, or exclude it with `include`, or it shows up as `Extra`.
