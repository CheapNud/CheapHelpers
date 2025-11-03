# CollectionExtensions

Collection manipulation and conversion extension methods with dynamic querying support.

## Overview

The `CollectionExtensions` class provides extension methods for working with collections, including item replacement, type conversions, null checking, and dynamic LINQ ordering capabilities.

## Namespace

```csharp
using CheapHelpers.Extensions;
```

## Methods

### Replace (IList)

Searches for the index of an old item and replaces it with a new item.

**Signature:**
```csharp
public static T Replace<T>(this IList<T> list, T oldItem, T newItem)
```

**Parameters:**
- `list`: The list to modify
- `oldItem`: The item to replace
- `newItem`: The replacement item

**Returns:** The new item that was inserted

**Example:**
```csharp
var numbers = new List<int> { 1, 2, 3, 4, 5 };
numbers.Replace(3, 10);
// Result: { 1, 2, 10, 4, 5 }
```

### Replace (List with Predicate)

Replaces an item in a list based on a predicate selector.

**Signature:**
```csharp
public static void Replace<T>(this List<T> list, Predicate<T> oldItemSelector, T newItem)
```

**Parameters:**
- `list`: The list to modify
- `oldItemSelector`: Predicate to find the item to replace
- `newItem`: The replacement item

**Example:**
```csharp
var users = new List<User>
{
    new User { Id = 1, Name = "John" },
    new User { Id = 2, Name = "Jane" },
    new User { Id = 3, Name = "Bob" }
};

users.Replace(u => u.Id == 2, new User { Id = 2, Name = "Janet" });
// Jane is replaced with Janet
```

### ToBindingList

Converts an IList to a BindingList for data binding scenarios.

**Signature:**
```csharp
public static BindingList<T> ToBindingList<T>(this IList<T> source)
```

**Parameters:**
- `source`: The source list

**Returns:** BindingList containing all items from the source

**Example:**
```csharp
var items = new List<string> { "A", "B", "C" };
var bindingList = items.ToBindingList();
// Use bindingList with Windows Forms data binding
```

### ToObservableCollection

Converts an IEnumerable to an ObservableCollection for WPF/MAUI data binding.

**Signature:**
```csharp
public static ObservableCollection<T> ToObservableCollection<T>(this IEnumerable<T> source)
```

**Parameters:**
- `source`: The source enumerable

**Returns:** ObservableCollection containing all items from the source

**Example:**
```csharp
var items = GetItems().Where(x => x.IsActive);
var observableItems = items.ToObservableCollection();
// Bind to WPF/MAUI UI controls
```

### IsNullOrEmpty

Checks if an enumerable is null or contains no elements.

**Signature:**
```csharp
public static bool IsNullOrEmpty<T>(this IEnumerable<T> enumerable)
```

**Parameters:**
- `enumerable`: The enumerable to check

**Returns:** `true` if the enumerable is null or empty; otherwise `false`

**Example:**
```csharp
List<int> numbers = null;
numbers.IsNullOrEmpty();  // true

numbers = new List<int>();
numbers.IsNullOrEmpty();  // true

numbers.Add(1);
numbers.IsNullOrEmpty();  // false
```

### OrderByDynamic

Dynamically orders a queryable collection by a property name in ascending order using reflection.

**Signature:**
```csharp
public static IQueryable<T> OrderByDynamic<T>(this IQueryable<T> query, string orderByMember)
```

**Parameters:**
- `query`: The queryable collection to order
- `orderByMember`: The property name to order by

**Returns:** An ordered queryable collection in ascending order

**Example:**
```csharp
var users = dbContext.Users.AsQueryable();

// Dynamic ordering based on runtime property name
string sortColumn = GetUserSelectedColumn(); // e.g., "LastName"
var sortedUsers = users.OrderByDynamic(sortColumn);

// Equivalent to: users.OrderBy(u => u.LastName)
```

### OrderByDescendingDynamic

Dynamically orders a queryable collection by a property name in descending order using reflection.

**Signature:**
```csharp
public static IQueryable<T> OrderByDescendingDynamic<T>(this IQueryable<T> query, string orderByMember)
```

**Parameters:**
- `query`: The queryable collection to order
- `orderByMember`: The property name to order by

**Returns:** An ordered queryable collection in descending order

