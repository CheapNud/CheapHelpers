using System.Text.Json;
using CheapHelpers.Json;

namespace CheapHelpers.Tests;

/// <summary>Pins the lenient number, boolean and string converters and the tolerant enum converter.</summary>
public sealed class JsonConverterTests
{
    public enum Provider
    {
        None,
        OpenAi,
        Whisper
    }

    public sealed class Payload
    {
        public int Count { get; set; }
        public int? MaybeCount { get; set; }
        public long Total { get; set; }
        public double Ratio { get; set; }
        public bool Enabled { get; set; }
        public string? Label { get; set; }
        public Provider Provider { get; set; }
    }

    private static readonly JsonSerializerOptions Options = TolerantJsonConverters.AddAll(new JsonSerializerOptions(JsonSerializerDefaults.Web));

    [Fact]
    public void Numeric_strings_are_read_as_numbers()
    {
        var payload = JsonSerializer.Deserialize<Payload>("""{ "count": "42", "maybeCount": "7", "total": "9000000000", "ratio": "1.5" }""", Options)!;

        Assert.Equal(42, payload.Count);
        Assert.Equal(7, payload.MaybeCount);
        Assert.Equal(9_000_000_000L, payload.Total);
        Assert.Equal(1.5, payload.Ratio);
    }

    [Fact]
    public void Empty_or_bad_strings_and_nulls_become_defaults()
    {
        var payload = JsonSerializer.Deserialize<Payload>("""{ "count": "", "maybeCount": null, "total": "n/a", "ratio": null, "enabled": null }""", Options)!;

        Assert.Equal(0, payload.Count);
        Assert.Null(payload.MaybeCount);
        Assert.Equal(0, payload.Total);
        Assert.Equal(0, payload.Ratio);
        Assert.False(payload.Enabled);
    }

    [Theory]
    [InlineData("true", true)]
    [InlineData("\"True\"", true)]
    [InlineData("1", true)]
    [InlineData("\"0\"", false)]
    [InlineData("\"yes please\"", false)]
    public void Booleans_are_read_from_literals_numbers_and_strings(string json, bool expected)
    {
        Assert.Equal(expected, JsonSerializer.Deserialize<Payload>($$"""{ "enabled": {{json}} }""", Options)!.Enabled);
    }

    [Fact]
    public void Numbers_are_read_as_strings()
    {
        Assert.Equal("42", JsonSerializer.Deserialize<Payload>("""{ "label": 42 }""", Options)!.Label);
        Assert.Equal("2.5", JsonSerializer.Deserialize<Payload>("""{ "label": 2.5 }""", Options)!.Label);
        Assert.Equal("18446744073709551615", JsonSerializer.Deserialize<Payload>("""{ "label": 18446744073709551615 }""", Options)!.Label);
    }

    public sealed class Keyed
    {
        public Dictionary<string, int> ByName { get; set; } = [];
        public Dictionary<int, long> ByNumber { get; set; } = [];
        public Dictionary<double, bool> ByRatio { get; set; } = [];
        public Dictionary<bool, string> ByFlag { get; set; } = [];
        public Dictionary<Provider, int> ByProvider { get; set; } = [];
    }

    [Fact]
    public void Dictionaries_keyed_by_converted_types_round_trip()
    {
        var keyed = new Keyed
        {
            ByName = { ["a"] = 1 },
            ByNumber = { [2] = 3 },
            ByRatio = { [0.5] = true },
            ByFlag = { [true] = "yes" },
            ByProvider = { [Provider.Whisper] = 4 }
        };

        var json = JsonSerializer.Serialize(keyed, Options);
        var roundTripped = JsonSerializer.Deserialize<Keyed>(json, Options)!;

        Assert.Equal(1, roundTripped.ByName["a"]);
        Assert.Equal(3, roundTripped.ByNumber[2]);
        Assert.True(roundTripped.ByRatio[0.5]);
        Assert.Equal("yes", roundTripped.ByFlag[true]);
        Assert.Equal(4, roundTripped.ByProvider[Provider.Whisper]);
        Assert.Contains("\"Whisper\":4", json);
    }

    public enum Small : byte
    {
        Zero,
        One
    }

    [Fact]
    public void Enum_ordinals_outside_the_underlying_range_fall_back_instead_of_truncating()
    {
        Assert.Equal(Small.One, JsonSerializer.Deserialize<Small>("1", Options));
        Assert.Equal(Small.Zero, JsonSerializer.Deserialize<Small>("257", Options));
        Assert.Equal(Small.Zero, JsonSerializer.Deserialize<Small>("-1", Options));
    }

    [Theory]
    [InlineData("\"openai\"", Provider.OpenAi)]
    [InlineData("2", Provider.Whisper)]
    [InlineData("\"RetiredProvider\"", Provider.None)]
    [InlineData("{ \"nested\": true }", Provider.None)]
    public void Enums_accept_any_casing_and_ordinals_and_fall_back_on_unknown_values(string json, Provider expected)
    {
        Assert.Equal(expected, JsonSerializer.Deserialize<Payload>($$"""{ "provider": {{json}}, "count": 3 }""", Options)!.Provider);
    }

    [Fact]
    public void Enum_converter_maps_legacy_names_and_uses_the_given_fallback()
    {
        var options = new JsonSerializerOptions
        {
            Converters = { new TolerantEnumConverter<Provider>(Provider.OpenAi, new Dictionary<string, Provider> { ["whisper-v1"] = Provider.Whisper }) }
        };

        Assert.Equal(Provider.Whisper, JsonSerializer.Deserialize<Provider>("\"WHISPER-V1\"", options));
        Assert.Equal(Provider.OpenAi, JsonSerializer.Deserialize<Provider>("\"gone\"", options));
        Assert.Equal("\"Whisper\"", JsonSerializer.Serialize(Provider.Whisper, options));
    }
}
