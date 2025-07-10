using System;
using System.IO;
using System.Linq;

namespace CheapHelpers.Helpers.Files
{
    public static class FileHelper
    {
        /// <summary>
        /// ONLY FILENAME, use GetTrustedFileNameFromPath to use a fullpath or create a fileinfo
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        public static string GetTrustedFileName(string filename)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(filename);

            return @$"{Path.GetFileNameWithoutExtension(filename)}_{Guid.NewGuid().ToString("N")[..8][..8]}{Path.GetExtension(filename)}";
        }

        public static string GetTrustedFileName(FileInfo file)
        {
            ArgumentNullException.ThrowIfNull(file);
            ArgumentException.ThrowIfNullOrWhiteSpace(file.Name);

            return GetTrustedFileName(file.Name);
        }

        public static string GetTrustedFileNameFromPath(string filepath)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(filepath);

            return GetTrustedFileName(new FileInfo(filepath));
        }

        public static string GetTrustedFileNameFromTempPath(string filename)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(filename);

            return GetTrustedFileNameFromPath(Path.Combine(Path.GetTempPath(), filename));
        }

        public static string ChangeFileNameId(string filename)
        {
            var arr = Path.GetFileNameWithoutExtension(filename).Split('_');

            var filenamewoid = filename.Replace(@$"_{arr.Last()}", "");

            var a = @$"{filenamewoid}{Path.GetExtension(filename)}";
            return GetTrustedFileName(a);
        }
    }
}
