using CsvHelper;
using CsvHelper.Configuration;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;

namespace CheapHelpers.Services
{
    public class CsvService : ICsvService
    {
        /// <summary>
        /// generic export module
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="list"></param>
        /// <returns></returns>
        public async Task Export(string filePath, IEnumerable<string> list)
        {
            using (StreamWriter writer = new StreamWriter(filePath, false))
            {
                var config = new CsvConfiguration(CultureInfo.CreateSpecificCulture("nl-BE")) { Delimiter = ";" };
                using (var csvWriter = new CsvWriter(writer, config))
                {
                    await csvWriter.WriteRecordsAsync(list);
                }
            }
        }

        /// <summary>
        /// generic export module
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="list"></param>
        /// <returns></returns>
        public async Task Export(string filePath, IEnumerable<dynamic> list)
        {
            using (StreamWriter writer = new StreamWriter(filePath, false))
            {
                var config = new CsvConfiguration(CultureInfo.CreateSpecificCulture("nl-BE")) { Delimiter = ";" };
                using (var csvWriter = new CsvWriter(writer, config))
                {
                    await csvWriter.WriteRecordsAsync(list);
                }
            }
        }

    }
}
