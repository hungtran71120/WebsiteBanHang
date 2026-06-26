using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using HungStore.Application.Auth.Dtos;
using HungStore.Application.Users.Dtos;

namespace HungStore.API.IntegrationTests;

[Collection(IntegrationTestCollection.Name)]
public class UsersControllerTests
{
    private readonly HttpClient _client;

    public UsersControllerTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetAll_AsCustomer_ReturnsForbidden()
    {
        var customerToken = await TestAuthHelper.RegisterAndGetCustomerAccessTokenAsync(_client);

        var response = await SendAuthorizedAsync(HttpMethod.Get, "/api/users", customerToken, null);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetAll_AsAdmin_ReturnsRegisteredCustomer()
    {
        var customerToken = await TestAuthHelper.RegisterAndGetCustomerAccessTokenAsync(_client);
        var customer = await GetMeAsync(customerToken);
        var adminToken = await TestAuthHelper.GetAdminAccessTokenAsync(_client);

        var response = await SendAuthorizedAsync(HttpMethod.Get, $"/api/users?keyword={customer.Email}", adminToken, null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var page = await response.Content.ReadFromJsonAsync<PagedResultDto<UserDto>>();
        page!.Items.Should().Contain(u => u.Id == customer.Id);
    }

    [Fact]
    public async Task SetLockout_AsAdmin_LocksTargetUserAndBlocksFutureLogin()
    {
        var email = $"locktest-{Guid.NewGuid():N}@test.com";
        const string password = "Password123";
        await _client.PostAsJsonAsync("/api/auth/register", new RegisterRequest { Email = email, Password = password, FullName = "Lock Test" });
        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest { Email = email, Password = password });
        var auth = await loginResponse.Content.ReadFromJsonAsync<AuthResponseDto>();
        var customer = await GetMeAsync(auth!.AccessToken);
        var adminToken = await TestAuthHelper.GetAdminAccessTokenAsync(_client);

        var response = await SendAuthorizedAsync(HttpMethod.Put, $"/api/users/{customer.Id}/lockout", adminToken,
            new SetUserLockoutRequest { Locked = true });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await response.Content.ReadFromJsonAsync<UserDto>();
        updated!.IsLocked.Should().BeTrue();

        var relogin = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest { Email = email, Password = password });
        relogin.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task SetLockout_AdminLocksOwnAccount_ReturnsBadRequest()
    {
        var adminToken = await TestAuthHelper.GetAdminAccessTokenAsync(_client);
        var admin = await GetMeAsync(adminToken);

        var response = await SendAuthorizedAsync(HttpMethod.Put, $"/api/users/{admin.Id}/lockout", adminToken,
            new SetUserLockoutRequest { Locked = true });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task SetLockout_AsCustomer_ReturnsForbidden()
    {
        var customerToken = await TestAuthHelper.RegisterAndGetCustomerAccessTokenAsync(_client);
        var customer = await GetMeAsync(customerToken);

        var response = await SendAuthorizedAsync(HttpMethod.Put, $"/api/users/{customer.Id}/lockout", customerToken,
            new SetUserLockoutRequest { Locked = true });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    private async Task<UserDto> GetMeAsync(string token)
    {
        var response = await SendAuthorizedAsync(HttpMethod.Get, "/api/users/me", token, null);
        return (await response.Content.ReadFromJsonAsync<UserDto>())!;
    }

    private async Task<HttpResponseMessage> SendAuthorizedAsync(HttpMethod method, string url, string token, object? body)
    {
        using var request = new HttpRequestMessage(method, url);
        if (body is not null)
        {
            request.Content = JsonContent.Create(body);
        }
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return await _client.SendAsync(request);
    }
}
