using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace CheapHelpers.Services
{
    public interface IPdfService
    {
        void Optimize(string source, string destination);
        void Optimize(Stream source, Stream destination);
        Task<PdfOptimizationResult> OptimizeAsync(string source, string destination, PdfOptimizationLevel level = PdfOptimizationLevel.Balanced);
        Task<PdfOptimizationResult> OptimizeAsync(Stream source, Stream destination, PdfOptimizationLevel level = PdfOptimizationLevel.Balanced);
    }
}