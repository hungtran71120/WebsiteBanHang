using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using HungStore.Application.Categories.Dtos;
using HungStore.Application.Products.Dtos;
using HungStore.Application.Wishlist.Dtos;

namespace HungStore.API.IntegrationTests;

[Collection(IntegrationTestCollection.Name)]
public class WishlistControllerTests
{
    private readonly HttpClient _client;

    public WishlistControllerTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetWishlist_NotAuthenticated_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync("/api/wishlist");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task AddItem_ThenGetWishlist_ReturnsProduct()
    {
        var customerToken = await TestAuthHelper.RegisterAndGetCustomerAccessTokenAsync(_client);
        var adminToken = await TestAuthHelper.GetAdminAccessTokenAsync(_client);
        var category = await CreateCategoryAsync(adminToken);
        var product = await CreateProductAsync(adminToken, category.Id, "Wishlist Test Product", 150m, 10);

        var addResponse = await SendAuthorizedAsync(HttpMethod.Post, "/api/wishlist/items", customerToken,
            new AddWishlistItemRequest { ProductId = product.Id });
        addResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var getResponse = await SendAuthorizedAsync(HttpMethod.Get, "/api/wishlist", customerToken, null);
        var wishlist = await getResponse.Content.ReadFromJsonAsync<WishlistDto>();

        wishlist!.Items.Should().ContainSingle(i => i.ProductId == product.Id && i.ProductName == "Wishlist Test Product");
    }

    [Fact]
    public async Task AddItem_SameProductTwice_DoesNotDuplicate()
    {
        var customerToken = await TestAuthHelper.RegisterAndGetCustomerAccessTokenAsync(_client);
        var adminToken = await TestAuthHelper.GetAdminAccessTokenAsync(_client);
        var category = await CreateCategoryAsync(adminToken);
        var product = await CreateProductAsync(adminToken, category.Id, "Duplicate Wishlist Product", 150m, 10);

        await SendAuthorizedAsync(HttpMethod.Post, "/api/wishlist/items", customerToken,
            new AddWishlistItemRequest { ProductId = product.Id });
        var secondAdd = await SendAuthorizedAsync(HttpMethod.Post, "/api/wishlist/items", customerToken,
            new AddWishlistItemRequest { ProductId = product.Id });
        var wishlist = await secondAdd.Content.ReadFromJsonAsync<WishlistDto>();

        wishlist!.Items.Should().ContainSingle(i => i.ProductId == product.Id);
    }

    [Fact]
    public async Task RemoveItem_RemovesFromWishlist()
    {
        var customerToken = await TestAuthHelper.RegisterAndGetCustomerAccessTokenAsync(_client);
        var adminToken = await TestAuthHelper.GetAdminAccessTokenAsync(_client);
        var category = await CreateCategoryAsync(adminToken);
        var product = await CreateProductAsync(adminToken, category.Id, "Removable Wishlist Product", 150m, 10);

        await SendAuthorizedAsync(HttpMethod.Post, "/api/wishlist/items", customerToken,
            new AddWishlistItemRequest { ProductId = product.Id });

        var removeResponse = await SendAuthorizedAsync(HttpMethod.Delete, $"/api/wishlist/items/{product.Id}", customerToken, null);
        removeResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var wishlist = await removeResponse.Content.ReadFromJsonAsync<WishlistDto>();
        wishlist!.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task RemoveItem_NotInWishlist_ReturnsNotFound()
    {
        var customerToken = await TestAuthHelper.RegisterAndGetCustomerAccessTokenAsync(_client);

        var response = await SendAuthorizedAsync(HttpMethod.Delete, $"/api/wishlist/items/{Guid.NewGuid()}", customerToken, null);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
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

    private async Task<CategoryDto> CreateCategoryAsync(string adminToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/categories")
        {
            Content = JsonContent.Create(new CreateCategoryRequest { Name = $"Category-{Guid.NewGuid():N}" })
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        var response = await _client.SendAsync(request);
        return (await response.Content.ReadFromJsonAsync<CategoryDto>())!;
    }

    private async Task<ProductDto> CreateProductAsync(string adminToken, Guid categoryId, string name, decimal price, int stock)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/products")
        {
            Content = JsonContent.Create(new CreateProductRequest
            {
                Name = name,
                Description = "Test product",
                Price = price,
                Stock = stock,
                CategoryId = categoryId
            })
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        var response = await _client.SendAsync(request);
        return (await response.Content.ReadFromJsonAsync<ProductDto>())!;
    }
}
