using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Components.Forms;

namespace AzureTranslator.Services;

public interface IAzureBlobService
{
    /// <summary>
    /// Gets a blob client for a specific file in a container.
    /// </summary>
    Task<BlobClient> GetBlob(string containerName, string fileName);

    /// <summary>
    /// Generates a SAS URI for a blob.
    /// </summary>
    Task<Uri> GenerateBlobSas(string containerName, string blobName, string fileName);

    /// <summary>
    /// Gets a container or if the container doesn't exist, one will be created.
    /// </summary>
    Task<BlobContainerClient> GetOrCreateContainer(string containerName);

    /// <summary>
    /// Uploads a file into a container under the folder name and returns the full blob path.
    /// </summary>
    Task<string> UploadFile(IBrowserFile file, string containerName, string folderName);

    /// <summary>
    /// Generates a SAS URI for the entire container.
    /// </summary>
    Task<Uri> GenerateContainerSasUri(string containerName, bool writeAccess = false);
}
