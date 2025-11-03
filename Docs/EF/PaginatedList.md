# PaginatedList - Pagination Implementation

The `PaginatedList<T>` class provides a complete pagination solution for Entity Framework Core queries, inheriting from `List<T>` while adding pagination metadata and navigation properties.

## Overview

PaginatedList is designed to handle large datasets efficiently by:

- Loading only the requested page of data
- Providing total count and page navigation metadata
- Supporting cancellation for long-running queries
- Handling SQL exceptions gracefully
- Offering flexible count options for performance optimization

The class inherits from `List<T>`, so you can use it like a regular list while having access to pagination information.

## Installation

```bash
dotnet add package CheapHelpers.EF
```

## Basic Usage

### Creating a PaginatedList

```csharp
using CheapHelpers.EF.Infrastructure;
using Microsoft.EntityFrameworkCore;

// From IQueryable
var query = context.Products.AsNoTracking();
var page = await PaginatedList<Product>.CreateAsync(
    source: query,
    pageIndex: 1,
    pageSize: 20);

// With extension method (recommended)
using CheapHelpers.EF.Extensions;

var page = await context.Products
    .AsNoTracking()
    .ToPaginatedListAsync(pageIndex: 1, pageSize: 20);
```

### Accessing Items and Metadata

```csharp
// PaginatedList inherits from List<T>
foreach (var product in page)
{
    Console.WriteLine(product.Name);
}

// Pagination metadata
Console.WriteLine($"Page {page.PageIndex} of {page.TotalPages}");
Console.WriteLine($"Total items: {page.ResultCount}");
Console.WriteLine($"Items on this page: {page.Count}");

// Navigation
if (page.HasNextPage)
{
    var nextPage = await query.ToPaginatedListAsync(page.PageIndex + 1, 20);
}

if (page.HasPreviousPage)
{
    var prevPage = await query.ToPaginatedListAsync(page.PageIndex - 1, 20);
}
```

## Properties

### PageIndex

The current page number (1-based).

```csharp
var page = await query.ToPaginatedListAsync(pageIndex: 3, pageSize: 20);
Console.WriteLine(page.PageIndex);  // Output: 3
```

**Type:** `int`

**Note:** First page is 1, not 0.

### TotalPages

The total number of pages based on result count and page size.

```csharp
var page = await query.ToPaginatedListAsync(pageIndex: 1, pageSize: 20);
Console.WriteLine($"Total pages: {page.TotalPages}");

// Example: 95 total items / 20 per page = 5 pages
```

**Type:** `int`

**Calculation:** `Math.Ceiling(ResultCount / (double)pageSize)`

### ResultCount

The total number of items in the query (before pagination).

```csharp
var page = await query.ToPaginatedListAsync(pageIndex: 1, pageSize: 20);
Console.WriteLine($"Total items in database: {page.ResultCount}");

// Note: This is the total count, not the count on current page
// Use page.Count for items on current page
```

**Type:** `int`

**Note:** This requires a COUNT query to the database. For large datasets, this can be expensive.

### HasPreviousPage

Indicates if there is a previous page.

```csharp
if (page.HasPreviousPage)
{
    Console.WriteLine("There are previous pages");
}

// Always false for page 1
```

**Type:** `bool`

**Calculation:** `PageIndex > 1`

### HasNextPage

Indicates if there is a next page.

```csharp
if (page.HasNextPage)
{
    Console.WriteLine("There are more pages");
}

// False when on last page
```

**Type:** `bool`

**Calculation:** `PageIndex < TotalPages`

### Count (Inherited from List)

Number of items on the current page.

```csharp
var page = await query.ToPaginatedListAsync(pageIndex: 1, pageSize: 20);
Console.WriteLine($"Items on this page: {page.Count}");

// Usually equals pageSize, except on the last page
```

**Type:** `int`

**Example:**
- Total items: 95
- Page size: 20
- Page 1-4: Count = 20
- Page 5: Count = 15

## Static Factory Methods

### CreateAsync (with count)

Creates a paginated list with total count calculation.

