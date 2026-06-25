using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using ShopeeClone.Application.Products.Dtos;

namespace ShopeeClone.API.IntegrationTests;

[Collection(IntegrationTestCollection.Name)]
public class RecommendationsControllerTests
{
    private readonly HttpClient _client;

    public RecommendationsControllerTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetRecommendations_NotAuthenticated_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/recommendations");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var recommendations = await response.Content.ReadFromJsonAsync<List<ProductDto>>();
        recommendations.Should().NotBeNull();
    }

    [Fact]
    public async Task GetRecommendations_Authenticated_ReturnsOk()
    {
        var customerToken = await TestAuthHelper.RegisterAndGetCustomerAccessTokenAsync(_client);

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/recommendations");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", customerToken);

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var recommendations = await response.Content.ReadFromJsonAsync<List<ProductDto>>();
        recommendations.Should().NotBeNull();
    }
}
