using CheapHelpers.Services.DataExchange.Pdf.Configuration;
using CheapHelpers.Services.DataExchange.Pdf.Helpers;
using iText.Kernel.Geom;

namespace CheapHelpers.Services.DataExchange.Pdf.Templates
{
    // Template builder for easy configuration
    public static class PdfTemplateBuilder
    {
        public static PdfDocumentTemplate CreateOrderTemplate()
        {
            return new PdfDocumentTemplate
            {
                Title = "Orders",
                PageSize = PageSize.A4.Rotate(),
                UseFooter = true,
                FooterTemplate = "Orders Export",
                ColorScheme = PdfColorScheme.Sober,
                Columns =
                [
                    new() { PropertyName = "OrderNumber", DisplayName = "Ordernummer", Width = 1f },
                    new() { PropertyName = "CreationDate", DisplayName = "Creatie Datum", Width = 1f, ValueFormatter = obj => ((DateTime)obj).ToShortDateString() },
                    new() { PropertyName = "DeliveryDate", DisplayName = "Lever Datum", Width = 1f, ValueFormatter = obj => ((DateTime)obj).ToShortDateString() },
                    new() { PropertyName = "VblCode", DisplayName = "Vblnummer", Width = 1f },
                    new() { PropertyName = "OldModelCode", DisplayName = "Model", Width = 1f },
                    new() { PropertyName = "Fabrics", DisplayName = "Stof", Width = 1f },
                    new() { PropertyName = "Customer.Code", DisplayName = "Klant", Width = 1f }
                ]
            };
        }

        public static PdfDocumentTemplate CreateServiceTemplate()
        {
            return new PdfDocumentTemplate
            {
                Title = "Service Rapport",
                UseHeader = true,
                UseFooter = true,
                ColorScheme = PdfColorScheme.Default,
                Sections =
                [
                    new PdfSectionConfig
                    {
                        Title = "Service Details",
                        Columns =
                        [
                            new() { PropertyName = "ServiceNumber", DisplayName = "Service Nr", Width = 1f },
                            new() { PropertyName = "Customer.Name", DisplayName = "Klant", Width = 2f },
                            new() { PropertyName = "ExecutionDate", DisplayName = "Uitvoering", Width = 1f }
                        ]
                    }
                ]
            };
        }
    }
}