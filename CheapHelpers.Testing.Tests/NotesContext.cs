using Microsoft.EntityFrameworkCore;

namespace CheapHelpers.Testing.Tests;

public sealed class NotesContext(DbContextOptions<NotesContext> options) : DbContext(options)
{
    public DbSet<Note> Notes => Set<Note>();
}
