# BitHelper

Bit manipulation and byte conversion utilities.

## Overview

The `BitHelper` class provides utilities for low-level bit operations, byte manipulation, endianness handling, and hexadecimal string conversions. Essential for binary file parsing, network protocols, and hardware interfacing.

## Namespace

```csharp
using CheapHelpers.Helpers;
```

## Bit Extraction Methods

### GetBit (byte)

Gets a bit value at a specific position from a byte.

**Signature:**
```csharp
public static int GetBit(byte b, int bitNumber)
```

**Parameters:**
- `b`: The byte to extract from
- `bitNumber`: Bit position (0-7), counted from LSB(0) to MSB(7)

**Returns:** The bit value (0 or 1)

**Example:**
```csharp
byte value = 0b10110100;  // Binary: 10110100

int bit0 = BitHelper.GetBit(value, 0);  // 0 (LSB)
int bit1 = BitHelper.GetBit(value, 1);  // 0
int bit2 = BitHelper.GetBit(value, 2);  // 1
int bit3 = BitHelper.GetBit(value, 3);  // 0
int bit4 = BitHelper.GetBit(value, 4);  // 1
int bit5 = BitHelper.GetBit(value, 5);  // 1
int bit6 = BitHelper.GetBit(value, 6);  // 0
int bit7 = BitHelper.GetBit(value, 7);  // 1 (MSB)
```

### GetBit (short)

Gets a bit value at a specific position from a short.

**Signature:**
```csharp
public static int GetBit(short b, int bitNumber)
```

**Parameters:**
- `b`: The short to extract from
- `bitNumber`: Bit position, counted from LSB to MSB

**Returns:** The bit value (0 or 1)

**Example:**
```csharp
short value = 0b0000000010110100;
int bit10 = BitHelper.GetBit(value, 10);  // 1
```

### GetBit (ushort)

Gets a bit value at a specific position from an unsigned short.

**Signature:**
```csharp
public static int GetBit(ushort b, int bitNumber)
```

**Parameters:**
- `b`: The ushort to extract from
- `bitNumber`: Bit position, counted from LSB to MSB

**Returns:** The bit value (0 or 1)

**Example:**
```csharp
ushort value = 0xFFFF;  // All bits set
int bit15 = BitHelper.GetBit(value, 15);  // 1
```

## Byte Concatenation

### ConcatBytesToInt

Concatenates multiple bytes into a single integer value.

**Signature:**
```csharp
public static int ConcatBytesToInt(params byte[] bytes)
```

**Parameters:**
- `bytes`: 1-4 bytes to concatenate

**Returns:** Integer value created from concatenated bytes

**Throws:**
- `ArgumentOutOfRangeException`: If more than 4 bytes or less than 1 byte provided

**Example:**
```csharp
// Single byte
int value1 = BitHelper.ConcatBytesToInt(0xFF);
// Result: 255

// Two bytes (like ushort)
int value2 = BitHelper.ConcatBytesToInt(0x12, 0x34);
// Result: 0x1234 = 4660

// Three bytes
int value3 = BitHelper.ConcatBytesToInt(0x12, 0x34, 0x56);
// Result: 0x123456 = 1193046

// Four bytes (full int)
int value4 = BitHelper.ConcatBytesToInt(0x12, 0x34, 0x56, 0x78);
// Result: 0x12345678 = 305419896
```

## Little-Endian Parsing

### ParseLittleEndianInt16

Parses a little-endian Int16 from a byte buffer.

**Signature:**
```csharp
public static short ParseLittleEndianInt16(byte[] buffer, int offset)
```

**Parameters:**
- `buffer`: The byte buffer
- `offset`: Offset in the buffer to start reading

**Returns:** Parsed Int16 value

**Example:**
```csharp
byte[] buffer = { 0x00, 0x34, 0x12, 0x00 };
short value = BitHelper.ParseLittleEndianInt16(buffer, 1);
// Reads bytes at offset 1-2: [0x34, 0x12]
// Little-endian: 0x1234
// Result: 4660
```

### ParseLittleEndianUInt16

Parses a little-endian UInt16 from a byte buffer.

**Signature:**
```csharp
public static ushort ParseLittleEndianUInt16(byte[] buffer, int offset)
```

**Parameters:**
- `buffer`: The byte buffer
- `offset`: Offset in the buffer to start reading

**Returns:** Parsed UInt16 value

