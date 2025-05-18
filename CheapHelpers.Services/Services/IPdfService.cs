using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace CheapHelpers.Services
{
    public interface IPdfService
    {
        void Optimize(string source, string destination);
        void Optimize(Stream source, Stream destination);

    }
}