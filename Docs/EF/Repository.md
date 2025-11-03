# BaseRepo - Repository Pattern

The `BaseRepo` class provides a generic repository implementation for Entity Framework Core with built-in support for CRUD operations, pagination, and common query patterns. It uses `IDbContextFactory<TContext>` for proper context lifecycle management.

## Overview

BaseRepo eliminates repetitive data access code by providing a comprehensive set of generic methods for common database operations. It follows the repository pattern and implements `IDisposable` for proper resource cleanup.

### Key Features

- Generic CRUD operations for any entity type
- Built-in pagination support
- No-tracking queries for read operations (better performance)
- CancellationToken support for all async operations
- Automatic error logging with Debug.WriteLine
- Code-based entity lookups (IEntityCode)
- Bulk operations (AddRange, UpdateRange, DeleteRange)
- Predicate-based queries and deletions
- Context factory pattern for thread safety

## Installation

```bash
dotnet add package CheapHelpers.EF
```

## Basic Setup

### Entity Requirements

Entities must implement one of these interfaces:

```csharp
using CheapHelpers.Models.Contracts;

// Basic entity with ID
public class Product : IEntityId
{
    public int Id { get; set; }
    public string Name { get; set; }
    public decimal Price { get; set; }
}

// Entity with code-based lookup
public class Category : IEntityCode
{
    public int Id { get; set; }
    public string Code { get; set; }  // Required for IEntityCode
    public string Name { get; set; }
}

// Auditable entity with automatic timestamps
public class Order : IEntityId, IAuditable
{
    public int Id { get; set; }
    public string OrderNumber { get; set; }
    public DateTime CreatedAt { get; set; }  // Auto-populated by CheapContext
    public DateTime UpdatedAt { get; set; }  // Auto-updated by CheapContext
}
```

### Repository Registration

```csharp
using CheapHelpers.EF;
using CheapHelpers.EF.Repositories;
using Microsoft.EntityFrameworkCore;

// In Program.cs or Startup.cs
builder.Services.AddDbContextFactory<CheapContext<CheapUser>>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddScoped<BaseRepo>();
```

### Basic Usage

```csharp
using CheapHelpers.EF.Repositories;

public class ProductService
{
    private readonly BaseRepo _repo;

    public ProductService(BaseRepo repo)
    {
        _repo = repo;
    }

    public async Task<Product?> GetProductAsync(int id)
    {
        return await _repo.GetByIdAsync<Product>(id);
    }
}
```

## Read Operations

### GetAllAsync

Retrieves all entities of a type without tracking.

```csharp
// Get all products
var products = await repo.GetAllAsync<Product>();

// With cancellation token
var products = await repo.GetAllAsync<Product>(cancellationToken);
```

**Signature:**
```csharp
Task<List<T>> GetAllAsync<T>(CancellationToken token = default) where T : class, IEntityId
```

### GetAllPaginatedAsync

Retrieves entities with pagination support.

```csharp
// Get first page (10 items)
var page1 = await repo.GetAllPaginatedAsync<Product>();

// Get page 2 with 20 items per page
var page2 = await repo.GetAllPaginatedAsync<Product>(pageIndex: 2, pageSize: 20);

// Access pagination metadata
Console.WriteLine($"Page {page2.PageIndex} of {page2.TotalPages}");
Console.WriteLine($"Total items: {page2.ResultCount}");
Console.WriteLine($"Has next page: {page2.HasNextPage}");

// Iterate over items
foreach (var product in page2)
{
    Console.WriteLine(product.Name);
}
```

**Signature:**
```csharp
Task<PaginatedList<T>> GetAllPaginatedAsync<T>(
    int? pageIndex = null,
    int pageSize = 10,
    CancellationToken token = default)
    where T : class, IEntityId
```

**Default values:**
- `pageIndex`: 1 (first page)
- `pageSize`: 10 items

### GetByIdAsync

Retrieves a single entity by its ID.

```csharp
// Get product by ID
var product = await repo.GetByIdAsync<Product>(123);

if (product != null)
{
    Console.WriteLine(product.Name);
}
```

**Signature:**
```csharp
Task<T?> GetByIdAsync<T>(int id, CancellationToken token = default)
    where T : class, IEntityId
```

### GetByCodeAsync

Retrieves a single entity by its code (for entities implementing `IEntityCode`).

```csharp
// Get category by code
var category = await repo.GetByCodeAsync<Category>("electronics");

if (category != null)
{
    Console.WriteLine(category.Name);
}
```

**Signature:**
```csharp
Task<T?> GetByCodeAsync<T>(string code, CancellationToken token = default)
    where T : class, IEntityCode
```

### GetWhereAsync

Retrieves entities based on a predicate condition.

