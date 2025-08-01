using Azure;
using Azure.AI.Translation.Document;

namespace AzureTranslator.Services;

public class AzureTranslatorService(IConfiguration config,
    IAzureBlobService blobService,
    ILogger<AzureTranslatorService> logger) : IAzureTranslatorService
{
    private readonly DocumentTranslationClient _client = new(
        config.GetValue<Uri>("Azure:TranslatorEndpoint"),
        new AzureKeyCredential(config.GetValue<string>("Azure:TranslatorKey")!));

    public async Task TranslateDocs(string sourceContainerName,
        string targetContainerName,
        string targetFolderName,
        string targetLanguage = "en",
        CancellationToken cancellationToken = default)
    {
        try
        {
            await blobService.GetOrCreateContainer(targetContainerName);

            var sourceContainerUri = await blobService.GenerateContainerSasUri(sourceContainerName);
            var targetContainerUri = await blobService.GenerateContainerSasUri(targetContainerName, writeAccess: true);

            // Create input with prefix filtering for source
            var source = new TranslationSource(sourceContainerUri)
            {
                Prefix = $"{targetFolderName}/",
            };

            var target = new TranslationTarget(targetContainerUri, targetLanguage);
            var input = new DocumentTranslationInput(source, [target]);

            var operation = await _client.StartTranslationAsync(input, cancellationToken);

            await operation.UpdateStatusAsync(cancellationToken);
            await operation.WaitForCompletionAsync(cancellationToken);

            _ = operation.HasCompleted ? true : throw new InvalidOperationException("Translation operation did not complete successfully.");

            logger.LogInformation("Document translation completed successfully for user prefix {UserPrefix}", targetFolderName);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error during document translation for user prefix {UserPrefix}", targetFolderName);
            throw;
        }
    }
}