using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using ShopeeClone.Application.Auth.Dtos;

namespace ShopeeClone.API.IntegrationTests;

[Collection(IntegrationTestCollection.Name)]
public class AuthControllerTests
{
    private readonly HttpClient _client;

    public AuthControllerTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Register_Then_Login_ThenGetProfile_Succeeds()
    {
        var email = $"user-{Guid.NewGuid():N}@test.com";
        var registerRequest = new RegisterRequest
        {
            Email = email,
            Password = "Password123",
            FullName = "Integration Test User",
            PhoneNumber = "0900000000",
            Address = "123 Test Street"
        };

        var registerResponse = await _client.PostAsJsonAsync("/api/auth/register", registerRequest);
        registerResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest
        {
            Email = email,
            Password = "Password123"
        });
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var auth = await loginResponse.Content.ReadFromJsonAsync<AuthResponseDto>();
        auth.Should().NotBeNull();
        auth!.User.Role.Should().Be("Customer");

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/users/me");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);

        var meResponse = await _client.SendAsync(request);
        meResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var profile = await meResponse.Content.ReadFromJsonAsync<UserDto>();
        profile!.Email.Should().Be(email);
    }

    [Fact]
    public async Task GetMe_WithoutToken_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync("/api/users/me");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_WithWrongPassword_ReturnsUnauthorized()
    {
        var email = $"user-{Guid.NewGuid():N}@test.com";
        await _client.PostAsJsonAsync("/api/auth/register", new RegisterRequest
        {
            Email = email,
            Password = "Password123",
            FullName = "Wrong Password User"
        });

        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest
        {
            Email = email,
            Password = "WrongPassword"
        });

        loginResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task RefreshToken_RotatesAndOldTokenBecomesInvalid()
    {
        var email = $"user-{Guid.NewGuid():N}@test.com";
        await _client.PostAsJsonAsync("/api/auth/register", new RegisterRequest
        {
            Email = email,
            Password = "Password123",
            FullName = "Refresh Token User"
        });

        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest
        {
            Email = email,
            Password = "Password123"
        });
        var auth = await loginResponse.Content.ReadFromJsonAsync<AuthResponseDto>();

        var refreshResponse = await _client.PostAsJsonAsync("/api/auth/refresh-token", new RefreshTokenRequest
        {
            RefreshToken = auth!.RefreshToken
        });
        refreshResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var reuseResponse = await _client.PostAsJsonAsync("/api/auth/refresh-token", new RefreshTokenRequest
        {
            RefreshToken = auth.RefreshToken
        });
        reuseResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
