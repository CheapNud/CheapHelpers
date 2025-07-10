using Newtonsoft.Json;

namespace CheapHelpers.Models.DTOs.AddressSearch;

public class Name
{
    public string? NameLocale { get; init; }

    [JsonProperty("name")]
    public string? NameString { get; init; }
}
