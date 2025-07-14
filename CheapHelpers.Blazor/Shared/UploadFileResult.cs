namespace CheapHelpers.Blazor.Shared
{
    public class UploadFileResult
    {
        public bool HasException
        {
            get => Exception != null;
        }
        public string FileName { get; set; }
        public string UploadPath { get; set; }
        public string BlobContainer { get; set; }
        public string FullFilePath
        {
            get => Path.Combine(UploadPath, FileName);
        }
        public Exception Exception { get; set; }
    }
}