```csharp
// Get products with price > 100
var expensiveProducts = await repo.GetWhereAsync<Product>(
    p => p.Price > 100);

// Complex predicates
var activeProducts = await repo.GetWhereAsync<Product>(
    p => p.IsActive && p.Stock > 0 && p.CategoryId == 5);

// Using variables in predicates
var minPrice = 50m;
var category = "Electronics";
var products = await repo.GetWhereAsync<Product>(
    p => p.Price >= minPrice && p.Category == category);
```

**Signature:**
```csharp
Task<List<T>> GetWhereAsync<T>(
    Expression<Func<T, bool>> predicate,
    CancellationToken token = default)
    where T : class, IEntityId
```

### GetWherePaginatedAsync

Retrieves paginated entities based on a predicate condition.

```csharp
// Get paginated active products
var activePage = await repo.GetWherePaginatedAsync<Product>(
    p => p.IsActive,
    pageIndex: 1,
    pageSize: 20);

// With complex conditions
var results = await repo.GetWherePaginatedAsync<Product>(
    p => p.Price > 100 && p.Stock > 0,
    pageIndex: 2,
    pageSize: 50);
```

**Signature:**
```csharp
Task<PaginatedList<T>> GetWherePaginatedAsync<T>(
    Expression<Func<T, bool>> predicate,
    int? pageIndex = null,
    int pageSize = 10,
    CancellationToken token = default)
    where T : class, IEntityId
```

### ExistsAsync

Checks if an entity exists by ID.

```csharp
// Check if product exists
var exists = await repo.ExistsAsync<Product>(123);

if (exists)
{
    Console.WriteLine("Product found!");
}
```

**Signature:**
```csharp
Task<bool> ExistsAsync<T>(int id, CancellationToken token = default)
    where T : class, IEntityId
```

### ExistsByCodeAsync

Checks if an entity exists by code.

```csharp
// Check if category code is taken
var exists = await repo.ExistsByCodeAsync<Category>("electronics");

if (exists)
{
    Console.WriteLine("Category code already exists!");
}
```

**Signature:**
```csharp
Task<bool> ExistsByCodeAsync<T>(string code, CancellationToken token = default)
    where T : class, IEntityCode
```

## Create Operations

### AddAsync

Adds a new entity unconditionally.

```csharp
var newProduct = new Product
{
    Name = "Gaming Laptop",
    Price = 1299.99m
};

var added = await repo.AddAsync(newProduct);
Console.WriteLine($"Created product with ID: {added.Id}");
```

**Signature:**
```csharp
Task<T> AddAsync<T>(T entity, CancellationToken token = default)
    where T : class, IEntityId
```

### AddIfNullAsync

Adds a new entity only if the existing entity is null, otherwise returns the existing entity.

```csharp
// Check if product exists first
var existing = await repo.GetByIdAsync<Product>(123);

// Add only if not found
var product = await repo.AddIfNullAsync(existing, new Product
{
    Name = "New Product",
    Price = 99.99m
});

// If existing was null, new product is added
// If existing was not null, existing product is returned
```

**Signature:**
```csharp
Task<T> AddIfNullAsync<T>(
    T? existingEntity,
    T newEntity,
    CancellationToken token = default)
    where T : class, IEntityId
```

### AddIfNotExistsAsync

Adds a new entity only if no entity with the same code exists.

```csharp
var category = new Category
{
    Code = "electronics",
    Name = "Electronics"
};

// Adds if code doesn't exist, returns existing if it does
var result = await repo.AddIfNotExistsAsync(category);

if (result.Id == category.Id)
{
    Console.WriteLine("Category was created");
}
else
{
    Console.WriteLine("Category already existed");
}
```

**Signature:**
```csharp
Task<T> AddIfNotExistsAsync<T>(T entity, CancellationToken token = default)
    where T : class, IEntityCode
```

**Note:** Logs a warning if adding an entity with null/empty code.

### AddRangeAsync

Adds multiple entities at once.

```csharp
var products = new List<Product>
{
    new() { Name = "Product 1", Price = 10.99m },
    new() { Name = "Product 2", Price = 20.99m },
    new() { Name = "Product 3", Price = 30.99m }
};

var added = await repo.AddRangeAsync(products);
Console.WriteLine($"Added {added.Count} products");
```

**Signature:**
```csharp
Task<List<T>> AddRangeAsync<T>(
    IEnumerable<T> entities,
    CancellationToken token = default)
    where T : class, IEntityId
```

**Note:** Returns empty list if input collection is empty.

## Update Operations

### UpdateAsync

Updates an existing entity.

```csharp
var product = await repo.GetByIdAsync<Product>(123);
if (product != null)
{
    product.Price = 149.99m;
    product.Name = "Updated Product";

    await repo.UpdateAsync(product);
}
```

**Signature:**
```csharp
Task<T> UpdateAsync<T>(T entity, CancellationToken token = default)
    where T : class, IEntityId
```

