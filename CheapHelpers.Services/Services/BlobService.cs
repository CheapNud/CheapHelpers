using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using CheapHelpers.Models;
using MimeMapping;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace CheapHelpers.Services
{
    /// <summary>
    /// Service for retrieving images from Azure blob containers
    /// BE CAREFULL: Get image returns an image string FOR BLAZOR, fix this.
    /// Put this in the main lib, and make GetFileByteArray return the byte stream of that image, same for base64, make a method for this as well
    /// Uri should repsond with a non available uri.
    /// </summary>
    public class BlobService(BlobServiceClient blobServiceClient)
    {
        public string GetFile(string filename, BlobContainers blobcontainer)
        {
            if (string.IsNullOrWhiteSpace(filename))
            {
                return "noimageplaceholder.jpg";
            }

            return GetFileUri(filename, blobcontainer).AbsoluteUri;
        }

        public async Task<byte[]> GetFileByteArray(string filename, BlobContainers blobcontainer)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(filename))
                {
                    return null;
                }

                var client = GetClient(filename, blobcontainer);

                try
                {
                    var result = await client.DownloadContentAsync();
                    return result.Value.Content.ToArray();
                }
                catch (Azure.RequestFailedException rfex)
                {
                    Debug.WriteLine(rfex.InnerException);
                    return null;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                throw;
            }
        }

        /// <summary>
        /// Do not use anymore, worked only in old library
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="blobcontainer"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public async Task<Stream> GetFileStream(string filename, string blobcontainer)
        {
            throw new NotImplementedException();

            //windows.storage.common vs WindowsAZure.Storage (deprecated)
            //deprecated library has a single working functional call, the new library always throws an error AFTER multiple calls, WTF microshit???? -> wait on fix, download content to ram for now.
            //i used to use the derprecated package but they are deprecated for a reason.

            //if (string.IsNullOrWhiteSpace(filename))
            //{
            //    return null;
            //}


            // This is one common way of creating a CloudStorageAccount object. You can get 
            // your Storage Account Name and Key from the Azure Portal.
            //StorageCredentials credentials = new StorageCredentials(, accountKey);
            //CloudStorageAccount storageAccount = new CloudStorageAccount(credentials, useHttps: true);
            //CloudStorageAccount storageAccount = CloudStorageAccount.Parse(CloudConfigurationManager.GetSetting("StorageConnectionString"));
            //CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
            // Another common way to create a CloudStorageAccount object is to use a connection string:
            // CloudStorageAccount storageAccount = CloudStorageAccount.Parse(connectionString);
            //CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
            // This call creates a local CloudBlobContainer object, but does not make a network call
            // to the Azure Storage Service. The container on the service that this object represents may
            // or may not exist at this point. If it does exist, the properties will not yet have been
            // popluated on this object.
            // This makes an actual service call to the Azure Storage service. Unless this call fails,
            // the container will have been created.
            //await blobContainer.CreateAsync();
            // This also does not make a service call, it only creates a local object.
            //CloudBlockBlob blockblob = container.GetBlockBlobReference(new CloudBlockBlob(blobUri).Name);
            // //return client.Uri.AbsoluteUri;
            //var result = await client.DownloadContentAsync();
            //return Convert.ToBase64String(result.Value.Content.ToArray());

            //var client = GetClient(filename, blobcontainer);
            //client.SetHttpHeaders(new BlobHttpHeaders { ContentType = "application/octet-stream" });
            //return await client.OpenReadAsync();

            //CloudBlobContainer blobContainer = _cloudBlobClient.GetContainerReference(blobcontainer);
            //CloudBlockBlob blob = blobContainer.GetBlockBlobReference(filename);
            //return await blob.OpenReadAsync();
        }

        public string GetFile(FileAttachment file, BlobContainers container)
        {
            if (file is null)
            {
                return "noimageplaceholder.jpg";
            }

            if (string.IsNullOrWhiteSpace(file.FileName))
            {
                return "noimageplaceholder.jpg";
            }

            return GetFileUri(file, container).AbsoluteUri;
        }

        public async Task UploadFile(string filepath, string filename, BlobContainers container, bool overwrite = true)
        {
            if (filepath is null)
            {
                throw new ArgumentNullException(nameof(filepath));
            }

            if (string.IsNullOrWhiteSpace(filename))
            {
                filename = Path.GetFileName(filepath);
            }

            using (var data = File.OpenRead(filepath))
            {
                await UploadFile(data, filename, container, overwrite);
            }
        }

        public async Task UploadFile(Stream stream, string filename, BlobContainers container, bool overwrite = true)
        {
            if (stream is null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            var client = GetClient(filename, container);
            await client.UploadAsync(stream, overwrite);
        }

        public async Task DeleteFile(string filename, BlobContainers container)
        {
            var client = GetClient(filename, container);
            await client.DeleteIfExistsAsync(Azure.Storage.Blobs.Models.DeleteSnapshotsOption.IncludeSnapshots);
        }

        public async Task CopyFile(string filename, BlobContainers sourcecontainer, BlobContainers targetcontainer, string newfilename = null, bool deleteOriginal = true)
        {
            var sourceclient = GetClient(filename, sourcecontainer);

            string targetfilename = newfilename ?? filename;
            var targetclient = GetClient(targetfilename, targetcontainer);

            await targetclient.StartCopyFromUriAsync(sourceclient.Uri);
            if (deleteOriginal)
            {
                await sourceclient.DeleteAsync(Azure.Storage.Blobs.Models.DeleteSnapshotsOption.IncludeSnapshots);
            }
        }

        public Uri GetFileUri(FileAttachment fa, BlobContainers container)
        {
            return GetFileUri(fa?.FileName, container);
        }

        /// <summary>
        /// will not throw but returns placeholder
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="container"></param>
        /// <returns></returns>
        public Uri GetFileUri(string filename, BlobContainers container, DateTimeOffset expires = default)
        {
            if (string.IsNullOrWhiteSpace(filename))
            {
                return new Uri("https://www.mecamgroup.com/noimageplaceholder.jpg");
            }

            if (expires == default)
            {
                expires = DateTimeOffset.UtcNow.AddMinutes(10);
            }

            try
            {
                var mime = MimeUtility.GetMimeMapping(filename);
                var client = GetClient(filename, container);
                client.SetHttpHeaders(new BlobHttpHeaders { ContentType = mime });
                return client.GenerateSasUri(Azure.Storage.Sas.BlobSasPermissions.Read, expires);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                return new Uri("https://www.mecamgroup.com/noimageplaceholder.jpg");
            }
        }

        private BlobClient GetClient(string filename, BlobContainers container)
        {
            if (string.IsNullOrWhiteSpace(filename))
            {
                return null;
            }

            var blobcontainer = blobServiceClient.GetBlobContainerClient(container.StringValue());

            //var validationOptions = new DownloadTransferValidationOptions
            //{
            //    AutoValidateChecksum = true,
            //    ChecksumAlgorithm = StorageChecksumAlgorithm.Auto
            //};

            //BlobDownloadToOptions downloadOptions = new BlobDownloadToOptions()
            //{
            //    TransferValidation = validationOptions
            //};

            var client = blobcontainer.GetBlobClient(filename);
            return client;
        }

        public async Task DeleteAttachment(FileAttachment attachment, BlobContainers container)
        {
            await DeleteFile(attachment.FileName, container);
        }

        public async Task DeleteAttachments(IEnumerable<FileAttachment> attachments, BlobContainers container)
        {
            foreach (var file in attachments)
            {
                await DeleteFile(file.FileName, container);
            }
        }

        /// <summary>
        /// downloads, auto-orients image, overwrites existing image
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="blobcontainer"></param>
        /// <returns></returns>
        public async Task CorrectImageOrientation(string filename, BlobContainers container)
        {
            var img = await GetFileByteArray(filename, container);
            using (MemoryStream outStream = new())
            {
                using (SixLabors.ImageSharp.Image image = SixLabors.ImageSharp.Image.Load(img))
                {
                    image.Mutate(x => x.AutoOrient());
                    await image.SaveAsJpegAsync(outStream);
                }
                await UploadFile(outStream, filename, container);
            }
        }
    }
}
