namespace CheapHelpers.Blazor.Helpers
{
    // Supporting interfaces and classes
    public interface ISmsRecipient
    {
        string PhoneNumber { get; }
        string DisplayName { get; }
    }
}