**Example:**
```csharp
byte[] buffer = { 0xFF, 0xFF };
ushort value = BitHelper.ParseLittleEndianUInt16(buffer, 0);
// Result: 65535
```

### ParseLittleEndianInt16Bits

Parses specific bits from a little-endian Int16 in a byte buffer.

**Signature:**
```csharp
public static short ParseLittleEndianInt16Bits(
    byte[] buffer,
    int offset,
    int bitOffset,
    int bitLength)
```

**Parameters:**
- `buffer`: The byte buffer
- `offset`: Offset in the buffer to start reading
- `bitOffset`: Bit offset within the Int16
- `bitLength`: Number of bits to extract

**Returns:** Parsed bit value as Int16

**Example:**
```csharp
byte[] buffer = { 0xFF, 0x0F };  // 0x0FFF in little-endian
short value = BitHelper.ParseLittleEndianInt16Bits(buffer, 0, 4, 8);
// Reads Int16: 0x0FFF
// Shifts right by 4 bits: 0x00FF
// Masks to 8 bits: 0x00FF
// Result: 255
```

### ParseLittleEndianUInt16Bits

Parses specific bits from a little-endian UInt16 in a byte buffer.

**Signature:**
```csharp
public static ushort ParseLittleEndianUInt16Bits(
    byte[] buffer,
    int offset,
    int bitOffset,
    int bitLength)
```

**Parameters:**
- `buffer`: The byte buffer
- `offset`: Offset in the buffer to start reading
- `bitOffset`: Bit offset within the UInt16
- `bitLength`: Number of bits to extract

**Returns:** Parsed bit value as UInt16

**Example:**
```csharp
byte[] buffer = { 0xAB, 0xCD };
ushort value = BitHelper.ParseLittleEndianUInt16Bits(buffer, 0, 0, 4);
// Extracts lowest 4 bits from little-endian UInt16
```

### ParseLittleEndianInt32

Parses a little-endian Int32 from a byte buffer.

**Signature:**
```csharp
public static int ParseLittleEndianInt32(byte[] buffer, int offset)
```

**Parameters:**
- `buffer`: The byte buffer
- `offset`: Offset in the buffer to start reading

**Returns:** Parsed Int32 value

**Example:**
```csharp
byte[] buffer = { 0x78, 0x56, 0x34, 0x12 };
int value = BitHelper.ParseLittleEndianInt32(buffer, 0);
// Little-endian: 0x12345678
// Result: 305419896
```

### ParseLittleEndianUInt32

Parses a little-endian UInt32 from a byte buffer.

**Signature:**
```csharp
public static uint ParseLittleEndianUInt32(byte[] buffer, int offset)
```

**Parameters:**
- `buffer`: The byte buffer
- `offset`: Offset in the buffer to start reading

**Returns:** Parsed UInt32 value

**Example:**
```csharp
byte[] buffer = { 0xFF, 0xFF, 0xFF, 0xFF };
uint value = BitHelper.ParseLittleEndianUInt32(buffer, 0);
// Result: 4294967295
```

## Hexadecimal Conversions

### HexStringToByteArray

Converts a hexadecimal string to a byte array.

**Signature:**
```csharp
public static byte[] HexStringToByteArray(string hexString)
```

**Parameters:**
- `hexString`: Hexadecimal string (without 0x prefix)

**Returns:** Byte array representation of the hex string

**Example:**
```csharp
byte[] bytes = BitHelper.HexStringToByteArray("A1B2C3");
// Result: [0xA1, 0xB2, 0xC3]

byte[] hash = BitHelper.HexStringToByteArray("DEADBEEF");
// Result: [0xDE, 0xAD, 0xBE, 0xEF]
```

### HexStringToBytes (LINQ)

Converts a hexadecimal string to a byte array using LINQ.

**Signature:**
```csharp
public static byte[] HexStringToBytes(string hex)
```

**Parameters:**
- `hex`: Hexadecimal string (without 0x prefix)

**Returns:** Byte array representation of the hex string

**Example:**
```csharp
byte[] bytes = BitHelper.HexStringToBytes("A1B2C3");
// Result: [0xA1, 0xB2, 0xC3]
```

### ByteArrayToHexString

Converts a byte array to a hexadecimal string.

**Signature:**
```csharp
public static string ByteArrayToHexString(byte[] ba)
```

**Parameters:**
- `ba`: Byte array to convert

**Returns:** Hexadecimal string representation (lowercase, without 0x prefix)

