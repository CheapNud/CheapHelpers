namespace CheapHelpers.Blazor.Helpers
{
    public class SimplePhoneRecipient : ISmsRecipient
    {
        public required string PhoneNumber { get; set; }
        public string DisplayName { get; set; } = string.Empty;

        public static implicit operator SimplePhoneRecipient(string phoneNumber) =>
            new() { PhoneNumber = phoneNumber };
    }
}