using System;
using System.Collections.Generic;

namespace CheapHelpers
{
    /// <summary>
    /// Consolidated constants for the CheapHelpers library
    /// </summary>
    public static class Constants
    {
        /// <summary>
        /// Environment and configuration-related constants
        /// </summary>
        public static class Environment
        {
            /// <summary>
            /// Standard ASP.NET Core environment variable name
            /// </summary>
            public const string AspNetCoreEnvironment = "ASPNETCORE_ENVIRONMENT";

            /// <summary>
            /// Development environment name
            /// </summary>
            public const string Development = "Development";

            /// <summary>
            /// Production environment name
            /// </summary>
            public const string Production = "Production";

            /// <summary>
            /// Staging environment name
            /// </summary>
            public const string Staging = "Staging";
        }

        /// <summary>
        /// Configuration keys for appsettings.json and other configuration sources
        /// </summary>
        public static class Configuration
        {
            /// <summary>
            /// Configuration key for default admin user email
            /// </summary>
            public const string UserEmail = "UserEmail";

            /// <summary>
            /// Configuration key for default admin user password
            /// </summary>
            public const string UserPassword = "UserPassword";

            /// <summary>
            /// Configuration key for Azure AD Client ID
            /// </summary>
            public const string ClientId = "ClientId";

            /// <summary>
            /// Configuration key for Azure AD Tenant ID
            /// </summary>
            public const string TenantId = "TenantId";

            /// <summary>
            /// Configuration key for Azure AD Client Secret
            /// </summary>
            public const string ClientSecret = "ClientSecret";

            /// <summary>
            /// Configuration key for Azure Storage connection string
            /// </summary>
            public const string StorageConnection = "StorageConnection";

            /// <summary>
            /// Configuration key for Azure SQL connection string
            /// </summary>
            public const string AzureSqlConnection = "AzureSQLConnection";

            /// <summary>
            /// Configuration key for default connection string
            /// </summary>
            public const string DefaultConnection = "DefaultConnection";

            /// <summary>
            /// Configuration key for Vision API endpoint
            /// </summary>
            public const string VisionEndpoint = "VisionEndpoint";

            /// <summary>
            /// Configuration key for Vision API key
            /// </summary>
            public const string VisionKey = "VisionKey";

            /// <summary>
            /// Configuration key for Translation service key
            /// </summary>
            public const string TranslationKey = "TranslationKey";

            /// <summary>
            /// Configuration key for Translation service endpoint
            /// </summary>
            public const string TranslationEndpoint = "TranslationEndpoint";

            /// <summary>
            /// Configuration key for Translation document endpoint
            /// </summary>
            public const string TranslationDocumentEndpoint = "TranslationDocumentEndpoint";

            /// <summary>
            /// Configuration key for Azure Maps API key
            /// </summary>
            public const string MapsKey = "MapsKey";

            /// <summary>
            /// Configuration key for Azure Maps Client ID
            /// </summary>
            public const string MapsClientId = "MapsClientId";

            /// <summary>
            /// Configuration key for Azure Maps endpoint
            /// </summary>
            public const string MapsEndpoint = "MapsEndpoint";
        }

        /// <summary>
        /// Authentication and authorization-related constants
        /// </summary>
        public static class Authentication
        {
            /// <summary>
            /// Default admin role name
            /// </summary>
            public const string AdminRole = "Admin";

            /// <summary>
            /// Default user role name
            /// </summary>
            public const string UserRole = "User";

            /// <summary>
            /// Error message when user is not authenticated
            /// </summary>
            public const string UserNotAuthenticatedMessage = "user not authenticated";

            /// <summary>
            /// Navigation state JSON column name in database
            /// </summary>
            public const string NavigationStateJsonColumn = "NavigationStateJson";

            /// <summary>
            /// Default JSON value for empty navigation state
            /// </summary>
            public const string EmptyJsonObject = "{}";

            /// <summary>
            /// Prefix for expand state keys in navigation state
            /// </summary>
            public const string ExpandPrefix = "Expand";

            /// <summary>
            /// Allowed username characters for ASP.NET Identity
            /// </summary>
            public const string AllowedUserNameCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";
        }

