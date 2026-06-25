namespace ShopeeClone.Application.Products.Interfaces;

public interface IFileStorageService
{
    Task<string> SaveProductImageAsync(Stream content, string contentType);
}