```csharp
var page = await PaginatedList<Product>.CreateAsync(
    source: context.Products.AsNoTracking(),
    pageIndex: 2,
    pageSize: 50,
    token: cancellationToken);
```

**Signature:**
```csharp
static Task<PaginatedList<T>> CreateAsync(
    IQueryable<T> source,
    int pageIndex,
    int pageSize,
    CancellationToken token = default)
```

**Parameters:**
- `source`: The IQueryable to paginate
- `pageIndex`: Current page number (1-based)
- `pageSize`: Number of items per page
- `token`: Cancellation token

**Process:**
1. Calculates skip count: `(pageIndex - 1) * pageSize`
2. Executes data query: `source.Skip(skipCount).Take(pageSize)`
3. Executes count query: `source.Count()`
4. Creates PaginatedList with results

**Database Queries:**
```sql
-- Data query
SELECT * FROM Products OFFSET 50 ROWS FETCH NEXT 50 ROWS ONLY

-- Count query
SELECT COUNT(*) FROM Products
```

### CreateAsync (with optional count)

Creates a paginated list with optional total count calculation for performance optimization.

```csharp
// With count (default)
var page = await PaginatedList<Product>.CreateAsync(
    source: context.Products.AsNoTracking(),
    pageIndex: 1,
    pageSize: 20,
    count: true,
    token: cancellationToken);

// Without count (faster, but no total pages/count)
var page = await PaginatedList<Product>.CreateAsync(
    source: context.Products.AsNoTracking(),
    pageIndex: 1,
    pageSize: 20,
    count: false,
    token: cancellationToken);

Console.WriteLine(page.ResultCount);  // 0 when count=false
Console.WriteLine(page.TotalPages);   // 1 when count=false
```

**Signature:**
```csharp
static Task<PaginatedList<T>> CreateAsync(
    IQueryable<T> source,
    int pageIndex,
    int pageSize,
    bool count,
    CancellationToken token = default)
```

**Parameters:**
- `source`: The IQueryable to paginate
- `pageIndex`: Current page number (1-based)
- `pageSize`: Number of items per page
- `count`: Whether to perform count operation
- `token`: Cancellation token

**When count = false:**
- `ResultCount` = 0
- `TotalPages` = 1
- `HasNextPage` = false (since TotalPages = 1)
- Only one database query (data, no count)

**Use case:** When you don't need pagination metadata (e.g., "Load More" button instead of page numbers)

## Constructors

### Primary Constructor

```csharp
// Used internally by CreateAsync
var page = new PaginatedList<Product>(
    source: productList,
    count: 100,
    pageIndex: 1,
    pageSize: 20);
```

**Signature:**
```csharp
public PaginatedList(List<T> source, int count, int pageIndex, int pageSize)
```

### Default Constructor

```csharp
// Creates empty paginated list
var emptyPage = new PaginatedList<Product>();

Console.WriteLine(emptyPage.Count);        // 0
Console.WriteLine(emptyPage.PageIndex);    // 0
Console.WriteLine(emptyPage.TotalPages);   // 1
Console.WriteLine(emptyPage.ResultCount);  // 0
```

**Use case:** Returning empty result when no data found

## Exception Handling

### TaskCanceledException

Properly propagates cancellation requests.

```csharp
using var cts = new CancellationTokenSource();
cts.CancelAfter(TimeSpan.FromSeconds(5));

try
{
    var page = await query.ToPaginatedListAsync(1, 20, cts.Token);
}
catch (TaskCanceledException)
{
    Console.WriteLine("Query was cancelled");
}
```

### SqlException

Logs SQL errors with cancellation state.

```csharp
try
{
    var page = await query.ToPaginatedListAsync(1, 20);
}
catch (SqlException ex)
{
    // Debug output includes:
    // - Cancellation state
    // - SQL error message
    Console.WriteLine($"SQL Error: {ex.Message}");
}
```

**Debug Output:**
```
SQL Exception - Cancellation Requested: False
SQL Exception Message: Invalid column name 'InvalidColumn'.
```

### General Exceptions

