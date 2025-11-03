# ContextExtensions - DbContext Extension Methods

The `ContextExtensions` class provides powerful extension methods for Entity Framework Core DbContext and related types, including pagination helpers, bulk deletion, and advanced database operations.

## Overview

ContextExtensions adds functionality to EF Core's DbContext with:

- Pagination extension for IQueryable
- Bulk delete operations with batching
- DbSet clearing operations
- IDENTITY INSERT operations (advanced/dangerous)
- Custom distinct operations
- Static helper for adding entities

## Installation

```bash
dotnet add package CheapHelpers.EF
```

## Extension Methods

### ToPaginatedListAsync

Converts an `IQueryable<T>` to a `PaginatedList<T>`.

```csharp
using CheapHelpers.EF.Extensions;
using Microsoft.EntityFrameworkCore;

// Basic pagination
var query = context.Products.AsNoTracking();
var page = await query.ToPaginatedListAsync(pageIndex: 1, pageSize: 20);

// With filtering
var activeProducts = context.Products
    .Where(p => p.IsActive)
    .AsNoTracking();
var page = await activeProducts.ToPaginatedListAsync(1, 50);

// Access pagination data
Console.WriteLine($"Page {page.PageIndex} of {page.TotalPages}");
Console.WriteLine($"Total items: {page.ResultCount}");

foreach (var product in page)
{
    Console.WriteLine(product.Name);
}
```

**Signature:**
```csharp
static Task<PaginatedList<T>> ToPaginatedListAsync<T>(
    this IQueryable<T> query,
    int? pageIndex = null,
    int pageSize = 10,
    CancellationToken token = default)
    where T : class, IEntityId
```

**Parameters:**
- `query`: The IQueryable source to paginate
- `pageIndex`: Current page number (1-based), defaults to 1
- `pageSize`: Number of items per page, defaults to 10
- `token`: Cancellation token for async operation

**Returns:** `PaginatedList<T>` with items and pagination metadata

**Example with Complex Query:**
```csharp
var searchResults = await context.Products
    .Include(p => p.Category)
    .Where(p => p.Name.Contains(searchTerm))
    .OrderByDescending(p => p.CreatedAt)
    .AsNoTracking()
    .ToPaginatedListAsync(pageIndex: 2, pageSize: 25);
```

### Clear

Removes all entities from a DbSet.

```csharp
using CheapHelpers.EF.Extensions;

// Clear all products
context.Products.Clear();
await context.SaveChangesAsync();

// Clear multiple sets
context.Orders.Clear();
context.OrderItems.Clear();
await context.SaveChangesAsync();
```

**Signature:**
```csharp
static void Clear<T>(this DbSet<T> dbset) where T : class
```

**Warning:** This marks all entities in the DbSet for deletion. Use with caution on large tables - consider `BatchDelete` instead.

**Example:**
```csharp
public async Task ResetCategoryDataAsync()
{
    using var context = contextFactory.CreateDbContext();

    context.Categories.Clear();
    await context.SaveChangesAsync();

    // Now seed new data
    context.Categories.AddRange(GetDefaultCategories());
    await context.SaveChangesAsync();
}
```

### BatchDelete

Efficiently deletes large amounts of data in batches with optional concurrency.

```csharp
using CheapHelpers.EF.Extensions;

// Delete all records with default settings (1000 per batch, 1 concurrent context)
await contextFactory.BatchDelete<Product>();

// Custom batch size
await contextFactory.BatchDelete<Product>(batchsize: 500);

// With concurrency for faster deletion
await contextFactory.BatchDelete<Product>(
    batchsize: 1000,
    concurrentcontextcalls: 4);
```

**Signature:**
```csharp
static Task BatchDelete<T>(
    this IDbContextFactory<DbContext> factory,
    int batchsize = 1000,
    int concurrentcontextcalls = 1)
    where T : class
```

**Parameters:**
- `factory`: DbContext factory for creating multiple contexts
- `batchsize`: Number of records to delete per batch (default: 1000)
- `concurrentcontextcalls`: Number of parallel contexts to use (default: 1)

**Important Notes:**
- Use this for large tables (1000+ records)
- Don't use for small tables - use `Clear()` or `DeleteWhereAsync()` instead
- Logs progress to Debug output
- Each batch runs in separate context
- Higher concurrency = faster deletion but more database load

