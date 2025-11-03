# StringExtensions

Comprehensive string manipulation and validation extension methods.

## Overview

The `StringExtensions` class provides a collection of extension methods for common string operations including capitalization, validation, phone number formatting, character filtering, and string truncation.

## Namespace

```csharp
using CheapHelpers.Extensions;
```

## Methods

### Capitalize

Converts the first character of a string to uppercase and the rest to lowercase.

**Signature:**
```csharp
public static string Capitalize(this string str)
```

**Parameters:**
- `str`: The string to capitalize (cannot be null or whitespace)

**Returns:** String with first character uppercase and remaining lowercase

**Throws:** `ArgumentException` if the string is null or whitespace

**Example:**
```csharp
string text = "hello WORLD";
string capitalized = text.Capitalize();
// Result: "Hello world"
```

### IsDigitsOnly

Checks if a string contains only numeric digits (0-9).

**Signature:**
```csharp
public static bool IsDigitsOnly(this string str)
```

**Parameters:**
- `str`: The string to validate

**Returns:** `true` if the string contains only digits; otherwise `false`

**Throws:** `ArgumentException` if the string is null or whitespace

**Example:**
```csharp
"12345".IsDigitsOnly();      // true
"123abc".IsDigitsOnly();     // false
"123.45".IsDigitsOnly();     // false
```

### CheckBool

Converts Dutch boolean string representation (j/n) to boolean value.

**Signature:**
```csharp
public static bool CheckBool(this string str)
```

**Parameters:**
- `str`: String to check (typically "j" for ja/yes or "n" for nee/no)

**Returns:** `true` if the string equals "j" (case-insensitive); otherwise `false`

**Example:**
```csharp
"j".CheckBool();    // true
"J".CheckBool();    // true
"n".CheckBool();    // false
"".CheckBool();     // false
```

### ToInternationalPhoneNumber

Converts various phone number formats to international format (+31 or +32).

**Signature:**
```csharp
public static string ToInternationalPhoneNumber(this string phonenumber)
```

**Parameters:**
- `phonenumber`: Phone number string in various formats

**Returns:** International format phone number starting with +, or `null` if conversion fails

**Throws:** `ArgumentException` if the phone number is null or whitespace

**Supported Formats:**
- Numbers starting with '+' are returned as-is
- Numbers starting with '00' are converted to '+' format
- Dutch numbers starting with '06' are converted to '+31'
- Belgian numbers starting with '0' are converted to '+32'

**Example:**
```csharp
"06 12 34 56 78".ToInternationalPhoneNumber();   // "+31612345678"
"0032 123 456".ToInternationalPhoneNumber();     // "+32123456"
"+31 6 1234 5678".ToInternationalPhoneNumber();  // "+31612345678"
"00316123456".ToInternationalPhoneNumber();      // "+316123456"
```

### CharArrayToString

Concatenates character enumerable into a single string.

**Signature:**
```csharp
public static string CharArrayToString(this IEnumerable<char> input)
```

**Parameters:**
- `input`: Enumerable of characters

**Returns:** Concatenated string

**Example:**
```csharp
char[] chars = { 'H', 'e', 'l', 'l', 'o' };
string text = chars.CharArrayToString();
// Result: "Hello"
```

### StringArrayToString

Concatenates string enumerable into a single string.

**Signature:**
```csharp
public static string StringArrayToString(this IEnumerable<string> input)
```

**Parameters:**
- `input`: Enumerable of strings

**Returns:** Concatenated string

**Example:**
```csharp
string[] words = { "Hello", " ", "World" };
string text = words.StringArrayToString();
// Result: "Hello World"
```

### ToShortString

Truncates a string to a specified length and appends "..." if it exceeds that length.

**Signature:**
```csharp
public static string ToShortString(this string input, int chars = 20)
```

**Parameters:**
- `input`: The string to truncate
- `chars`: Maximum length before truncation (default: 20)

**Returns:** Truncated string with "..." appended if needed

