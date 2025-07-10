using iText.Kernel.Colors;

namespace CheapHelpers.Services.DataExchange.Pdf.Templates
{
    public record PdfColorScheme
    {
        public DeviceRgb Primary { get; init; }
        public DeviceRgb Secondary { get; init; }
        public DeviceRgb Background { get; init; }
        public DeviceRgb Text { get; init; }
        public DeviceRgb Border { get; init; }

        public static readonly PdfColorScheme Default = new()
        {
            Primary = new DeviceRgb(116, 116, 100),
            Secondary = new DeviceRgb(144, 120, 64),
            Background = new DeviceRgb(245, 245, 245),
            Text = new DeviceRgb(51, 51, 51),
            Border = new DeviceRgb(200, 200, 200)
        };

        public static readonly PdfColorScheme Sober = new()
        {
            Primary = new DeviceRgb(188, 224, 255),
            Secondary = new DeviceRgb(119, 119, 119),
            Background = new DeviceRgb(211, 211, 211),
            Text = new DeviceRgb(0, 0, 0),
            Border = new DeviceRgb(200, 200, 200)
        };
    }
}