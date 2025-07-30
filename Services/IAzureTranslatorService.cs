namespace AzureTranslator.Services;

public interface IAzureTranslatorService
{
    /// <summary>
    /// Translates documents in the source container to the target language and puts the translated documents in the target container.
    /// </summary>
    Task TranslateDocs(string sourceContainerName,
        string targetContainerName,
        string targetLanguage,
        string targetFolderName,
        CancellationToken cancellationToken = default);
}
