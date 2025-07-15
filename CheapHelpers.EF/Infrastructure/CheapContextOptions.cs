namespace CheapHelpers.EF.Infrastructure
{
    public class CheapContextOptions
    {
        public string EnvironmentVariable { get; set; } = "ASPNETCORE_ENVIRONMENT";
        public int DevCommandTimeoutMs { get; set; } = 150000;
        public bool EnableAuditing { get; set; } = true;
        public bool EnableSensitiveDataLogging { get; set; } = true;
    }
}
