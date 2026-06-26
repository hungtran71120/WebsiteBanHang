using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using HungStore.Application.Products.Interfaces;

namespace HungStore.Infrastructure.FileStorage;

public class AzureBlobFileStorageService : IFileStorageService
{
    private readonly BlobContainerClient _containerClient;

    public AzureBlobFileStorageService(string connectionString, string containerName)
    {
        _containerClient = new BlobContainerClient(connectionString, containerName);
        _containerClient.CreateIfNotExists(PublicAccessType.Blob);
    }

    public Task<string> SaveProductImageAsync(Stream content, string contentType)
    {
        return SaveImageAsync(content, contentType, "products");
    }

    public Task<string> SaveBannerImageAsync(Stream content, string contentType)
    {
        return SaveImageAsync(content, contentType, "banners");
    }

    private async Task<string> SaveImageAsync(Stream content, string contentType, string subfolder)
    {
        var extension = ImageValidation.ResolveExtension(content, contentType);

        var blobName = $"{subfolder}/{Guid.NewGuid()}{extension}";
        var blobClient = _containerClient.GetBlobClient(blobName);

        content.Position = 0;
        await blobClient.UploadAsync(content, overwrite: true);

        return blobClient.Uri.ToString();
    }
}
