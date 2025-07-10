using Microsoft.AspNetCore.Http.Connections;

namespace CheapHelpers.Services.WebServices.Configuration
{
    /// <summary>
    /// Configuration options for WebServiceBase
    /// </summary>
    public record WebServiceOptions
    {
        public string BaseEndpoint { get; init; } = "https://mysite.com/";
        public int TimeoutMinutes { get; init; } = 5;
        public bool CreateHub { get; init; } = false;
        public HttpTransportType SignalRTransportType { get; init; } = HttpTransportType.WebSockets;
        public bool EnableSignalRLogging { get; init; } = false;

        // Authentication settings
        public bool UseAuthentication { get; init; } = false;
        public string TokenEndpoint { get; init; } = "https://login.microsoftonline.com/<TOKEN>/oauth2/v2.0/token";
        public string ClientId { get; init; } = "";
        public string ClientSecret { get; init; } = "";
        public string Scope { get; init; } = "api://<TOKEN>/.default";
    }
}