**Example:**
```csharp
byte[] bytes = { 0xDE, 0xAD, 0xBE, 0xEF };
string hex = BitHelper.ByteArrayToHexString(bytes);
// Result: "deadbeef"
```

## Common Use Cases

### Binary File Parsing

```csharp
public class BinaryFileParser
{
    public FileHeader ParseHeader(byte[] fileData)
    {
        // Read magic number (4 bytes, little-endian)
        uint magicNumber = BitHelper.ParseLittleEndianUInt32(fileData, 0);

        // Read version (2 bytes, little-endian)
        ushort version = BitHelper.ParseLittleEndianUInt16(fileData, 4);

        // Read flags (1 byte, extract individual bits)
        byte flags = fileData[6];
        bool isCompressed = BitHelper.GetBit(flags, 0) == 1;
        bool isEncrypted = BitHelper.GetBit(flags, 1) == 1;

        return new FileHeader
        {
            MagicNumber = magicNumber,
            Version = version,
            IsCompressed = isCompressed,
            IsEncrypted = isEncrypted
        };
    }
}
```

### Network Protocol Implementation

```csharp
public class PacketParser
{
    public Packet ParsePacket(byte[] data)
    {
        // Parse header
        ushort packetType = BitHelper.ParseLittleEndianUInt16(data, 0);
        ushort length = BitHelper.ParseLittleEndianUInt16(data, 2);
        uint sequenceNumber = BitHelper.ParseLittleEndianUInt32(data, 4);

        // Parse flags from single byte
        byte flagsByte = data[8];
        bool ackRequired = BitHelper.GetBit(flagsByte, 0) == 1;
        bool isFragment = BitHelper.GetBit(flagsByte, 1) == 1;
        bool isLastFragment = BitHelper.GetBit(flagsByte, 2) == 1;

        return new Packet
        {
            Type = (PacketType)packetType,
            Length = length,
            SequenceNumber = sequenceNumber,
            AckRequired = ackRequired,
            IsFragment = isFragment,
            IsLastFragment = isLastFragment
        };
    }
}
```

### Hardware Register Reading

```csharp
public class RegisterReader
{
    public RegisterStatus ReadStatus(ushort registerValue)
    {
        // Extract status bits from 16-bit register
        bool deviceReady = BitHelper.GetBit(registerValue, 0) == 1;
        bool errorFlag = BitHelper.GetBit(registerValue, 1) == 1;
        bool busyFlag = BitHelper.GetBit(registerValue, 2) == 1;

        // Extract 4-bit error code (bits 4-7)
        byte[] regBytes = BitConverter.GetBytes(registerValue);
        short errorCode = BitHelper.ParseLittleEndianInt16Bits(regBytes, 0, 4, 4);

        return new RegisterStatus
        {
            Ready = deviceReady,
            Error = errorFlag,
            Busy = busyFlag,
            ErrorCode = (byte)errorCode
        };
    }
}
```

### Checksum Calculation

```csharp
public class ChecksumCalculator
{
    public string CalculateChecksum(byte[] data)
    {
        // Calculate checksum
        int sum = 0;
        foreach (byte b in data)
        {
            sum += b;
        }

        // Convert to 4-byte checksum
        byte[] checksumBytes =
        {
            (byte)((sum >> 24) & 0xFF),
            (byte)((sum >> 16) & 0xFF),
            (byte)((sum >> 8) & 0xFF),
            (byte)(sum & 0xFF)
        };

        // Return as hex string
        return BitHelper.ByteArrayToHexString(checksumBytes);
    }

    public bool VerifyChecksum(byte[] data, string expectedChecksum)
    {
        string actualChecksum = CalculateChecksum(data);
        return actualChecksum.Equals(expectedChecksum, StringComparison.OrdinalIgnoreCase);
    }
}
```

### Data Serialization

```csharp
public class LittleEndianSerializer
{
    public byte[] SerializeStruct(DataStruct data)
    {
        var buffer = new List<byte>();

        // Serialize Int32 (little-endian)
        buffer.AddRange(new[]
        {
            (byte)(data.IntValue & 0xFF),
            (byte)((data.IntValue >> 8) & 0xFF),
            (byte)((data.IntValue >> 16) & 0xFF),
            (byte)((data.IntValue >> 24) & 0xFF)
        });

        // Serialize UInt16 (little-endian)
        buffer.AddRange(new[]
        {
            (byte)(data.ShortValue & 0xFF),
            (byte)((data.ShortValue >> 8) & 0xFF)
        });

        return buffer.ToArray();
    }

    public DataStruct DeserializeStruct(byte[] buffer)
    {
        return new DataStruct
        {
            IntValue = BitHelper.ParseLittleEndianInt32(buffer, 0),
            ShortValue = BitHelper.ParseLittleEndianUInt16(buffer, 4)
        };
    }
}
```

