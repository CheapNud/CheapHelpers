namespace CheapHelpers.Ota;

/// <summary>
/// Where the server reads its releases from.
/// </summary>
public enum OtaSourceKind
{
    /// <summary>The release API of a Gitea or Forgejo repository.</summary>
    Forge,

    /// <summary>The public release endpoints of another instance.</summary>
    Upstream,

    /// <summary>A local folder with one subfolder per version.</summary>
    LocalFolder,
}