All exceptions are logged and re-thrown.

```csharp
try
{
    var page = await query.ToPaginatedListAsync(1, 20);
}
catch (Exception ex)
{
    // Debug output includes:
    // - Cancellation state
    // - Exception message
    throw;
}
```

**Debug Output:**
```
General Exception - Cancellation Requested: False
Exception Message: An error occurred while processing the query.
```

## Common Patterns

### Pattern 1: Web API Pagination

```csharp
[HttpGet]
public async Task<ActionResult<PaginatedResponse<ProductDto>>> GetProducts(
    [FromQuery] int page = 1,
    [FromQuery] int pageSize = 20)
{
    // Validate
    if (page < 1) page = 1;
    if (pageSize < 1 || pageSize > 100) pageSize = 20;

    // Query
    var products = await context.Products
        .AsNoTracking()
        .ToPaginatedListAsync(page, pageSize);

    // Map to DTO
    var response = new PaginatedResponse<ProductDto>
    {
        Items = products.Select(p => new ProductDto(p)).ToList(),
        PageIndex = products.PageIndex,
        TotalPages = products.TotalPages,
        TotalCount = products.ResultCount,
        HasPreviousPage = products.HasPreviousPage,
        HasNextPage = products.HasNextPage
    };

    return Ok(response);
}
```

### Pattern 2: Infinite Scroll (No Count)

```csharp
[HttpGet("infinite")]
public async Task<ActionResult<List<ProductDto>>> GetProductsInfiniteScroll(
    [FromQuery] int page = 1,
    [FromQuery] int pageSize = 20)
{
    var products = await PaginatedList<Product>.CreateAsync(
        source: context.Products.AsNoTracking(),
        pageIndex: page,
        pageSize: pageSize,
        count: false);  // Skip expensive count query

    var dtos = products.Select(p => new ProductDto(p)).ToList();

    // Client checks if result.Count < pageSize to know if last page
    return Ok(dtos);
}
```

### Pattern 3: Blazor Component Pagination

```razor
@page "/products"
@inject IDbContextFactory<AppDbContext> ContextFactory

<MudTable Items="@currentPage" Loading="@loading">
    <HeaderContent>
        <MudTh>Name</MudTh>
        <MudTh>Price</MudTh>
    </HeaderContent>
    <RowTemplate>
        <MudTd>@context.Name</MudTd>
        <MudTd>@context.Price.ToString("C")</MudTd>
    </RowTemplate>
    <PagerContent>
        <MudPagination
            Count="@currentPage?.TotalPages ?? 1"
            Selected="@currentPage?.PageIndex ?? 1"
            SelectedChanged="@OnPageChanged" />
    </PagerContent>
</MudTable>

@code {
    private PaginatedList<Product>? currentPage;
    private bool loading = true;

    protected override async Task OnInitializedAsync()
    {
        await LoadPageAsync(1);
    }

    private async Task OnPageChanged(int newPage)
    {
        await LoadPageAsync(newPage);
    }

    private async Task LoadPageAsync(int page)
    {
        loading = true;

        using var context = ContextFactory.CreateDbContext();
        currentPage = await context.Products
            .AsNoTracking()
            .ToPaginatedListAsync(page, 20);

        loading = false;
    }
}
```

### Pattern 4: Search with Pagination

```csharp
public async Task<PaginatedList<Product>> SearchProductsAsync(
    string searchTerm,
    int page,
    int pageSize,
    CancellationToken cancellationToken)
{
    var query = context.Products.AsNoTracking();

    if (!string.IsNullOrWhiteSpace(searchTerm))
    {
        query = query.Where(p =>
            p.Name.Contains(searchTerm) ||
            p.Description.Contains(searchTerm));
    }

    return await query
        .OrderBy(p => p.Name)
        .ToPaginatedListAsync(page, pageSize, cancellationToken);
}
```

### Pattern 5: Navigation Helper

