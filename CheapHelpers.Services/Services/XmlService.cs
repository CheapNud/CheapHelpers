using MoreLinq;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace CheapHelpers.Services
{
    public class XmlService : IXmlService
    {
        Task IXmlService.Export(string filePath, dynamic data)
        {
            throw new System.NotImplementedException();
        }
    }
}
