using System.Dynamic;
using System.Text;
using CheapHelpers.Services.DataExchange.Csv;
using CsvHelper;

namespace CheapHelpers.Services.Tests;

public partial class CsvServiceTests : IDisposable
{
    private const string NewLine = "\r\n";

    private readonly CsvService _service = new();
    private readonly string _folder = Directory.CreateTempSubdirectory("csvservice-").FullName;

    /// <summary>Removes the temp folder the file exports wrote into.</summary>
    public void Dispose()
    {
        Directory.Delete(_folder, recursive: true);
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task ExportToBytes_PositionalRecord_WritesHeaderAndEscapesFormula()
    {
        SessionRow[] rows = [new("2026-01-02", "Chess", "=cmd()")];

        var csv = Decode(await _service.ExportToBytesAsync(rows, culture: "en-US", delimiter: ","));

        Assert.Equal($"Date,Game,Notes{NewLine}2026-01-02,Chess,\"'=cmd()\"{NewLine}", csv);
    }

    [Fact]
    public async Task ExportToBytes_PlainClassDefaults_KeepsNumbersAndCultureDates()
    {
        AmountRow[] rows = [new()];

        var csv = Decode(await _service.ExportToBytesAsync(rows));

        Assert.Equal($"Amount;When;Code{NewLine}-5,5;2/01/2026 13:04:05;\"'-5\"{NewLine}", csv);
    }

    [Fact]
    public async Task ExportToBytes_Empty_WritesHeaderOnly()
    {
        var csv = Decode(await _service.ExportToBytesAsync(Array.Empty<SessionRow>()));

        Assert.Equal($"Date;Game;Notes{NewLine}", csv);
    }

    [Fact]
    public async Task Export_Strings_EscapesAndAppends()
    {
        var path = Path.Combine(_folder, "lines.csv");
        string[] lines = ["a", "-5", "=cmd"];

        await _service.Export(path, lines);
        Assert.Equal(["a", "\"'-5\"", "\"'=cmd\""], await File.ReadAllLinesAsync(path, TestContext.Current.CancellationToken));

        await _service.Export(path, lines);
        Assert.Equal(6, (await File.ReadAllLinesAsync(path, TestContext.Current.CancellationToken)).Length);
    }

    [Fact]
    public async Task Export_Expandos_UsesFirstRecordKeysAsHeader()
    {
        var path = Path.Combine(_folder, "expando.csv");
        List<dynamic> rows = [CreateExpando("Chess", 2), CreateExpando("Go", 3)];

        await _service.Export(path, rows);

        Assert.Equal(["Game;Players", "Chess;2", "Go;3"], await File.ReadAllLinesAsync(path, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Export_PlainDynamicObjects_WritesThroughReflection()
    {
        var path = Path.Combine(_folder, "plain.csv");
        List<dynamic> rows = [new SampleRow("Chess", 2), new SampleRow("Go", 3)];

        await _service.Export(path, rows);

        Assert.Equal(["Game;Players", "Chess;2", "Go;3"], await File.ReadAllLinesAsync(path, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Export_DynamicNullFirstRecord_Throws()
    {
        List<dynamic?> rows = [null, new SampleRow("Go", 3)];

        await Assert.ThrowsAsync<ArgumentException>(() => _service.Export(Path.Combine(_folder, "null.csv"), rows!));
    }

    [Fact]
    public async Task ExportToBytes_CsvRecordType_UsesGeneratedWriter()
    {
        GeneratedRow[] rows = [new() { Game = "Chess", Players = 2 }];

        var csv = Decode(await _service.ExportToBytesAsync(rows));

        Assert.True(CsvRecordWriter.IsRegistered<GeneratedRow>());
        Assert.Equal($"Game;Players{NewLine}Chess;2{NewLine}", csv);
    }

    [Fact]
    public async Task ExportToBytes_Scalars_WritesOneValuePerRecord()
    {
        int[] numbers = [1, -2];
        string[] texts = ["a", "-5"];

        Assert.Equal($"1{NewLine}-2{NewLine}", Decode(await _service.ExportToBytesAsync(numbers)));
        Assert.Equal($"a{NewLine}\"'-5\"{NewLine}", Decode(await _service.ExportToBytesAsync(texts)));
    }

    [Fact]
    public async Task ExportToBytes_TwoCharacterDelimiter_Throws()
    {
        SessionRow[] rows = [new("2026-01-02", "Chess", "")];

        await Assert.ThrowsAsync<ArgumentException>(() => _service.ExportToBytesAsync(rows, delimiter: ";;"));
    }

    private static string Decode(byte[] bytes) => Encoding.UTF8.GetString(bytes);

    private static ExpandoObject CreateExpando(string game, int players)
    {
        var row = new ExpandoObject();
        IDictionary<string, object?> fields = row;
        fields["Game"] = game;
        fields["Players"] = players;
        return row;
    }

    public record SessionRow(string Date, string Game, string Notes);

    public record SampleRow(string Game, int Players);

    public class AmountRow
    {
        public decimal Amount { get; set; } = -5.5m;

        public DateTime When { get; set; } = new(2026, 1, 2, 13, 4, 5);

        public string Code { get; set; } = "-5";

        // A getter that is not public stays out of the export
        public string Secret { private get; set; } = "hidden";
    }

    [CsvRecord]
    public partial class GeneratedRow
    {
        public string Game { get; set; } = "";

        public int Players { get; set; }
    }
}
