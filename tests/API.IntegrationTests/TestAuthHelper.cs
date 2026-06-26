using System.Net.Http.Json;
using HungStore.Application.Auth.Dtos;

namespace HungStore.API.IntegrationTests;

public static class TestAuthHelper
{
    public static async Task<string> GetAdminAccessTokenAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest
        {
            Email = "admin@shopeeclone.dev",
            Password = "DEV-ONLY-Admin@123"
        });

        var auth = await response.Content.ReadFromJsonAsync<AuthResponseDto>();
        return auth!.AccessToken;
    }

    public static async Task<string> RegisterAndGetCustomerAccessTokenAsync(HttpClient client)
    {
        var email = $"customer-{Guid.NewGuid():N}@test.com";
        await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest
        {
            Email = email,
            Password = "Password123",
            FullName = "Test Customer"
        });

        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest
        {
            Email = email,
            Password = "Password123"
        });

        var auth = await response.Content.ReadFromJsonAsync<AuthResponseDto>();
        return auth!.AccessToken;
    }
}
