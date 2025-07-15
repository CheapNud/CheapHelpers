namespace CheapHelpers.Blazor.Helpers
{
    public class SmsResult<T> where T : ISmsRecipient
    {
        public required T Recipient { get; set; }
        public required string Message { get; set; }
        public required string PhoneNumber { get; set; }
        public DateTime SentAt { get; set; }
    }
}