### UpdateRangeAsync

Updates multiple entities at once.

```csharp
var products = await repo.GetWhereAsync<Product>(p => p.CategoryId == 5);

foreach (var product in products)
{
    product.Price *= 1.1m; // Increase price by 10%
}

await repo.UpdateRangeAsync(products);
```

**Signature:**
```csharp
Task<List<T>> UpdateRangeAsync<T>(
    IEnumerable<T> entities,
    CancellationToken token = default)
    where T : class, IEntityId
```

## Delete Operations

### DeleteAsync (by ID)

Deletes an entity by its ID.

```csharp
var deleted = await repo.DeleteAsync<Product>(123);

if (deleted)
{
    Console.WriteLine("Product deleted successfully");
}
else
{
    Console.WriteLine("Product not found");
}
```

**Signature:**
```csharp
Task<bool> DeleteAsync<T>(int id, CancellationToken token = default)
    where T : class, IEntityId
```

**Returns:** `true` if entity was found and deleted, `false` if not found.

### DeleteAsync (by Entity)

Deletes an entity directly.

```csharp
var product = await repo.GetByIdAsync<Product>(123);
if (product != null)
{
    await repo.DeleteAsync(product);
}
```

**Signature:**
```csharp
Task<bool> DeleteAsync<T>(T entity, CancellationToken token = default)
    where T : class, IEntityId
```

### DeleteRangeAsync

Deletes multiple entities at once.

```csharp
var outdatedProducts = await repo.GetWhereAsync<Product>(
    p => p.LastUpdated < DateTime.UtcNow.AddYears(-2));

var deletedCount = await repo.DeleteRangeAsync(outdatedProducts);
Console.WriteLine($"Deleted {deletedCount} outdated products");
```

**Signature:**
```csharp
Task<int> DeleteRangeAsync<T>(
    IEnumerable<T> entities,
    CancellationToken token = default)
    where T : class, IEntityId
```

**Returns:** Number of entities deleted (same as count of input collection).

### DeleteWhereAsync

Deletes entities based on a predicate condition.

```csharp
// Delete all inactive products
var deletedCount = await repo.DeleteWhereAsync<Product>(
    p => !p.IsActive);

Console.WriteLine($"Deleted {deletedCount} inactive products");

// Delete with complex conditions
var count = await repo.DeleteWhereAsync<Product>(
    p => p.Stock == 0 && p.LastUpdated < DateTime.UtcNow.AddMonths(-6));
```

**Signature:**
```csharp
Task<int> DeleteWhereAsync<T>(
    Expression<Func<T, bool>> predicate,
    CancellationToken token = default)
    where T : class, IEntityId
```

**Returns:** Number of entities deleted.

## Utility Methods

### CountAsync

Gets the total count of entities.

```csharp
var totalProducts = await repo.CountAsync<Product>();
Console.WriteLine($"Total products: {totalProducts}");
```

**Signature:**
```csharp
Task<int> CountAsync<T>(CancellationToken token = default)
    where T : class, IEntityId
```

### CountWhereAsync

Gets the count of entities matching a predicate.

```csharp
// Count active products
var activeCount = await repo.CountWhereAsync<Product>(
    p => p.IsActive);

// Count with complex conditions
var count = await repo.CountWhereAsync<Product>(
    p => p.Price > 100 && p.Stock > 0 && p.CategoryId == 5);
```

**Signature:**
```csharp
Task<int> CountWhereAsync<T>(
    Expression<Func<T, bool>> predicate,
    CancellationToken token = default)
    where T : class, IEntityId
```

## Static Helper Methods

### AddIfNotExistsAsync (Static)

Static helper for adding entities within an existing context transaction.

```csharp
using var context = contextFactory.CreateDbContext();
using var transaction = context.Database.BeginTransaction();

try
{
    var category = new Category { Code = "electronics", Name = "Electronics" };
    var result = await BaseRepo.AddIfNotExistsAsync(context, category);

    // Continue with more operations...

    await transaction.CommitAsync();
}
catch
{
    await transaction.RollbackAsync();
    throw;
}
```

**Signature:**
```csharp
static Task<T> AddIfNotExistsAsync<T>(
    DbContext context,
    T entity,
    CancellationToken token = default)
    where T : class, IEntityCode
```

**Use Case:** When you need to add entities as part of a larger transaction with multiple operations.

## Best Practices

### 1. Use AsNoTracking for Read Operations

All read operations in BaseRepo automatically use `AsNoTracking()` for better performance:

```csharp
// No need to manually specify AsNoTracking
var products = await repo.GetAllAsync<Product>();
```

### 2. Leverage Pagination for Large Datasets

