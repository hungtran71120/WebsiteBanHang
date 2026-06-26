using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using HungStore.Application.Banners.Dtos;

namespace HungStore.API.IntegrationTests;

[Collection(IntegrationTestCollection.Name)]
public class BannersControllerTests
{
    private readonly HttpClient _client;

    public BannersControllerTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Create_AsAdmin_ThenGetActive_IncludesNewBanner()
    {
        var adminToken = await TestAuthHelper.GetAdminAccessTokenAsync(_client);
        var title = $"Banner-{Guid.NewGuid():N}";

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/banners")
        {
            Content = JsonContent.Create(new CreateBannerRequest { Title = title, LinkUrl = "/products", DisplayOrder = 0, IsActive = true })
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        var createResponse = await _client.SendAsync(request);
        createResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var activeResponse = await _client.GetAsync("/api/banners/active");
        var banners = await activeResponse.Content.ReadFromJsonAsync<List<BannerDto>>();
        banners.Should().Contain(b => b.Title == title);
    }

    [Fact]
    public async Task Create_AsCustomer_ReturnsForbidden()
    {
        var customerToken = await TestAuthHelper.RegisterAndGetCustomerAccessTokenAsync(_client);

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/banners")
        {
            Content = JsonContent.Create(new CreateBannerRequest { Title = "Should Fail", LinkUrl = "/products" })
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", customerToken);

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetActive_ExcludesInactiveBanners()
    {
        var adminToken = await TestAuthHelper.GetAdminAccessTokenAsync(_client);
        var inactiveTitle = $"Inactive-{Guid.NewGuid():N}";

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/banners")
        {
            Content = JsonContent.Create(new CreateBannerRequest { Title = inactiveTitle, LinkUrl = "/products", IsActive = false })
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        await _client.SendAsync(request);

        var activeResponse = await _client.GetAsync("/api/banners/active");
        var banners = await activeResponse.Content.ReadFromJsonAsync<List<BannerDto>>();

        banners.Should().NotContain(b => b.Title == inactiveTitle);
    }

    [Fact]
    public async Task Create_WithExternalLinkUrl_ReturnsBadRequest()
    {
        var adminToken = await TestAuthHelper.GetAdminAccessTokenAsync(_client);

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/banners")
        {
            Content = JsonContent.Create(new CreateBannerRequest { Title = "Bad Link", LinkUrl = "https://evil.example.com" })
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Delete_AsAdmin_RemovesBanner()
    {
        var adminToken = await TestAuthHelper.GetAdminAccessTokenAsync(_client);
        var banner = await CreateBannerAsync(adminToken, "To Delete");

        using var deleteRequest = new HttpRequestMessage(HttpMethod.Delete, $"/api/banners/{banner.Id}");
        deleteRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        var deleteResponse = await _client.SendAsync(deleteRequest);

        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using var getRequest = new HttpRequestMessage(HttpMethod.Get, $"/api/banners/{banner.Id}");
        getRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        var getResponse = await _client.SendAsync(getRequest);

        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private async Task<BannerDto> CreateBannerAsync(string adminToken, string title)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/banners")
        {
            Content = JsonContent.Create(new CreateBannerRequest { Title = $"{title}-{Guid.NewGuid():N}", LinkUrl = "/products" })
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        var response = await _client.SendAsync(request);
        return (await response.Content.ReadFromJsonAsync<BannerDto>())!;
    }
}