        /// <summary>
        /// Database-related constants including column types and names
        /// </summary>
        public static class Database
        {
            /// <summary>
            /// TEXT column type (SQLite)
            /// </summary>
            public const string TextColumnType = "TEXT";

            /// <summary>
            /// NVARCHAR column type (SQL Server)
            /// </summary>
            public const string NVarCharColumnType = "NVARCHAR";

            /// <summary>
            /// VARCHAR column type
            /// </summary>
            public const string VarCharColumnType = "VARCHAR";

            /// <summary>
            /// DATETIME column type
            /// </summary>
            public const string DateTimeColumnType = "DATETIME";

            /// <summary>
            /// SQLite function to get current UTC datetime
            /// </summary>
            public const string SqliteUtcNowFunction = "DATETIME('now')";

            /// <summary>
            /// SQL Server function to get current UTC datetime
            /// </summary>
            public const string SqlServerUtcNowFunction = "GETUTCDATE()";

            /// <summary>
            /// PostgreSQL function to get current UTC datetime (also works with TimescaleDB)
            /// </summary>
            public const string NpgsqlUtcNowFunction = "NOW() AT TIME ZONE 'UTC'";

            /// <summary>
            /// Known EF Core database provider names for runtime detection.
            /// </summary>
            public static class ProviderNames
            {
                public const string Sqlite = "Microsoft.EntityFrameworkCore.Sqlite";
                public const string SqlServer = "Microsoft.EntityFrameworkCore.SqlServer";
                public const string Npgsql = "Npgsql.EntityFrameworkCore.PostgreSQL";
            }

            /// <summary>
            /// Index name prefix for FileAttachments table
            /// </summary>
            public const string FileAttachmentsIndexPrefix = "IX_FileAttachments_";

            /// <summary>
            /// Index name for FileAttachments CreatedAt column
            /// </summary>
            public const string FileAttachmentsCreatedAtIndex = "IX_FileAttachments_CreatedAt";

            /// <summary>
            /// Index name for FileAttachments Visible column
            /// </summary>
            public const string FileAttachmentsVisibleIndex = "IX_FileAttachments_Visible";

            /// <summary>
            /// Index name for FileAttachments MimeType column
            /// </summary>
            public const string FileAttachmentsMimeTypeIndex = "IX_FileAttachments_MimeType";

            /// <summary>
            /// Index name for FileAttachments Entity lookup
            /// </summary>
            public const string FileAttachmentsEntityIndex = "IX_FileAttachments_Entity";

            /// <summary>
            /// Index name prefix for Users table
            /// </summary>
            public const string UsersIndexPrefix = "IX_Users_";

            /// <summary>
            /// Index name for Users NavigationState column
            /// </summary>
            public const string UsersNavigationStateIndex = "IX_Users_NavigationState";

            /// <summary>
            /// Index name for InAppNotifications UserId column
            /// </summary>
            public const string NotificationsUserIdIndex = "IX_InAppNotifications_UserId";

            /// <summary>
            /// Index name for InAppNotifications UserId and IsRead composite
            /// </summary>
            public const string NotificationsUserIdIsReadIndex = "IX_InAppNotifications_UserId_IsRead";

            /// <summary>
            /// Index name for InAppNotifications UserId and NotificationType composite
            /// </summary>
            public const string NotificationsUserIdTypeIndex = "IX_InAppNotifications_UserId_NotificationType";

            /// <summary>
            /// Index name for InAppNotifications CreatedAt column
            /// </summary>
            public const string NotificationsCreatedAtIndex = "IX_InAppNotifications_CreatedAt";

            /// <summary>
            /// Index name for InAppNotifications ExpiresAt column
            /// </summary>
            public const string NotificationsExpiresAtIndex = "IX_InAppNotifications_ExpiresAt";

            /// <summary>
            /// Index name for UserNotificationPreferences UserId and NotificationType unique composite
            /// </summary>
            public const string NotificationPreferencesUserIdTypeIndex = "IX_UserNotificationPreferences_UserId_NotificationType";

            // API Key indexes
            public const string ApiKeysKeyHashIndex = "IX_ApiKeys_KeyHash";
            public const string ApiKeysKeyPrefixIndex = "IX_ApiKeys_KeyPrefix";
            public const string ApiKeysUserIdIndex = "IX_ApiKeys_UserId";
            public const string ApiKeysUserIdIsActiveIndex = "IX_ApiKeys_UserId_IsActive";
        }

