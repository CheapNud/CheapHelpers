using CheapHelpers.Helpers.Types;
using ClosedXML.Excel;
using System.Diagnostics;

namespace CheapHelpers.Services.DataExchange.Excel;

public class XlsxService : IXlsxService
{
    public async Task Generate(string filepath, List<dynamic> records)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filepath);
        ArgumentNullException.ThrowIfNull(records);

        try
        {
            await Task.Run(() =>
            {
                using var workbook = new XLWorkbook();
                var worksheet = workbook.Worksheets.Add("Data");

                if (records.Count == 0)
                {
                    Debug.WriteLine("No records provided to generate XLSX file");
                    workbook.SaveAs(filepath);
                    return;
                }

                // Get property names from first record for headers
                var firstRecord = records[0];
                var properties = DynamicHelper.GetPropertyNames(firstRecord);

                // Write headers
                for (int i = 0; i < properties.Count; i++)
                {
                    worksheet.Cell(1, i + 1).Value = properties[i];
                    worksheet.Cell(1, i + 1).Style.Font.Bold = true;
                }

                // Write data rows
                for (int rowIndex = 0; rowIndex < records.Count; rowIndex++)
                {
                    var record = records[rowIndex];

                    for (int colIndex = 0; colIndex < properties.Count; colIndex++)
                    {
                        var propertyName = properties[colIndex];
                        var value = DynamicHelper.GetPropertyValue(record, propertyName);
                        worksheet.Cell(rowIndex + 2, colIndex + 1).Value = value?.ToString() ?? string.Empty;
                    }
                }

                // Auto-fit columns
                worksheet.ColumnsUsed().AdjustToContents();

                workbook.SaveAs(filepath);
            });

            Debug.WriteLine($"Successfully generated XLSX file: {filepath} with {records.Count} records");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to generate XLSX file '{filepath}': {ex.Message}");
            throw;
        }
    }
}