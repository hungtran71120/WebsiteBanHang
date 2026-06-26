namespace HungStore.Application.Products.Interfaces;

public interface IFileStorageService
{
    Task<string> SaveProductImageAsync(Stream content, string contentType);
    Task<string> SaveBannerImageAsync(Stream content, string contentType);
}
