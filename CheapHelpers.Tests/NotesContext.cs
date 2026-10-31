using Microsoft.EntityFrameworkCore;

namespace CheapHelpers.Tests;

public sealed class NotesContext(DbContextOptions<NotesContext> options) : DbContext(options)
{
    public DbSet<Note> Notes => Set<Note>();
}
