# XML Services

Dynamic and strongly-typed XML serialization and deserialization services.

## Table of Contents

- [Overview](#overview)
- [Available Services](#available-services)
- [Usage Examples](#usage-examples)
- [Dependency Injection Setup](#dependency-injection-setup)
- [Common Scenarios](#common-scenarios)

## Overview

The CheapHelpers.Services XML package provides flexible XML serialization with two approaches:

1. **Dynamic Serialization** - Convert any object/collection to XML dynamically
2. **Strongly-Typed Serialization** - Use standard XmlSerializer for type-safe operations

### Key Features

- Dynamic object to XML conversion
- Support for ExpandoObject and anonymous types
- Strongly-typed serialization/deserialization
- File and string operations
- Nested object support
- Collection handling
- Automatic element name sanitization

## Available Services

### IXmlService

```csharp
public interface IXmlService
{
    // Dynamic object serialization (original functionality)
    Task ExportDynamic(string filePath, dynamic data);

    // Strongly-typed serialization using XmlSerializer
    Task<T?> DeserializeAsync<T>(string filePath) where T : class;
    Task SerializeAsync<T>(string filePath, T data) where T : class;
    T? DeserializeFromString<T>(string xml) where T : class;
    string SerializeToString<T>(T data) where T : class;
}
```

## Usage Examples

### Dynamic XML Export

#### Export Single Object

```csharp
var xmlService = new XmlService();

var customer = new
{
    CustomerId = 123,
    Name = "John Doe",
    Email = "john@example.com",
    CreatedDate = DateTime.Now
};

await xmlService.ExportDynamic("customer.xml", customer);
```

**Output:**
```xml
<?xml version="1.0" encoding="utf-8" standalone="yes"?>
<Root>
  <Data>
    <CustomerId>123</CustomerId>
    <Name>John Doe</Name>
    <Email>john@example.com</Email>
    <CreatedDate>2024-01-15T10:30:00</CreatedDate>
  </Data>
</Root>
```

#### Export Collection

```csharp
var products = new[]
{
    new { ProductId = 1, Name = "Widget", Price = 19.99 },
    new { ProductId = 2, Name = "Gadget", Price = 29.99 },
    new { ProductId = 3, Name = "Doohickey", Price = 39.99 }
};

await xmlService.ExportDynamic("products.xml", products);
```

**Output:**
```xml
<?xml version="1.0" encoding="utf-8" standalone="yes"?>
<Root>
  <Items>
    <Item>
      <ProductId>1</ProductId>
      <Name>Widget</Name>
      <Price>19.99</Price>
    </Item>
    <Item>
      <ProductId>2</ProductId>
      <Name>Gadget</Name>
      <Price>29.99</Price>
    </Item>
    <Item>
      <ProductId>3</ProductId>
      <Name>Doohickey</Name>
      <Price>39.99</Price>
    </Item>
  </Items>
</Root>
```

#### Export Nested Objects

```csharp
var order = new
{
    OrderNumber = "ORD-001",
    OrderDate = DateTime.Now,
    Customer = new
    {
        Name = "Jane Smith",
        Email = "jane@example.com"
    },
    Items = new[]
    {
        new { Product = "Widget", Quantity = 2, Price = 19.99 },
        new { Product = "Gadget", Quantity = 1, Price = 29.99 }
    }
};

await xmlService.ExportDynamic("order.xml", order);
```

**Output:**
```xml
<?xml version="1.0" encoding="utf-8" standalone="yes"?>
<Root>
  <Data>
    <OrderNumber>ORD-001</OrderNumber>
    <OrderDate>2024-01-15T10:30:00</OrderDate>
    <Customer>
      <Name>Jane Smith</Name>
      <Email>jane@example.com</Email>
    </Customer>
    <Items>
      <Item>
        <Product>Widget</Product>
        <Quantity>2</Quantity>
        <Price>19.99</Price>
      </Item>
      <Item>
        <Product>Gadget</Product>
        <Quantity>1</Quantity>
        <Price>29.99</Price>
      </Item>
    </Items>
  </Data>
</Root>
```

#### Export ExpandoObject

```csharp
dynamic customer = new ExpandoObject();
customer.Id = 456;
customer.FirstName = "Alice";
customer.LastName = "Johnson";
customer.IsActive = true;

await xmlService.ExportDynamic("customer-dynamic.xml", customer);
```

### Strongly-Typed XML Operations

#### Serialize to File

```csharp
public class Customer
{
    public int CustomerId { get; set; }
    public string Name { get; set; }
    public string Email { get; set; }
    public DateTime CreatedDate { get; set; }
}

var customer = new Customer
{
    CustomerId = 789,
    Name = "Bob Smith",
    Email = "bob@example.com",
    CreatedDate = DateTime.Now
};

await xmlService.SerializeAsync("customer-typed.xml", customer);
```

#### Deserialize from File

```csharp
var customer = await xmlService.DeserializeAsync<Customer>("customer-typed.xml");

if (customer != null)
{
    Console.WriteLine($"Loaded: {customer.Name}");
}
```

#### Serialize to String

```csharp
public class Product
{
    public int ProductId { get; set; }
    public string Name { get; set; }
    public decimal Price { get; set; }
}

var product = new Product
{
    ProductId = 101,
    Name = "Super Widget",
    Price = 49.99m
};

string xmlString = xmlService.SerializeToString(product);
Console.WriteLine(xmlString);
```

**Output:**
```xml
<?xml version="1.0" encoding="utf-16"?>
<Product xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" xmlns:xsd="http://www.w3.org/2001/XMLSchema">
  <ProductId>101</ProductId>
  <Name>Super Widget</Name>
  <Price>49.99</Price>
</Product>
```

#### Deserialize from String

```csharp
string xmlString = @"
<Product>
  <ProductId>102</ProductId>
  <Name>Mega Gadget</Name>
  <Price>99.99</Price>
</Product>";

var product = xmlService.DeserializeFromString<Product>(xmlString);
Console.WriteLine($"{product.Name}: ${product.Price}");
```

### Using XML Attributes

```csharp
using System.Xml.Serialization;

public class Book
{
    [XmlAttribute("id")]
    public int BookId { get; set; }

    [XmlElement("Title")]
    public string Title { get; set; }

    [XmlElement("Author")]
    public string Author { get; set; }

    [XmlAttribute("isbn")]
    public string ISBN { get; set; }
}

var book = new Book
{
    BookId = 1,
    Title = "XML for Beginners",
    Author = "John Writer",
    ISBN = "978-1234567890"
};

string xml = xmlService.SerializeToString(book);
```

**Output:**
```xml
<Book id="1" isbn="978-1234567890">
  <Title>XML for Beginners</Title>
  <Author>John Writer</Author>
</Book>
```

### Collections

```csharp
[XmlRoot("ProductCatalog")]
public class ProductCatalog
{
    [XmlElement("Product")]
    public List<Product> Products { get; set; }
}

var catalog = new ProductCatalog
{
    Products = new List<Product>
    {
        new Product { ProductId = 1, Name = "Widget", Price = 19.99m },
        new Product { ProductId = 2, Name = "Gadget", Price = 29.99m }
    }
};

await xmlService.SerializeAsync("catalog.xml", catalog);
```

## Dependency Injection Setup

### ASP.NET Core Registration

```csharp
public void ConfigureServices(IServiceCollection services)
{
    services.AddScoped<IXmlService, XmlService>();
}
```

### Usage in Controllers

```csharp
public class ExportController : ControllerBase
{
    private readonly IXmlService _xmlService;

    public ExportController(IXmlService xmlService)
    {
        _xmlService = xmlService;
    }

    [HttpGet("export/customers")]
    public async Task<IActionResult> ExportCustomers()
    {
        var customers = await _dbContext.Customers.ToListAsync();

        var tempFile = Path.GetTempFileName() + ".xml";
        await _xmlService.ExportDynamic(tempFile, customers);

        var fileBytes = await System.IO.File.ReadAllBytesAsync(tempFile);
        System.IO.File.Delete(tempFile);

        return File(fileBytes, "application/xml", "customers.xml");
    }
}
```

## Common Scenarios

### Scenario 1: Export Database Query Results

```csharp
public class ReportService
{
    private readonly IXmlService _xmlService;
    private readonly IDbContext _dbContext;

    public async Task ExportOrdersToXmlAsync(DateTime fromDate)
    {
        var orders = await _dbContext.Orders
            .Where(o => o.OrderDate >= fromDate)
            .Select(o => new
            {
                o.OrderNumber,
                o.OrderDate,
                CustomerName = o.Customer.Name,
                o.TotalAmount,
                ItemCount = o.OrderItems.Count
            })
            .ToListAsync();

        var fileName = $"orders-{fromDate:yyyy-MM-dd}.xml";
        await _xmlService.ExportDynamic(fileName, orders);
    }
}
```

### Scenario 2: Configuration File Management

```csharp
[XmlRoot("AppConfiguration")]
public class AppConfiguration
{
    public string ConnectionString { get; set; }
    public int TimeoutSeconds { get; set; }
    public bool EnableLogging { get; set; }
    public List<string> AllowedHosts { get; set; }
}

public class ConfigurationManager
{
    private readonly IXmlService _xmlService;
    private const string ConfigFile = "app-config.xml";

    public async Task<AppConfiguration> LoadConfigurationAsync()
    {
        if (File.Exists(ConfigFile))
        {
            return await _xmlService.DeserializeAsync<AppConfiguration>(ConfigFile)
                   ?? CreateDefaultConfiguration();
        }

        return CreateDefaultConfiguration();
    }

    public async Task SaveConfigurationAsync(AppConfiguration config)
    {
        await _xmlService.SerializeAsync(ConfigFile, config);
    }

    private AppConfiguration CreateDefaultConfiguration()
    {
        return new AppConfiguration
        {
            ConnectionString = "Server=localhost;Database=MyDb",
            TimeoutSeconds = 30,
            EnableLogging = true,
            AllowedHosts = new List<string> { "localhost", "*.example.com" }
        };
    }
}
```

### Scenario 3: API Response Export

```csharp
[ApiController]
[Route("api/[controller]")]
public class CustomersController : ControllerBase
{
    private readonly IXmlService _xmlService;
    private readonly ICustomerRepository _customerRepo;

    [HttpGet("export")]
    public async Task<IActionResult> ExportAsXml()
    {
        var customers = await _customerRepo.GetAllActiveAsync();

        // Dynamic export
        var xmlString = xmlService.SerializeToString(customers.Select(c => new
        {
            c.CustomerId,
            c.Name,
            c.Email,
            c.Phone,
            AccountCreated = c.CreatedDate.ToString("yyyy-MM-dd")
        }).ToList());

        return Content(xmlString, "application/xml");
    }
}
```

### Scenario 4: Data Migration

```csharp
public class DataMigrationService
{
    private readonly IXmlService _xmlService;

    public async Task ExportLegacyDataAsync()
    {
        // Export old system data
        var legacyData = await GetLegacyDataAsync();
        await _xmlService.ExportDynamic("legacy-export.xml", legacyData);
    }

    public async Task ImportToNewSystemAsync()
    {
        // Import into new system
        var importedData = await _xmlService.DeserializeAsync<LegacyDataModel>("legacy-export.xml");

        foreach (var record in importedData.Records)
        {
            await ProcessLegacyRecordAsync(record);
        }
    }
}
```

### Scenario 5: Sanitized Element Names

The service automatically sanitizes invalid XML element names:

```csharp
var data = new
{
    Id = 1,
    Name = "Test",
    Special_Field = "Value",      // Underscores are kept
    Email_Address = "test@example.com",  // Underscores replace spaces
    _PrivateField = "Hidden"       // Leading underscore is kept
};

await xmlService.ExportDynamic("sanitized.xml", data);
```

**Output:**
```xml
<Data>
  <Id>1</Id>
  <Name>Test</Name>
  <Special_Field>Value</Special_Field>
  <Email_Address>test@example.com</Email_Address>
  <_PrivateField>Hidden</_PrivateField>
</Data>
```

### Scenario 6: Nested Complex Objects

```csharp
public class Company
{
    public string Name { get; set; }
    public Address Headquarters { get; set; }
    public List<Department> Departments { get; set; }
}

public class Address
{
    public string Street { get; set; }
    public string City { get; set; }
    public string Country { get; set; }
}

public class Department
{
    public string Name { get; set; }
    public int EmployeeCount { get; set; }
}

var company = new Company
{
    Name = "Acme Corp",
    Headquarters = new Address
    {
        Street = "123 Main St",
        City = "Springfield",
        Country = "USA"
    },
    Departments = new List<Department>
    {
        new Department { Name = "Engineering", EmployeeCount = 50 },
        new Department { Name = "Sales", EmployeeCount = 30 }
    }
};

await xmlService.SerializeAsync("company.xml", company);
```

### Scenario 7: Handling Null Values

```csharp
public class OptionalData
{
    public int Id { get; set; }
    public string Name { get; set; }

    [XmlElement(IsNullable = true)]
    public string? OptionalField { get; set; }

    [XmlIgnore]
    public string IgnoredField { get; set; }
}

var data = new OptionalData
{
    Id = 1,
    Name = "Test",
    OptionalField = null,  // Will be serialized as null
    IgnoredField = "Not in XML"  // Will not appear in XML
};

string xml = xmlService.SerializeToString(data);
```

### Scenario 8: Date/Time Handling

```csharp
public class EventData
{
    public DateTime EventDate { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    // Custom format
    [XmlIgnore]
    public DateTime CustomDate { get; set; }

    [XmlElement("CustomDate")]
    public string CustomDateFormatted
    {
        get => CustomDate.ToString("yyyy-MM-dd");
        set => CustomDate = DateTime.Parse(value);
    }
}

var eventData = new EventData
{
    EventDate = DateTime.Now,
    CreatedAt = DateTimeOffset.Now,
    CustomDate = DateTime.Now
};

await xmlService.SerializeAsync("event.xml", eventData);
```

## Element Name Sanitization

The dynamic XML export automatically sanitizes element names to ensure valid XML:

### Sanitization Rules

1. **Spaces** → Replaced with underscore (`_`)
2. **Hyphens** → Replaced with underscore (`_`)
3. **Invalid start character** → Prefix with underscore

```csharp
var data = new Dictionary<string, object>
{
    ["valid-name"] = "Value1",        // Becomes: valid_name
    ["name with spaces"] = "Value2",  // Becomes: name_with_spaces
    ["123numeric"] = "Value3",        // Becomes: _123numeric
    ["_already_valid"] = "Value4"     // Stays: _already_valid
};
```

## Supported Types

### Dynamic Export Supports

- Primitive types (int, string, bool, decimal, etc.)
- DateTime and DateTimeOffset
- TimeSpan and Guid
- Collections (arrays, lists, enumerables)
- Anonymous types
- ExpandoObject
- Regular classes (via reflection)
- Nested objects

### Strongly-Typed Operations Support

All types supported by `System.Xml.Serialization.XmlSerializer`:
- Classes with parameterless constructors
- Properties with public getters/setters
- Collections (List<T>, Array, etc.)
- Enums
- Nullable types

## Best Practices

1. **Choose the right approach**:
   - Use **dynamic** for quick exports and reporting
   - Use **strongly-typed** for configuration and data contracts

2. **Handle exceptions**:
   ```csharp
   try
   {
       await xmlService.SerializeAsync("data.xml", myData);
   }
   catch (Exception ex)
   {
       _logger.LogError(ex, "Failed to serialize data");
       throw;
   }
   ```

3. **Validate deserialized data**:
   ```csharp
   var data = await xmlService.DeserializeAsync<MyData>("file.xml");
   if (data == null)
   {
       throw new InvalidDataException("Failed to deserialize XML");
   }
   ```

4. **Use XML attributes for metadata**:
   ```csharp
   [XmlAttribute("version")]
   public string Version { get; set; }
   ```

5. **Clean up temporary files**:
   ```csharp
   var tempFile = Path.GetTempFileName() + ".xml";
   try
   {
       await xmlService.ExportDynamic(tempFile, data);
       // Use file...
   }
   finally
   {
       File.Delete(tempFile);
   }
   ```

6. **Use async methods**: All file operations are async for better scalability

7. **Directory handling**: The service automatically creates directories when serializing to files

## Performance Considerations

### Memory Usage

```csharp
// For large datasets, work with files instead of strings
await xmlService.SerializeAsync("large-data.xml", hugeDataset);

// Instead of
string xml = xmlService.SerializeToString(hugeDataset); // Holds entire XML in memory
```

### Async Operations

```csharp
// All file operations are async
await xmlService.SerializeAsync("file.xml", data);
await xmlService.DeserializeAsync<MyType>("file.xml");

// String operations are synchronous (in-memory)
string xml = xmlService.SerializeToString(data);
var obj = xmlService.DeserializeFromString<MyType>(xml);
```

## Related Documentation

- [Email Services](Email.md) - Email services with attachments
- [PDF Services](PDF.md) - PDF generation and optimization
- [Azure Services](Azure.md) - Azure document translation services
