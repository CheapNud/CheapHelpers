using BlazorDownloadFile;
using Microsoft.Extensions.Localization;
using Microsoft.JSInterop;
using MimeMapping;
using MudBlazor;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace CheapHelpers.Blazor
{
    public class DownloadHelper(IBlazorDownloadFileService download, ISnackbar toast, IStringLocalizer loc, IJSRuntime js)
    {
        private readonly IBlazorDownloadFileService download = download;
        private readonly ISnackbar toast = toast;
        private readonly IStringLocalizer loc = loc;
        private readonly IJSRuntime js = js;

        public async Task<string> CaptureDivAsPng(string fileName, string divId, int width = 800, int height = 600)
        {
            var options = new
            {
                //backgroundColor = "white", // Optional: Specify background
                width,
                height
            };

            var imageData = await js.InvokeAsync<string>("htmlToPng", divId, options);
            return imageData;
        }

        public async Task<string> CaptureDivAsJpg(string fileName, string divId, double quality = 0.92, int width = 800, int height = 600)
        {
            var options = new
            {
                backgroundColor = "white", // Optional: Specify background
                width,
                height
            };
            var imageData = await js.InvokeAsync<string>("htmlToJpg", divId, options, quality);
            return imageData;
        }

        public async Task DownloadDivAsJpg(string fileName, string divId, double quality = 0.92, int width = 800, int height = 600)
        {
            var imageData = await CaptureDivAsJpg(fileName, divId, quality, width, height);
            await DownloadBase64(fileName, imageData);
        }

        public async Task DownloadDivAsPng(string fileName, string divId, int width = 800, int height = 600)
        {
            var imageData = await CaptureDivAsPng(fileName, divId, width, height);
            await DownloadBase64(fileName, imageData);
        }

        public async Task DownloadBase64(string fileName, string base64)
        {
            try
            {
                byte[] bytes = Convert.FromBase64String(base64.Split(",")[1]);
                await download.DownloadFile(fileName, bytes, MimeUtility.GetMimeMapping(fileName));
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                throw;
            }
        }

        public async Task Download(string filePath, bool deleteFile = true)
        {
            try
            {
                //present as download
                if (!System.IO.File.Exists(filePath))
                {
                    toast.Add(loc["Error"], Severity.Error);
                    return;
                }

                using var memory = new MemoryStream();
                using (var stream = new FileStream(filePath, FileMode.Open))
                {
                    if (stream == null)
                    {
                        toast.Add(loc["Error"], Severity.Error);
                        return;
                    }
                    await stream.CopyToAsync(memory);
                }

                memory.Position = 0;
                var result = await download.DownloadFile(Path.GetFileName(filePath), memory, MimeUtility.GetMimeMapping(filePath));

                if (result.Succeeded && deleteFile && File.Exists(filePath))
                {
                    File.Delete(filePath);
                }
                else
                {
                    Debug.WriteLine(@$"skipped deleting {filePath}");
                    if (!result.Succeeded)
                    {
                        Debug.WriteLine(@$"download result failed");
                        toast.Add(loc["Error"], Severity.Error);
                    }
                }
            }
            catch (System.Exception ex)
            {
                Debug.WriteLine(ex.Message);
                throw;
            }
        }
    }
}
