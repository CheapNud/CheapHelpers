namespace CheapHelpers.Blazor.Helpers
{
    public class SmsError<T> where T : ISmsRecipient
    {
        public T? Recipient { get; set; }
        public required string Message { get; set; }
        public required string PhoneNumber { get; set; }
        public required string Error { get; set; }
        public Exception? Exception { get; set; }
    }
}
