using HungStore.Application.Common.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Supabase;

namespace HungStore.Infrastructure.FileStorage
{
    public class SupabaseStorageService : IFileStorageService
    {
        private readonly Client _client;

        public SupabaseStorageService(IConfiguration configuration)
        {
            var url = configuration["Supabase:Url"];
            var key = configuration["Supabase:ServiceKey"];

            _client = new Client(url, key);
        }

        public Task<string> SaveBannerImageAsync(Stream content, string contentType)
        {
            return SaveImageAsync(content, contentType, "banners");
        }

        public Task<string> SaveProductImageAsync(Stream content, string contentType)
        {
            return SaveImageAsync(content, contentType, "products");
        }

        private async Task<string> SaveImageAsync(Stream content, string contentType, string subfolder)
        {
            await _client.InitializeAsync();

            var extension = ImageValidation.ResolveExtension(content, contentType);

            var fileName = $"{Guid.NewGuid()}{extension}";

            using var memoryStream = new MemoryStream();

            await content.CopyToAsync(memoryStream);

            var bytes = memoryStream.ToArray();

            var bucket = _client.Storage.From(subfolder);

            await bucket.Upload(
                bytes,
                fileName
            );

            var publicUrl = bucket.GetPublicUrl(fileName);

            return publicUrl;
        }
    }
}
