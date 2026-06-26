using HungStore.Application.Products.Interfaces;

namespace HungStore.Infrastructure.FileStorage;

public class LocalFileStorageService : IFileStorageService
{
    private readonly string _webRootPath;

    public LocalFileStorageService(string webRootPath)
    {
        _webRootPath = webRootPath;
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

        var uploadsFolder = Path.Combine(_webRootPath, "uploads", subfolder);
        Directory.CreateDirectory(uploadsFolder);

        var uniqueFileName = $"{Guid.NewGuid()}{extension}";
        var filePath = Path.Combine(uploadsFolder, uniqueFileName);

        await using var fileStream = new FileStream(filePath, FileMode.Create);
        await content.CopyToAsync(fileStream);

        return $"/uploads/{subfolder}/{uniqueFileName}";
    }
}