**Example:**
```csharp
var products = dbContext.Products.AsQueryable();

// Dynamic descending sort
string sortColumn = "Price";
var sortedProducts = products.OrderByDescendingDynamic(sortColumn);

// Equivalent to: products.OrderByDescending(p => p.Price)
```

## Common Use Cases

### Dynamic Sorting in UI Grids

```csharp
public IQueryable<Product> GetSortedProducts(string sortColumn, bool ascending)
{
    var query = dbContext.Products.AsQueryable();

    return ascending
        ? query.OrderByDynamic(sortColumn)
        : query.OrderByDescendingDynamic(sortColumn);
}

// Usage from UI event handler
var products = GetSortedProducts("ProductName", true);
```

### WPF/MAUI Data Binding

```csharp
public class ViewModel
{
    private ObservableCollection<User> _users;

    public ObservableCollection<User> Users
    {
        get => _users;
        set => SetProperty(ref _users, value);
    }

    public async Task LoadUsers()
    {
        var userList = await userService.GetUsersAsync();
        Users = userList.ToObservableCollection();
    }
}
```

### Safe Collection Operations

```csharp
public void ProcessItems(IEnumerable<Item> items)
{
    // Guard against null or empty collections
    if (items.IsNullOrEmpty())
    {
        logger.LogWarning("No items to process");
        return;
    }

    foreach (var item in items)
    {
        ProcessItem(item);
    }
}
```

### Item Replacement in Collections

```csharp
// Replace specific item
var inventory = GetInventoryItems();
var outdatedItem = inventory.First(i => i.Id == 123);
var updatedItem = await FetchLatestVersion(outdatedItem.Id);
inventory.Replace(outdatedItem, updatedItem);

// Replace using predicate
var cart = GetShoppingCart();
cart.Replace(
    item => item.ProductId == productId,
    new CartItem { ProductId = productId, Quantity = newQuantity }
);
```

### Generic Sorting API

```csharp
public class DataTableController : ControllerBase
{
    [HttpGet]
    public IActionResult GetData(
        string sortBy = "Id",
        string sortDirection = "asc")
    {
        var query = repository.GetAll();

        var sorted = sortDirection.ToLower() == "desc"
            ? query.OrderByDescendingDynamic(sortBy)
            : query.OrderByDynamic(sortBy);

        return Ok(sorted.ToList());
    }
}
```

### Windows Forms Data Binding

```csharp
public class ProductForm : Form
{
    private BindingList<Product> _products;

    private void LoadProducts()
    {
        var productList = productRepository.GetAll().ToList();
        _products = productList.ToBindingList();

        // Bind to DataGridView
        productDataGridView.DataSource = _products;

        // Changes to _products automatically update the grid
    }
}
```

## Tips and Best Practices

1. **Dynamic Ordering Performance**: `OrderByDynamic` and `OrderByDescendingDynamic` use reflection and expression trees. While performant enough for most scenarios, avoid using them in tight loops. Cache the sorting logic if possible.

2. **Property Name Validation**: When using dynamic ordering, validate property names against a whitelist to prevent errors:
   ```csharp
   var allowedColumns = new[] { "Name", "Date", "Price" };
   if (!allowedColumns.Contains(sortColumn))
       throw new ArgumentException($"Invalid sort column: {sortColumn}");

   var sorted = query.OrderByDynamic(sortColumn);
   ```

3. **Null Safety with IsNullOrEmpty**: This method is more convenient than checking `enumerable == null || !enumerable.Any()` and is especially useful in guard clauses.

4. **ObservableCollection vs BindingList**:
   - Use `ToObservableCollection` for WPF, MAUI, Xamarin
   - Use `ToBindingList` for Windows Forms
   - Both provide automatic UI updates on collection changes

5. **Replace Operations**: The predicate-based `Replace` method only replaces the first match. For multiple replacements, iterate and replace individually or use a different approach.

6. **IQueryable vs IEnumerable**: Dynamic ordering methods require `IQueryable<T>`. If you have an `IEnumerable<T>`, convert it first:
   ```csharp
   var list = GetItems().AsQueryable();
   var sorted = list.OrderByDynamic("PropertyName");
   ```

7. **Database Compatibility**: Dynamic ordering generates expression trees that translate to SQL. Ensure the property names match database column names for Entity Framework scenarios.

8. **Type Safety**: While dynamic ordering is convenient, consider using strongly-typed alternatives when the sort columns are known at compile time for better type safety and IntelliSense support.