**Example:**
```csharp
string longText = "This is a very long text that needs truncation";
string shortened = longText.ToShortString(15);
// Result: "This is a very ..."
```

### TrimWithEllipsis

Trims a string to a specified maximum length and appends "..." if it exceeds that length.

**Signature:**
```csharp
public static string TrimWithEllipsis(this string input, int maxLength)
```

**Parameters:**
- `input`: The string to trim
- `maxLength`: The maximum text length (ellipsis will be added after this length)

**Returns:** Trimmed string with ellipsis if needed

**Example:**
```csharp
string text = "Hello World";
string trimmed = text.TrimWithEllipsis(8);
// Result: "Hello Wo..."
```

### RemoveSpecialCharacters

Removes all special characters from a string, keeping only alphanumeric characters.

**Signature:**
```csharp
public static string RemoveSpecialCharacters(this string str)
```

**Parameters:**
- `str`: The string to process

**Returns:** String with only alphanumeric characters (A-Z, a-z, 0-9)

**Example:**
```csharp
string input = "Hello, World! @2024";
string cleaned = input.RemoveSpecialCharacters();
// Result: "HelloWorld2024"
```

### RemoveSpecialCharactersKeepDash

Removes special characters from a string, keeping alphanumeric characters and dashes. Consecutive dashes are collapsed to a single dash.

**Signature:**
```csharp
public static string RemoveSpecialCharactersKeepDash(this string str)
```

**Parameters:**
- `str`: The string to process

**Returns:** String with alphanumeric characters and single dashes

**Example:**
```csharp
string input = "Hello---World! 2024";
string cleaned = input.RemoveSpecialCharactersKeepDash();
// Result: "Hello-World2024"
```

### Sanitize

Sanitizes a string for safe usage by converting spaces to underscores, slashes to dashes, and keeping only alphanumeric and common safe characters.

**Signature:**
```csharp
public static string Sanitize(this string str)
```

**Parameters:**
- `str`: The string to sanitize

**Returns:** Sanitized string safe for general use (file names, URLs, etc.)

**Character Transformations:**
- Spaces → Underscores
- Slashes → Dashes
- Keeps: Letters, digits, underscores, dashes, periods
- Removes: All other special characters

**Example:**
```csharp
string input = "Hello World/Test File!.txt";
string sanitized = input.Sanitize();
// Result: "Hello_World-Test_File.txt"
```

## Common Use Cases

### File Name Sanitization
```csharp
string userInput = "My Document (Final).docx";
string safeName = userInput.Sanitize();
// Use safeName for file operations
```

### URL Parameter Cleaning
```csharp
string searchTerm = "C# .NET Framework";
string urlSafe = searchTerm.RemoveSpecialCharactersKeepDash();
// Use in URL construction
```

### Display Text Truncation
```csharp
string description = GetLongDescription();
string preview = description.TrimWithEllipsis(100);
// Display preview in UI
```

### Phone Number Normalization
```csharp
string rawPhone = "06-1234-5678";
string international = rawPhone.ToInternationalPhoneNumber();
// Store normalized format in database
```

## Tips and Best Practices

1. **Null Safety**: Most methods throw exceptions on null or whitespace input. Always validate user input before calling these methods or wrap in try-catch blocks.

2. **Phone Number Formatting**: The `ToInternationalPhoneNumber` method is designed for Dutch (06) and Belgian (0) numbers. For other countries, implement custom logic.

3. **String Truncation**: Use `ToShortString` for display purposes with default length, and `TrimWithEllipsis` when you need precise control over the maximum length.

4. **Character Filtering**:
   - Use `RemoveSpecialCharacters` for alphanumeric-only output
   - Use `RemoveSpecialCharactersKeepDash` for URL slugs or file names
   - Use `Sanitize` for general-purpose safe strings

5. **Performance**: Methods like `CharArrayToString` and `StringArrayToString` use `string.Concat` for optimal performance.

6. **Validation**: Use `IsDigitsOnly` for input validation before parsing numeric strings to avoid exceptions.
