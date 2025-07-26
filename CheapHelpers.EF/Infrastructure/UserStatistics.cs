namespace CheapHelpers.EF.Infrastructure
{
    /// <summary>
    /// User statistics data model
    /// </summary>
    public class UserStatistics
    {
        public int TotalUsers { get; set; }
        public int ConfirmedEmails { get; set; }
        public int ActiveUsers { get; set; }
        public double ConfirmationRate { get; set; }
    }
}