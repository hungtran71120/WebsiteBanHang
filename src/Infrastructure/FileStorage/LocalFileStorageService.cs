using ShopeeClone.Application.Products.Interfaces;

namespace ShopeeClone.Infrastructure.FileStorage;

public class LocalFileStorageService : IFileStorageService
{
    private const long MaxFileSizeBytes = 5 * 1024 * 1024;

    private static readonly Dictionary<string, string> AllowedContentTypes = new()
    {
        ["image/jpeg"] = ".jpg",
        ["image/png"] = ".png",
        ["image/webp"] = ".webp"
    };

    private readonly string _webRootPath;

    public LocalFileStorageService(string webRootPath)
    {
        _webRootPath = webRootPath;
    }

    public async Task<string> SaveProductImageAsync(Stream content, string contentType)
    {
        if (!AllowedContentTypes.TryGetValue(contentType, out var extension))
        {
            throw new ArgumentException("Chỉ hỗ trợ ảnh JPEG, PNG hoặc WEBP.");
        }

        if (content.Length > MaxFileSizeBytes)
        {
            throw new ArgumentException("Kích thước ảnh không được vượt quá 5MB.");
        }

        var uploadsFolder = Path.Combine(_webRootPath, "uploads", "products");
        Directory.CreateDirectory(uploadsFolder);

        var uniqueFileName = $"{Guid.NewGuid()}{extension}";
        var filePath = Path.Combine(uploadsFolder, uniqueFileName);

        await using var fileStream = new FileStream(filePath, FileMode.Create);
        await content.CopyToAsync(fileStream);

        return $"/uploads/products/{uniqueFileName}";
    }
}