**Example with Progress Logging:**
```csharp
// Output to Debug window:
// Deleting from DbSet<AuditLog>
// 1000 records deleted from DbSet<AuditLog>
// 2000 records deleted from DbSet<AuditLog>
// ...

await contextFactory.BatchDelete<AuditLog>(batchsize: 2000, concurrentcontextcalls: 2);
```

**When to Use:**
- Clearing audit logs older than X days
- Deleting all records from a large table
- Cleanup operations on staging/development databases
- Data migration cleanup

**Performance Characteristics:**
- Batch size 1000: Good balance for most scenarios
- Batch size 500: Better for heavily indexed tables
- Batch size 2000+: Faster but may lock tables longer
- Concurrency 2-4: Safe for most databases
- Concurrency 5+: Only on powerful database servers

### DistinctByMyself

Custom distinct operation using GroupBy.

```csharp
using CheapHelpers.EF.Extensions;

// Get distinct products by name
var distinctProducts = context.Products
    .AsQueryable()
    .DistinctByMyself(p => p.Name);

foreach (var product in distinctProducts)
{
    Console.WriteLine(product.Name);
}
```

**Signature:**
```csharp
static IEnumerable<T> DistinctByMyself<T>(
    this IQueryable<T> context,
    Func<T, string> selector)
```

**Note:** Consider using EF Core's built-in `DistinctBy` (EF Core 6+) instead for better performance:

```csharp
// Preferred (EF Core 6+):
var distinct = await context.Products
    .DistinctBy(p => p.Name)
    .ToListAsync();
```

### Add

Static extension for adding entities if they don't exist by code.

```csharp
using CheapHelpers.EF.Extensions;

using var context = contextFactory.CreateDbContext();

// Add category if code doesn't exist
var category = await context.Add(new Category
{
    Code = "electronics",
    Name = "Electronics"
});

await context.SaveChangesAsync();
```

**Signature:**
```csharp
static Task<T> Add<T>(this DbContext context, T value)
    where T : class, IEntityCode
```

**Note:** This is a wrapper around `BaseRepo.AddIfNotExistsAsync` for use within an existing context.

**Example in Transaction:**
```csharp
using var context = contextFactory.CreateDbContext();
using var transaction = context.Database.BeginTransaction();

try
{
    var category = await context.Add(new Category { Code = "books", Name = "Books" });
    var subcategory = await context.Add(new Category { Code = "fiction", Name = "Fiction" });

    await context.SaveChangesAsync();
    await transaction.CommitAsync();
}
catch
{
    await transaction.RollbackAsync();
    throw;
}
```

## Advanced/Dangerous Methods

The following methods manipulate SQL Server IDENTITY columns. Use with extreme caution.

### EnableIdentityInsert

Enables IDENTITY INSERT for a table.

```csharp
// DO NOT USE unless you know what you're doing
await context.EnableIdentityInsert<Product>();
```

**Signature:**
```csharp
static Task EnableIdentityInsert<T>(this DbContext context)
```

**Warning:** This allows manual insertion of identity column values. Improper use can cause:
- Primary key conflicts
- Identity seed corruption
- Data integrity issues

### DisableIdentityInsert

Disables IDENTITY INSERT for a table.

```csharp
// DO NOT USE unless you know what you're doing
await context.DisableIdentityInsert<Product>();
```

**Signature:**
```csharp
static Task DisableIdentityInsert<T>(this DbContext context)
```

### SaveChangesWithIdentityInsert

Saves changes with IDENTITY INSERT enabled within a transaction.

```csharp
// DO NOT USE unless you know what you're doing
// Example: Importing data with specific IDs
var importedProduct = new Product
{
    Id = 999,  // Setting ID manually
    Name = "Imported Product",
    Price = 99.99m
};

context.Products.Add(importedProduct);
await context.SaveChangesWithIdentityInsert<Product>();
```

**Signature:**
```csharp
static Task SaveChangesWithIdentityInsert<T>(this DbContext context)
```

**Process:**
1. Begins a transaction
2. Enables IDENTITY INSERT
3. Saves changes
4. Disables IDENTITY INSERT
5. Commits transaction

**Valid Use Cases (Rare):**
- Importing data from another database with preserved IDs
- Seeding test data with specific IDs for testing
- Data migration scenarios where ID preservation is critical