        /// <summary>
        /// File-related constants including dangerous extensions and file size limits
        /// </summary>
        public static class File
        {
            /// <summary>
            /// Dangerous executable file extensions that should be blocked for security
            /// </summary>
            public static class DangerousExtensions
            {
                // Executables
                public const string Exe = ".exe";
                public const string Com = ".com";
                public const string Bat = ".bat";
                public const string Cmd = ".cmd";
                public const string Msi = ".msi";
                public const string Scr = ".scr";
                public const string Pif = ".pif";
                public const string Application = ".application";

                // Scripts
                public const string Vbs = ".vbs";
                public const string Vbe = ".vbe";
                public const string Js = ".js";
                public const string Jse = ".jse";
                public const string Ws = ".ws";
                public const string Wsf = ".wsf";
                public const string Wsc = ".wsc";
                public const string Wsh = ".wsh";
                public const string Ps1 = ".ps1";
                public const string Ps2 = ".ps2";
                public const string Psc1 = ".psc1";
                public const string Psc2 = ".psc2";

                // Dynamic libraries
                public const string Dll = ".dll";
                public const string So = ".so";
                public const string Dylib = ".dylib";

                // Shell scripts
                public const string Sh = ".sh";
                public const string Bash = ".bash";
                public const string Zsh = ".zsh";
                public const string Fish = ".fish";

                // Java executables
                public const string Jar = ".jar";
                public const string Class = ".class";

                // Microsoft Office macros (older formats with macro support)
                public const string Xlsm = ".xlsm";
                public const string Xlsb = ".xlsb";
                public const string Xltm = ".xltm";
                public const string Xla = ".xla";
                public const string Xlam = ".xlam";
                public const string Docm = ".docm";
                public const string Dotm = ".dotm";
                public const string Doc = ".doc";
                public const string Dot = ".dot";
                public const string Pptm = ".pptm";
                public const string Potm = ".potm";
                public const string Ppam = ".ppam";
                public const string Ppsm = ".ppsm";
                public const string Sldm = ".sldm";
                public const string Ppt = ".ppt";

                // Other dangerous formats
                public const string Hta = ".hta";
                public const string Cpl = ".cpl";
                public const string Msc = ".msc";
                public const string Reg = ".reg";
                public const string Lnk = ".lnk";
                public const string Inf = ".inf";
                public const string Scf = ".scf";

                // Database files (can contain macros)
                public const string Mdb = ".mdb";
                public const string Accdb = ".accdb";
                public const string Mde = ".mde";
                public const string Accde = ".accde";

                /// <summary>
                /// Gets all dangerous extensions as a HashSet for efficient lookup
                /// </summary>
                public static HashSet<string> GetAll() => new(StringComparer.OrdinalIgnoreCase)
                {
                    Exe, Com, Bat, Cmd, Msi, Scr, Pif, Application,
                    Vbs, Vbe, Js, Jse, Ws, Wsf, Wsc, Wsh, Ps1, Ps2, Psc1, Psc2,
                    Dll, So, Dylib,
                    Sh, Bash, Zsh, Fish,
                    Jar, Class,
                    Xlsm, Xlsb, Xltm, Xla, Xlam, Docm, Dotm, Doc, Dot,
                    Pptm, Potm, Ppam, Ppsm, Sldm, Ppt,
                    Hta, Cpl, Msc, Reg, Lnk, Inf, Scf,
                    Mdb, Accdb, Mde, Accde
                };
            }

            /// <summary>
            /// Common file extensions for documents and images
            /// </summary>
            public static class CommonExtensions
            {
                // Images
                public const string Jpg = ".jpg";
                public const string Jpeg = ".jpeg";
                public const string Png = ".png";
                public const string Gif = ".gif";
                public const string Bmp = ".bmp";
                public const string Webp = ".webp";
                public const string Svg = ".svg";
                public const string Tiff = ".tiff";
                public const string Tif = ".tif";

