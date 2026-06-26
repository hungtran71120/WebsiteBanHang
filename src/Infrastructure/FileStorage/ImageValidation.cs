namespace HungStore.Infrastructure.FileStorage;

internal static class ImageValidation
{
    public const long MaxFileSizeBytes = 5 * 1024 * 1024;

    private static readonly Dictionary<string, string> AllowedContentTypes = new()
    {
        ["image/jpeg"] = ".jpg",
        ["image/png"] = ".png",
        ["image/webp"] = ".webp"
    };

    public static string ResolveExtension(Stream content, string contentType)
    {
        if (!AllowedContentTypes.TryGetValue(contentType, out var extension))
        {
            throw new ArgumentException("Chỉ hỗ trợ ảnh JPEG, PNG hoặc WEBP.");
        }

        if (content.Length > MaxFileSizeBytes)
        {
            throw new ArgumentException("Kích thước ảnh không được vượt quá 5MB.");
        }

        return extension;
    }
}