```csharp
public class PaginationNavigator<T>
{
    private PaginatedList<T> _currentPage;

    public PaginationNavigator(PaginatedList<T> initialPage)
    {
        _currentPage = initialPage;
    }

    public async Task<PaginatedList<T>> NextPageAsync(IQueryable<T> source)
    {
        if (!_currentPage.HasNextPage)
            throw new InvalidOperationException("No next page available");

        _currentPage = await source.ToPaginatedListAsync(
            _currentPage.PageIndex + 1,
            _currentPage.Count);

        return _currentPage;
    }

    public async Task<PaginatedList<T>> PreviousPageAsync(IQueryable<T> source)
    {
        if (!_currentPage.HasPreviousPage)
            throw new InvalidOperationException("No previous page available");

        _currentPage = await source.ToPaginatedListAsync(
            _currentPage.PageIndex - 1,
            _currentPage.Count);

        return _currentPage;
    }

    public async Task<PaginatedList<T>> FirstPageAsync(IQueryable<T> source)
    {
        _currentPage = await source.ToPaginatedListAsync(1, _currentPage.Count);
        return _currentPage;
    }

    public async Task<PaginatedList<T>> LastPageAsync(IQueryable<T> source)
    {
        _currentPage = await source.ToPaginatedListAsync(
            _currentPage.TotalPages,
            _currentPage.Count);

        return _currentPage;
    }
}
```

### Pattern 6: Custom Page Response DTO

```csharp
public class PagedResponse<T>
{
    public List<T> Data { get; set; }
    public PaginationMetadata Pagination { get; set; }

    public static PagedResponse<T> FromPaginatedList(PaginatedList<T> page)
    {
        return new PagedResponse<T>
        {
            Data = page.ToList(),
            Pagination = new PaginationMetadata
            {
                CurrentPage = page.PageIndex,
                TotalPages = page.TotalPages,
                PageSize = page.Count,
                TotalCount = page.ResultCount,
                HasPrevious = page.HasPreviousPage,
                HasNext = page.HasNextPage
            }
        };
    }
}

public class PaginationMetadata
{
    public int CurrentPage { get; set; }
    public int TotalPages { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public bool HasPrevious { get; set; }
    public bool HasNext { get; set; }
}

// Usage
var page = await query.ToPaginatedListAsync(1, 20);
var response = PagedResponse<ProductDto>.FromPaginatedList(page);
return Ok(response);
```

## Best Practices

### 1. Always Use AsNoTracking for Read-Only Queries

```csharp
// BAD - loads change tracking overhead
var page = await context.Products.ToPaginatedListAsync(1, 20);

// GOOD - no tracking, better performance
var page = await context.Products
    .AsNoTracking()
    .ToPaginatedListAsync(1, 20);
```

### 2. Validate Page Parameters

```csharp
public async Task<PaginatedList<Product>> GetProductPageAsync(int page, int pageSize)
{
    // Prevent invalid pages
    if (page < 1) page = 1;

    // Prevent abuse
    if (pageSize < 1) pageSize = 10;
    if (pageSize > 100) pageSize = 100;

    return await context.Products
        .AsNoTracking()
        .ToPaginatedListAsync(page, pageSize);
}
```

### 3. Use Consistent Page Size

```csharp
// Define in configuration
public class PaginationSettings
{
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 100;
}

// Use throughout application
var page = await query.ToPaginatedListAsync(
    pageIndex,
    PaginationSettings.DefaultPageSize);
```

### 4. Cache Count for Static Data

```csharp
// For data that rarely changes, cache the count
private static int? cachedProductCount;
private static DateTime? cacheTime;

public async Task<PaginatedList<Product>> GetProductsAsync(int page)
{
    var query = context.Products.AsNoTracking();

    // Refresh cache every 5 minutes
    if (cachedProductCount == null || cacheTime < DateTime.UtcNow.AddMinutes(-5))
    {
        cachedProductCount = await query.CountAsync();
        cacheTime = DateTime.UtcNow;
    }

    // Use cached count instead of re-querying
    var items = await query
        .Skip((page - 1) * 20)
        .Take(20)
        .ToListAsync();

    return new PaginatedList<Product>(items, cachedProductCount.Value, page, 20);
}
```

