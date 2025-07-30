using Azure.Storage.Blobs;
using Azure.Storage.Sas;
using Microsoft.AspNetCore.Components.Forms;

namespace AzureTranslator.Services;

public class AzureBlobService(IConfiguration config, ILogger<AzureBlobService> logger) : IAzureBlobService
{
    private readonly BlobServiceClient _client = new(config.GetValue<string>(
        "Azure:StorageAccountConnectionString"));
    
    private const long OneGb = 1024L * 1024L * 1024L;
    
    public async Task<BlobContainerClient> GetOrCreateContainer(string containerName)
    {
        try
        {
            // Get a container client
            var containerClient = _client.GetBlobContainerClient(containerName);
            
            // Create the container if it doesn't already exist
            await containerClient.CreateIfNotExistsAsync();
            
            logger.LogInformation("Container {ContainerName} accessed successfully", containerName);
            return containerClient;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error accessing or creating container {ContainerName}", containerName);
            throw;
        }
    }

    public async Task<string> UploadFile(IBrowserFile file, string containerName, string folderName)
    {
        var extension = Path.GetExtension(file.Name);
        // include the prefix in the blob’s “folder”
        var blobName = $"{folderName}/{Guid.NewGuid()}{extension}";
        var containerClient = await GetOrCreateContainer(containerName);
        var blobClient = containerClient.GetBlobClient(blobName);
        await blobClient.UploadAsync(file.OpenReadStream(OneGb), overwrite: true);
        return blobName;
    }

    public async Task<Uri> GenerateContainerSasUri(string containerName, bool writeAccess = false)
    {
        var containerClient = await GetOrCreateContainer(containerName);
        var builder = new BlobSasBuilder
        {
            BlobContainerName = containerName,
            Resource = "c",
            ExpiresOn = DateTimeOffset.UtcNow.AddHours(1)
        };

        var permissions = writeAccess ? BlobSasPermissions.Write | BlobSasPermissions.List : BlobSasPermissions.Read | BlobSasPermissions.List;

        builder.SetPermissions(permissions);
        return containerClient.GenerateSasUri(builder);
    }

    public async Task<Uri?> GenerateBlobSas(string containerName, string blobName, string fileName)
    {
        try
        {
            // Get a container (creating it if needed)
            var containerClient = await GetOrCreateContainer(containerName);
            
            // Get a reference to a blob
            var blobClient = containerClient.GetBlobClient(blobName);
            
            // Check if blob exists
            var exists = await blobClient.ExistsAsync();
            
            if (!exists)
            {
                logger.LogWarning("Blob {FileName} not found in container {ContainerName}", 
                    blobName, containerName);
                return null;
            }


            var sasBuilder = new BlobSasBuilder
            {
                BlobContainerName = containerName,
                BlobName = blobName,
                ExpiresOn = DateTimeOffset.UtcNow.Add(TimeSpan.FromMinutes(120)),
                ContentDisposition = $"attachment; filename=\"{fileName}\""
            };

            sasBuilder.SetPermissions(BlobSasPermissions.Read);

            try
            {
                var sasUri = blobClient.GenerateSasUri(sasBuilder);
                logger.LogInformation("Successfully retrieved blob {FileName} from container {ContainerName}",
                    blobName, containerName);

                return sasUri;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unable to generate Sas URI for {Filename}", fileName);
                return null;
            }

        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving blob {FileName} from container {ContainerName}", 
                blobName, containerName);
            throw;
        }
    }

    public async Task<BlobClient?> GetBlob(string containerName, string fileName)
    {
        try
        {
            // Get a container (creating it if needed)
            var containerClient = await GetOrCreateContainer(containerName);

            // Get a reference to a blob
            var blobClient = containerClient.GetBlobClient(fileName);

            // Check if blob exists
            var exists = await blobClient.ExistsAsync();

            if (!exists)
            {
                logger.LogWarning("Blob {FileName} not found in container {ContainerName}",
                    fileName, containerName);
                return null;
            }

            logger.LogInformation("Successfully retrieved blob {FileName} from container {ContainerName}",
                fileName, containerName);

            // Return the blob client
            return blobClient;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving blob {FileName} from container {ContainerName}",
                fileName, containerName);
            throw;
        }
    }
}