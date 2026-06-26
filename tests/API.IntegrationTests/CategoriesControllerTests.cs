using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using HungStore.Application.Categories.Dtos;

namespace HungStore.API.IntegrationTests;

[Collection(IntegrationTestCollection.Name)]
public class CategoriesControllerTests
{
    private readonly HttpClient _client;

    public CategoriesControllerTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Create_AsAdmin_ThenGetAll_IncludesNewCategory()
    {
        var adminToken = await TestAuthHelper.GetAdminAccessTokenAsync(_client);
        var name = $"Category-{Guid.NewGuid():N}";

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/categories")
        {
            Content = JsonContent.Create(new CreateCategoryRequest { Name = name })
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        var createResponse = await _client.SendAsync(request);
        createResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var listResponse = await _client.GetAsync("/api/categories");
        var categories = await listResponse.Content.ReadFromJsonAsync<List<CategoryDto>>();
        categories.Should().Contain(c => c.Name == name);
    }

    [Fact]
    public async Task Create_AsCustomer_ReturnsForbidden()
    {
        var customerToken = await TestAuthHelper.RegisterAndGetCustomerAccessTokenAsync(_client);

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/categories")
        {
            Content = JsonContent.Create(new CreateCategoryRequest { Name = "Should Fail" })
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", customerToken);

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Delete_CategoryWithProducts_ReturnsBadRequest()
    {
        var adminToken = await TestAuthHelper.GetAdminAccessTokenAsync(_client);

        var category = await CreateCategoryAsync(adminToken, "With Products");
        await CreateProductAsync(adminToken, category.Id, "Product In Category");

        using var deleteRequest = new HttpRequestMessage(HttpMethod.Delete, $"/api/categories/{category.Id}");
        deleteRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        var response = await _client.SendAsync(deleteRequest);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    private async Task<CategoryDto> CreateCategoryAsync(string adminToken, string name)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/categories")
        {
            Content = JsonContent.Create(new CreateCategoryRequest { Name = $"{name}-{Guid.NewGuid():N}" })
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        var response = await _client.SendAsync(request);
        return (await response.Content.ReadFromJsonAsync<CategoryDto>())!;
    }

    private async Task CreateProductAsync(string adminToken, Guid categoryId, string name)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/products")
        {
            Content = JsonContent.Create(new
            {
                Name = name,
                Description = "Test product",
                Price = 100,
                Stock = 10,
                CategoryId = categoryId
            })
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        await _client.SendAsync(request);
    }
}