                // Documents
                public const string Pdf = ".pdf";
                public const string Docx = ".docx";
                public const string Xlsx = ".xlsx";
                public const string Pptx = ".pptx";
                public const string Txt = ".txt";
                public const string Csv = ".csv";
                public const string Xml = ".xml";
                public const string Json = ".json";

                // Videos
                public const string Mp4 = ".mp4";
                public const string Mov = ".mov";
                public const string Avi = ".avi";
                public const string Mkv = ".mkv";
                public const string Webm = ".webm";

                // Archives
                public const string Zip = ".zip";
                public const string Rar = ".rar";
                public const string SevenZ = ".7z";
                public const string Tar = ".tar";
                public const string Gz = ".gz";
            }

            /// <summary>
            /// Default file size limits
            /// </summary>
            public static class SizeLimits
            {
                /// <summary>
                /// Default maximum file size in MB
                /// </summary>
                public const int DefaultMaxFileSizeMB = 50;

                /// <summary>
                /// Default maximum file size in bytes (50 MB)
                /// </summary>
                public const long DefaultMaxFileSizeBytes = 52428800; // 50 * 1024 * 1024

                /// <summary>
                /// Bytes per MB conversion constant
                /// </summary>
                public const int BytesPerMB = 1048576; // 1024 * 1024
            }

            /// <summary>
            /// Default accept filter for file uploads
            /// </summary>
            public const string DefaultUploadAcceptFilter = ".jpg,.jpeg,.png,.mov,.mp4,.pdf";

            /// <summary>
            /// Default partial GUID length for file naming
            /// </summary>
            public const int DefaultPartialGuidLength = 8;
        }

        /// <summary>
        /// Validation message constants for exceptions and logging.
        /// User-facing localized equivalents are in CheapHelpers.Blazor/Properties/Language.resx
        /// using the corresponding resource keys (e.g., "FileTooLarge", "NoUploadPath").
        /// </summary>
        public static class Validation
        {
            // Resource keys for IStringLocalizer (user-facing)
            public const string FileTooLargeKey = "FileTooLarge";
            public const string NoUploadPathKey = "NoUploadPath";
            public const string NoExtensionKey = "NoExtension";
            public const string DangerousExtensionKey = "DangerousExtension";
            public const string UnrecognizableFileTypeKey = "UnrecognizableFileType";
            public const string DisallowedFileTypeKey = "DisallowedFileType";
            public const string PathTraversalKey = "PathTraversal";
            public const string SymbolicLinkNotAllowedKey = "SymbolicLinkNotAllowed";
            public const string FilenameNullOrEmptyKey = "FilenameNullOrEmpty";

            // Exception/log message strings (internal)
            public const string FileTooLargeMessage = "File too large";
            public const string NoUploadPathMessage = "no uploadpath set";
            public const string NoExtensionMessage = "Files without extensions are not allowed";
            public const string DangerousExtensionMessageTemplate = "File extension '{0}' is not allowed for security reasons";
            public const string UnrecognizableFileTypeMessageTemplate = "Unable to determine file type for '{0}'. The file may be corrupted, empty, or of an unsupported format.";
            public const string FileTypeDetectionFailedMessageTemplate = "File type detection returned null for '{0}'. This should not happen after IsTypeRecognizable check.";
            public const string DisallowedFileTypeMessageTemplate = "File type '{0}' (MIME: {1}, Extension: .{2}) is not allowed. Detected from file content analysis. Only the following types are permitted: {3}";
            public const string PathTraversalMessageTemplate = "Path traversal detected: attempting to write outside upload directory. Base: {0}, Resolved: {1}";
            public const string SymbolicLinkMessage = "Symbolic links are not allowed in upload paths";
            public const string FileValidationFailedMessageTemplate = "File type validation failed for '{0}': {1}";
            public const string ExtensionMismatchWarningTemplate = "WARNING: Extension mismatch for '{0}': claimed='{1}', detected='.{2}' (Type: {3})";
            public const string FileValidationSuccessTemplate = "File validation passed for '{0}': Type={1}, MIME={2}, Extension=.{3}";
            public const string FilenameParameter = "fileName";
            public const string MaxFileSizeParameter = "MaxFileSizeInMB";
            public const string UploadPathParameter = "UploadPath";
            public const string FilenameNullOrEmptyMessage = "Filename cannot be null or empty";
        }
    }
}
