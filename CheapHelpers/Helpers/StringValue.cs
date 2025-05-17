namespace CheapHelpers
{
    /// <summary>
    /// Attribute for enums
    /// </summary>
    [System.AttributeUsage(System.AttributeTargets.Field)]
    public class StringValue : System.Attribute
    {
        public StringValue(string value)
        {
            Value = value;
        }

        public string Value { get; }
    }
}
