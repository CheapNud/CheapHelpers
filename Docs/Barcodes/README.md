# CheapHelpers.Barcodes Documentation

Barcode generation and reading on ZXing.Net with no native image dependency, so it runs on Android without an image library.

## Package Information

- **Package**: CheapHelpers.Barcodes
- **Version**: 4.0.0
- **Target Framework**: .NET 11.0 (net11.0)
- **NuGet**: [CheapHelpers.Barcodes](https://www.nuget.org/packages/CheapHelpers.Barcodes)

## Usage

### Register

```csharp
using CheapHelpers.Barcodes;

builder.Services.AddCheapBarcodes(); // IBarcodeService as a singleton
```

### Generate

`GetBarcode` returns an 8-bit grayscale PNG. ZXing may make the image wider than requested when the barcode needs more room.

```csharp
byte[] code39 = barcodes.GetBarcode("HELLO-123", height: 80, width: 300);
byte[] qr = barcodes.GetBarcode("https://example.test", 200, 200, BarcodeFormat.QR_CODE);
```

### Read from pixels

Pass a pixel buffer the platform already decoded, such as a camera frame or an Android bitmap. The result is `null` when no barcode is found.

```csharp
var read = barcodes.ReadBarcode(pixels, width, height, RGBLuminanceSource.BitmapFormat.RGBA32);
if (read is { } found)
    Debug.WriteLine($"{found.Text} ({found.Format})");
```

### Read from an image file

Decoding JPEG or PNG bytes needs an image library, so that overload lives in CheapHelpers.Services as an extension method.

```csharp
using CheapHelpers.Services.Communication.Barcode;

var read = barcodes.ReadBarcode(File.ReadAllBytes("label.jpg"));
```

### Scan events

`OnScanAsync(barcode)` raises `BarcodeScanned` for every subscriber, for example when a hardware scanner or a JS scanner reports a code. The service is a singleton, so the event is app-wide. In a multi-user host such as Blazor Server, every circuit sees every scan, so filter per user and unsubscribe when the component is disposed.

## Dependencies

- ZXing.Net (0.16.11)
- System.IO.Hashing (10.0.13), for the PNG chunk checksums