**Why You Probably Shouldn't Use This:**
- Breaks database-generated ID sequences
- Can cause future ID conflicts
- Violates normal EF Core patterns
- Requires manual ID management
- SQL Server specific (won't work on SQLite, PostgreSQL, etc.)

**Alternative Approach:**
```csharp
// Instead of forcing IDs, use a mapping table
public class ImportMapping
{
    public int OldId { get; set; }
    public int NewId { get; set; }
}

// Import without forcing IDs
var product = new Product { Name = "Imported", Price = 99.99m };
context.Products.Add(product);
await context.SaveChangesAsync();

// Store mapping for reference
var mapping = new ImportMapping { OldId = 999, NewId = product.Id };
```

## Usage Patterns

### Pattern 1: Paginated API Endpoint

```csharp
[HttpGet]
public async Task<ActionResult<PaginatedList<ProductDto>>> GetProducts(
    [FromQuery] int page = 1,
    [FromQuery] int pageSize = 20,
    [FromQuery] string? search = null)
{
    var query = context.Products.AsNoTracking();

    if (!string.IsNullOrEmpty(search))
    {
        query = query.Where(p => p.Name.Contains(search));
    }

    var results = await query
        .OrderBy(p => p.Name)
        .ToPaginatedListAsync(page, pageSize);

    return Ok(results);
}
```

### Pattern 2: Cleanup Job

```csharp
public class DatabaseCleanupJob
{
    private readonly IDbContextFactory<AppDbContext> _factory;

    public async Task CleanupOldLogsAsync()
    {
        var cutoffDate = DateTime.UtcNow.AddMonths(-3);

        using var context = _factory.CreateDbContext();

        // Delete old logs
        var oldLogs = await context.AuditLogs
            .Where(log => log.CreatedAt < cutoffDate)
            .ToListAsync();

        if (oldLogs.Count > 10000)
        {
            // Use batch delete for large datasets
            await _factory.BatchDelete<AuditLog>();
        }
        else
        {
            // Use normal delete for smaller datasets
            context.AuditLogs.RemoveRange(oldLogs);
            await context.SaveChangesAsync();
        }
    }
}
```

### Pattern 3: Database Reset (Testing)

```csharp
public class TestDatabaseInitializer
{
    private readonly IDbContextFactory<AppDbContext> _factory;

    public async Task ResetDatabaseAsync()
    {
        using var context = _factory.CreateDbContext();

        // Clear all data
        context.OrderItems.Clear();
        context.Orders.Clear();
        context.Products.Clear();
        context.Categories.Clear();

        await context.SaveChangesAsync();

        // Seed fresh data
        await SeedDataAsync(context);
    }

    private async Task SeedDataAsync(AppDbContext context)
    {
        var electronics = await context.Add(new Category
        {
            Code = "electronics",
            Name = "Electronics"
        });

        var books = await context.Add(new Category
        {
            Code = "books",
            Name = "Books"
        });

        await context.SaveChangesAsync();
    }
}
```

### Pattern 4: Bulk Import with Deduplication

```csharp
public async Task ImportCategoriesAsync(List<CategoryImport> imports)
{
    using var context = _factory.CreateDbContext();
    using var transaction = context.Database.BeginTransaction();

    try
    {
        foreach (var import in imports)
        {
            // Add only if doesn't exist
            var category = await context.Add(new Category
            {
                Code = import.Code,
                Name = import.Name
            });

            Console.WriteLine(category.Id == 0
                ? $"Found existing: {category.Code}"
                : $"Created new: {category.Code}");
        }

        await context.SaveChangesAsync();
        await transaction.CommitAsync();
    }
    catch
    {
        await transaction.RollbackAsync();
        throw;
    }
}
```

### Pattern 5: Performance-Optimized Search

```csharp
public async Task<PaginatedList<Product>> SearchProductsAsync(
    ProductSearchRequest request,
    CancellationToken cancellationToken)
{
    // Build query
    var query = context.Products
        .AsNoTracking()
        .Include(p => p.Category);

    // Apply filters
    if (!string.IsNullOrEmpty(request.SearchTerm))
    {
        query = query.Where(p =>
            p.Name.Contains(request.SearchTerm) ||
            p.Description.Contains(request.SearchTerm));
    }

    if (request.CategoryId.HasValue)
    {
        query = query.Where(p => p.CategoryId == request.CategoryId);
    }

    if (request.MinPrice.HasValue)
    {
        query = query.Where(p => p.Price >= request.MinPrice);
    }

    if (request.MaxPrice.HasValue)
    {
        query = query.Where(p => p.Price <= request.MaxPrice);
    }

    // Apply sorting
    query = request.SortBy switch
    {
        "name" => query.OrderBy(p => p.Name),
        "price_asc" => query.OrderBy(p => p.Price),
        "price_desc" => query.OrderByDescending(p => p.Price),
        "newest" => query.OrderByDescending(p => p.CreatedAt),
        _ => query.OrderBy(p => p.Name)
    };

    // Paginate
    return await query.ToPaginatedListAsync(
        request.Page,
        request.PageSize,
        cancellationToken);
}
```

## Best Practices

### 1. Use ToPaginatedListAsync for User-Facing Data

```csharp
// Always paginate data returned to UI
// BAD:
var products = await context.Products.ToListAsync();
return Ok(products);

// GOOD:
var products = await context.Products
    .AsNoTracking()
    .ToPaginatedListAsync(page, pageSize);
return Ok(products);
```

### 2. Choose the Right Deletion Method

```csharp
// Small dataset (< 1000 records)
context.TempData.Clear();
await context.SaveChangesAsync();

// Medium dataset (1000-10000 records)
await repo.DeleteWhereAsync<TempData>(t => t.IsOld);

// Large dataset (10000+ records)
await contextFactory.BatchDelete<TempData>(batchsize: 2000);
```

### 3. Always Use AsNoTracking for Read-Only Queries

```csharp
// Before pagination
var query = context.Products
    .AsNoTracking()  // Important for performance
    .Where(p => p.IsActive);

var page = await query.ToPaginatedListAsync(1, 20);
```

### 4. Avoid IDENTITY INSERT Methods

```csharp
// BAD (unless you really need it):
await context.SaveChangesWithIdentityInsert<Product>();

// GOOD (let database generate IDs):
context.Products.Add(newProduct);
await context.SaveChangesAsync();
// newProduct.Id is now populated
```

### 5. Use Transactions for Multi-Step Operations

```csharp
using var context = contextFactory.CreateDbContext();
using var transaction = context.Database.BeginTransaction();

try
{
    // Step 1: Clear old data
    context.OrderItems.Clear();
    await context.SaveChangesAsync();

    // Step 2: Add new data
    var category = await context.Add(newCategory);
    await context.SaveChangesAsync();

    // Step 3: Add related data
    context.Products.AddRange(GetProductsForCategory(category.Id));
    await context.SaveChangesAsync();

    await transaction.CommitAsync();
}
catch
{
    await transaction.RollbackAsync();
    throw;
}
```

### 6. Be Careful with BatchDelete Concurrency

```csharp
// Safe for most databases
await factory.BatchDelete<Product>(concurrentcontextcalls: 2);

// Risky - may cause locks or timeouts
await factory.BatchDelete<Product>(concurrentcontextcalls: 10);

// Monitor database performance
// Adjust based on:
// - Database server capacity
// - Table size and indexes
// - Other concurrent operations
```

### 7. Use Proper Page Validation

```csharp
public async Task<PaginatedList<Product>> GetProductsAsync(int page, int pageSize)
{
    // Validate inputs
    if (page < 1) page = 1;
    if (pageSize < 1) pageSize = 10;
    if (pageSize > 100) pageSize = 100;  // Prevent abuse

    return await context.Products
        .AsNoTracking()
        .ToPaginatedListAsync(page, pageSize);
}
```

## Performance Considerations

### ToPaginatedListAsync

- Executes two queries: one for data, one for count
- Count query can be slow on large tables without indexes
- Consider disabling count for very large datasets (modify PaginatedList.CreateAsync)
- Use appropriate indexes on filter columns

**Query Example:**
```sql
-- Data query
SELECT * FROM Products WHERE IsActive = 1 ORDER BY Name OFFSET 20 ROWS FETCH NEXT 10 ROWS ONLY

-- Count query
SELECT COUNT(*) FROM Products WHERE IsActive = 1
```

### BatchDelete

- More efficient than loading entities into memory
- Uses EF Core's `ExecuteDeleteAsync` (EF Core 7+)
- Batch size affects transaction log size
- Higher concurrency uses more database connections
- Monitor tempdb usage for large deletions

### Clear

- Loads all entities into memory first
- Not suitable for large tables
- Uses change tracking overhead
- Good for small reference tables

## Related Documentation

- [Repository](Repository.md) - BaseRepo pattern and CRUD operations
- [PaginatedList](PaginatedList.md) - Pagination implementation details
- [CheapContext](CheapContext.md) - Context configuration and setup
