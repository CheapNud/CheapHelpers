using System.Text.Json;
using CheapHelpers.Extensions;

namespace CheapHelpers.Tests;

/// <summary>Pins that the safe getters return defaults instead of throwing.</summary>
public sealed class JsonElementExtensionsTests
{
    [Fact]
    public void Getters_read_matching_values_and_default_the_rest()
    {
        using var document = JsonDocument.Parse("""{ "name": "Sol", "count": 3, "big": 99999999999, "flag": true, "text": "3" }""");
        var root = document.RootElement;

        Assert.Equal("Sol", root.GetStringOrDefault("name"));
        Assert.Equal("none", root.GetStringOrDefault("count", "none"));
        Assert.Equal(3, root.GetInt32OrDefault("count"));
        Assert.Equal(-1, root.GetInt32OrDefault("big", -1));
        Assert.Equal(-1, root.GetInt32OrDefault("text", -1));
        Assert.True(root.GetBooleanOrDefault("flag"));
        Assert.True(root.GetBooleanOrDefault("missing", true));
        Assert.Null(root.GetPropertyOrNull("missing"));
        Assert.Null(root.GetProperty("name").GetPropertyOrNull("anything"));
    }
}
