using Microsoft.EntityFrameworkCore;

namespace CheapHelpers.Tests;

public class SqliteTestDatabaseTests
{
    private const string NoteText = "shared";

    [Fact]
    public async Task Two_contexts_see_the_same_row_on_a_schema_created_without_migrations()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var database = new SqliteTestDatabase<NotesContext>(options => new NotesContext(options), migrate: false);

        await using (var writer = database.CreateContext())
        {
            writer.Notes.Add(new Note { Text = NoteText });
            await writer.SaveChangesAsync(cancellationToken);
        }

        IDbContextFactory<NotesContext> factory = database;
        await using var reader = await factory.CreateDbContextAsync(cancellationToken);
        var note = await reader.Notes.SingleAsync(cancellationToken);
        Assert.Equal(NoteText, note.Text);
    }
}
