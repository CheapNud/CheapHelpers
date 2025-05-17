using ClosedXML.Excel;
using ClosedXML.Excel.Drawings;
using DocumentFormat.OpenXml.Spreadsheet;
using MoreLinq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace CheapHelpers.Services
{
    public class XlsxService : IXlsxService
    {
        Task IXlsxService.Generate(string filepath, List<dynamic> records)
        {
            throw new NotImplementedException();
        }
    }
}