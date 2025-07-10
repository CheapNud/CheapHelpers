using CheapHelpers.Services.Pdf.Templates;

namespace CheapHelpers.Services.Pdf.Export
{
    public interface IPdfExportService
    {
        Task<byte[]> ExportToPdfAsync<T>(IEnumerable<T> data, PdfDocumentTemplate template);
        Task ExportToPdfFileAsync<T>(IEnumerable<T> data, PdfDocumentTemplate template, string filePath);
        Task<byte[]> ExportSingleToPdfAsync<T>(T entity, PdfDocumentTemplate template);
    }
}