```csharp
// Don't load everything at once
// BAD:
var allProducts = await repo.GetAllAsync<Product>();

// GOOD:
var productsPage = await repo.GetAllPaginatedAsync<Product>(pageIndex: 1, pageSize: 50);
```

### 3. Use Predicate Methods for Filtering

```csharp
// More efficient than filtering in memory
// BAD:
var all = await repo.GetAllAsync<Product>();
var filtered = all.Where(p => p.Price > 100).ToList();

// GOOD:
var filtered = await repo.GetWhereAsync<Product>(p => p.Price > 100);
```

### 4. Always Use CancellationToken for Long Operations

```csharp
public async Task<List<Product>> GetProductsAsync(CancellationToken cancellationToken)
{
    return await repo.GetAllAsync<Product>(cancellationToken);
}
```

### 5. Check Existence Before Creating

```csharp
// Use AddIfNotExistsAsync for code-based entities
var category = await repo.AddIfNotExistsAsync(new Category
{
    Code = "electronics",
    Name = "Electronics"
});

// Or check manually for ID-based entities
var exists = await repo.ExistsAsync<Product>(productId);
if (!exists)
{
    await repo.AddAsync(newProduct);
}
```

### 6. Use Bulk Operations for Multiple Items

```csharp
// More efficient than individual calls
// BAD:
foreach (var product in products)
{
    await repo.AddAsync(product);
}

// GOOD:
await repo.AddRangeAsync(products);
```

### 7. Dispose of Repository Properly

```csharp
// In dependency injection (automatic disposal)
public class ProductService
{
    private readonly BaseRepo _repo;

    public ProductService(BaseRepo repo)
    {
        _repo = repo; // DI container handles disposal
    }
}

// Manual usage (rare)
using var repo = new BaseRepo(contextFactory);
var products = await repo.GetAllAsync<Product>();
```

### 8. Use IEntityCode for Natural Keys

```csharp
// When entities have natural unique identifiers
public class Country : IEntityCode
{
    public int Id { get; set; }
    public string Code { get; set; }  // "US", "GB", "DE", etc.
    public string Name { get; set; }
}

// Easier lookup and duplicate prevention
var usa = await repo.GetByCodeAsync<Country>("US");
var country = await repo.AddIfNotExistsAsync(new Country { Code = "FR", Name = "France" });
```

## Common Patterns

### Pattern 1: Get or Create

```csharp
public async Task<Category> GetOrCreateCategoryAsync(string code, string name)
{
    var category = await repo.GetByCodeAsync<Category>(code);

    if (category == null)
    {
        category = await repo.AddAsync(new Category { Code = code, Name = name });
    }

    return category;
}

// Or simpler with AddIfNotExistsAsync:
public async Task<Category> GetOrCreateCategoryAsync(string code, string name)
{
    return await repo.AddIfNotExistsAsync(new Category { Code = code, Name = name });
}
```

### Pattern 2: Update if Exists

```csharp
public async Task<bool> UpdateProductPriceAsync(int productId, decimal newPrice)
{
    var product = await repo.GetByIdAsync<Product>(productId);

    if (product == null)
        return false;

    product.Price = newPrice;
    await repo.UpdateAsync(product);
    return true;
}
```

### Pattern 3: Conditional Delete

```csharp
public async Task<int> CleanupOldOrdersAsync(int daysOld)
{
    var cutoffDate = DateTime.UtcNow.AddDays(-daysOld);
    return await repo.DeleteWhereAsync<Order>(
        o => o.CreatedAt < cutoffDate && o.Status == OrderStatus.Completed);
}
```

### Pattern 4: Paginated Search

```csharp
public async Task<PaginatedList<Product>> SearchProductsAsync(
    string searchTerm,
    int page,
    int pageSize)
{
    return await repo.GetWherePaginatedAsync<Product>(
        p => p.Name.Contains(searchTerm) || p.Description.Contains(searchTerm),
        pageIndex: page,
        pageSize: pageSize);
}
```

## Error Handling

All methods include try-catch blocks with Debug.WriteLine logging:

```csharp
try
{
    var product = await repo.GetByIdAsync<Product>(123);
}
catch (Exception ex)
{
    // Error is logged to Debug output:
    // "Error in GetByIdAsync<Product> with ID 123: [error message]"
    throw;
}
```

Best practice is to handle exceptions at the service layer:

```csharp
public class ProductService
{
    private readonly BaseRepo _repo;
    private readonly ILogger<ProductService> _logger;

    public async Task<Product?> GetProductSafeAsync(int id)
    {
        try
        {
            return await _repo.GetByIdAsync<Product>(id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get product {ProductId}", id);
            return null;
        }
    }
}
```

## Related Documentation

- [PaginatedList](PaginatedList.md) - Pagination implementation details
- [ContextExtensions](ContextExtensions.md) - Additional DbContext utilities
- [CheapContext](CheapContext.md) - Context configuration and auditing