### Bitmap Manipulation

```csharp
public class BitmapProcessor
{
    public void ProcessPixel(byte[] rgbaData, int pixelIndex)
    {
        int offset = pixelIndex * 4;  // 4 bytes per pixel (RGBA)

        byte r = rgbaData[offset];
        byte g = rgbaData[offset + 1];
        byte b = rgbaData[offset + 2];
        byte a = rgbaData[offset + 3];

        // Check alpha channel bits
        bool isTransparent = BitHelper.GetBit(a, 0) == 0;
        bool isFullyOpaque = a == 0xFF;

        // Pack into 32-bit color
        int packedColor = BitHelper.ConcatBytesToInt(r, g, b, a);
    }
}
```

### Multi-Byte Value Extraction

```csharp
public class SensorDataParser
{
    public SensorReading ParseReading(byte[] data, int offset)
    {
        // Temperature: 2 bytes, little-endian, signed
        short rawTemp = BitHelper.ParseLittleEndianInt16(data, offset);
        double temperature = rawTemp / 100.0;  // Scale factor

        // Humidity: 2 bytes, bits 0-11 (12 bits)
        ushort rawHumidity = BitHelper.ParseLittleEndianUInt16Bits(
            data, offset + 2, 0, 12);
        double humidity = rawHumidity / 40.95;  // Scale factor

        // Pressure: 3 bytes concatenated
        int pressure = BitHelper.ConcatBytesToInt(
            data[offset + 4],
            data[offset + 5],
            data[offset + 6]);

        return new SensorReading
        {
            Temperature = temperature,
            Humidity = humidity,
            Pressure = pressure
        };
    }
}
```

## Tips and Best Practices

1. **Bit Numbering**: Bit 0 is always the LSB (Least Significant Bit), bit 7/15/31 is the MSB (Most Significant Bit).

2. **Endianness Awareness**: The little-endian methods are for reading data in little-endian format (common in x86, most file formats). For big-endian data, reverse the byte order first.

3. **Buffer Bounds**: Always ensure buffer offset + data size doesn't exceed buffer length:
   ```csharp
   if (offset + 4 > buffer.Length)
       throw new ArgumentOutOfRangeException("Buffer too small");
   int value = BitHelper.ParseLittleEndianInt32(buffer, offset);
   ```

4. **Hex String Format**: Hex strings should not include "0x" prefix. Remove it before parsing:
   ```csharp
   string hex = "0xDEADBEEF";
   byte[] bytes = BitHelper.HexStringToByteArray(hex.Replace("0x", ""));
   ```

5. **Performance**: For parsing many values, consider using `Span<byte>` and `BinaryPrimitives` from .NET Core for better performance:
   ```csharp
   // Modern alternative for .NET Core
   int value = BinaryPrimitives.ReadInt32LittleEndian(buffer.AsSpan(offset));
   ```

6. **Bit Masking**: When extracting multiple bits, use the bit-specific methods or create custom masks:
   ```csharp
   // Extract bits 4-7 (4 bits)
   byte value = data[0];
   byte extracted = (byte)((value >> 4) & 0x0F);
   ```

7. **Hex Case**: `ByteArrayToHexString` returns lowercase. For uppercase, modify the format string or use `ToUpper()`:
   ```csharp
   string upperHex = BitHelper.ByteArrayToHexString(bytes).ToUpper();
   ```

8. **ConcatBytesToInt Order**: Bytes are concatenated in the order provided (first byte becomes most significant):
   ```csharp
   int value = BitHelper.ConcatBytesToInt(0x12, 0x34);
   // Result: 0x1234, not 0x3412
   ```

9. **Signed vs Unsigned**: Use signed methods (Int16/Int32) for values that can be negative, unsigned (UInt16/UInt32) for values that are always positive.

10. **Binary Literals**: Use binary literals (0b prefix) for clarity when working with bit flags:
    ```csharp
    byte flags = 0b10110100;
    bool bit2 = BitHelper.GetBit(flags, 2) == 1;
    ```
