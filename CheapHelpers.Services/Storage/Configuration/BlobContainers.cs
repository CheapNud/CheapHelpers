using CheapHelpers.Helpers.Enums;

namespace CheapHelpers.Services.Storage.Configuration
{
    public enum BlobContainers
    {
        // BlobService resolves the container by StringValue(), which is null without this attribute
        [StringValue("temp")]
        TempContainer,
    }
}