### 5. Skip Count for Large Tables

```csharp
// For tables with millions of rows, COUNT(*) is expensive
// Use count: false for better performance
var page = await PaginatedList<Product>.CreateAsync(
    source: context.Products.AsNoTracking(),
    pageIndex: page,
    pageSize: 50,
    count: false);

// Implement "Load More" UI instead of page numbers
```

### 6. Apply Ordering Before Pagination

```csharp
// BAD - undefined order, inconsistent paging
var page = await context.Products
    .AsNoTracking()
    .ToPaginatedListAsync(1, 20);

// GOOD - consistent ordering
var page = await context.Products
    .AsNoTracking()
    .OrderBy(p => p.Name)
    .ToPaginatedListAsync(1, 20);
```

### 7. Use CancellationToken for Long Queries

```csharp
[HttpGet]
public async Task<ActionResult<PaginatedList<Product>>> GetProducts(
    int page,
    CancellationToken cancellationToken)
{
    var products = await context.Products
        .AsNoTracking()
        .ToPaginatedListAsync(page, 20, cancellationToken);

    return Ok(products);
}
```

### 8. Include Navigation Properties Efficiently

```csharp
// BAD - causes cartesian explosion with pagination
var page = await context.Products
    .Include(p => p.Categories)
    .Include(p => p.Reviews)
    .AsNoTracking()
    .ToPaginatedListAsync(1, 20);

// GOOD - split queries or use select
var page = await context.Products
    .AsNoTracking()
    .AsSplitQuery()
    .Include(p => p.Category)
    .ToPaginatedListAsync(1, 20);

// BETTER - project to DTO
var page = await context.Products
    .AsNoTracking()
    .Select(p => new ProductDto
    {
        Id = p.Id,
        Name = p.Name,
        CategoryName = p.Category.Name
    })
    .ToPaginatedListAsync(1, 20);
```

## Performance Considerations

### Query Execution

PaginatedList executes **two database queries** by default:

1. **Data Query:** `Skip().Take()` to get page items
2. **Count Query:** `Count()` to get total count

**Example SQL:**
```sql
-- Query 1: Get page data
SELECT * FROM Products
ORDER BY Name
OFFSET 20 ROWS FETCH NEXT 20 ROWS ONLY;

-- Query 2: Get total count
SELECT COUNT(*) FROM Products;
```

### Count Query Optimization

The count query can be expensive on large tables:

- **Small tables (< 10,000 rows):** Count is fast
- **Medium tables (10,000 - 100,000 rows):** Count is acceptable
- **Large tables (100,000+ rows):** Count can be slow

**Solutions:**

1. **Disable count for large datasets:**
   ```csharp
   var page = await PaginatedList<Product>.CreateAsync(query, page, pageSize, count: false);
   ```

2. **Use approximate count:**
   ```csharp
   // SQL Server specific
   var approxCount = await context.Database
       .SqlQueryRaw<int>("SELECT rows FROM sys.partitions WHERE object_id = OBJECT_ID('Products')")
       .FirstOrDefaultAsync();
   ```

3. **Cache count:**
   ```csharp
   var count = await cache.GetOrAddAsync("products_count", async () =>
       await context.Products.CountAsync(),
       TimeSpan.FromMinutes(5));
   ```

### Index Requirements

Ensure proper indexes for:

- Ordering columns: `CREATE INDEX IX_Products_Name ON Products(Name)`
- Filter columns: `CREATE INDEX IX_Products_IsActive ON Products(IsActive)`
- Composite: `CREATE INDEX IX_Products_Category_Price ON Products(CategoryId, Price)`

### Memory Usage

- Page size 10-20: Minimal memory usage
- Page size 50-100: Moderate memory usage
- Page size 500+: High memory usage, avoid

## Related Documentation

- [Repository](Repository.md) - BaseRepo pattern with built-in pagination
- [ContextExtensions](ContextExtensions.md) - ToPaginatedListAsync extension method
- [CheapContext](CheapContext.md) - Context configuration
