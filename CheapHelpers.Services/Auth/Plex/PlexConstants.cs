namespace CheapHelpers.Services.Auth.Plex;

public static class PlexConstants
{
    public const string PlexApiBase = "https://plex.tv/api/v2";
    public const string PlexUsersApi = "https://plex.tv/api/users";
    public const string PlexAuthUrl = "https://app.plex.tv/auth#";

    public static class Headers
    {
        public const string ClientIdentifier = "X-Plex-Client-Identifier";
        public const string Product = "X-Plex-Product";
        public const string Token = "X-Plex-Token";
    }
}
