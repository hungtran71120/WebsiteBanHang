using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using ShopeeClone.Application.Categories.Dtos;
using ShopeeClone.Application.FlashSales.Dtos;
using ShopeeClone.Application.Products.Dtos;

namespace ShopeeClone.API.IntegrationTests;

[Collection(IntegrationTestCollection.Name)]
public class FlashSalesControllerTests
{
    private readonly HttpClient _client;

    public FlashSalesControllerTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetActive_IsPublic_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/flash-sales/active");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetPaged_AsCustomer_ReturnsForbidden()
    {
        var customerToken = await TestAuthHelper.RegisterAndGetCustomerAccessTokenAsync(_client);

        var response = await SendAuthorizedAsync(HttpMethod.Get, "/api/flash-sales?page=1&pageSize=10", customerToken, null);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Create_AsAdmin_ThenGetPaged_ReturnsCreatedFlashSale()
    {
        var adminToken = await TestAuthHelper.GetAdminAccessTokenAsync(_client);
        var name = $"Flash Sale {Guid.NewGuid():N}";

        var createResponse = await SendAuthorizedAsync(HttpMethod.Post, "/api/flash-sales", adminToken,
            new CreateFlashSaleRequest { Name = name, StartsAt = DateTime.UtcNow, EndsAt = DateTime.UtcNow.AddHours(2), IsActive = true });
        createResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var pagedResponse = await SendAuthorizedAsync(HttpMethod.Get, "/api/flash-sales?page=1&pageSize=50", adminToken, null);
        var page = await pagedResponse.Content.ReadFromJsonAsync<PagedResultDto<FlashSaleDto>>();

        page!.Items.Should().Contain(f => f.Name == name);
    }

    [Fact]
    public async Task Create_EndsBeforeStarts_ReturnsBadRequest()
    {
        var adminToken = await TestAuthHelper.GetAdminAccessTokenAsync(_client);

        var response = await SendAuthorizedAsync(HttpMethod.Post, "/api/flash-sales", adminToken,
            new CreateFlashSaleRequest { Name = "Bad", StartsAt = DateTime.UtcNow.AddHours(2), EndsAt = DateTime.UtcNow, IsActive = true });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task AddItem_AsAdmin_DecoratesProductDetailWithFlashSalePrice()
    {
        var adminToken = await TestAuthHelper.GetAdminAccessTokenAsync(_client);
        var category = await CreateCategoryAsync(adminToken);
        var product = await CreateProductAsync(adminToken, category.Id, "Flash Sale Test Product", 200m, 10);

        var createResponse = await SendAuthorizedAsync(HttpMethod.Post, "/api/flash-sales", adminToken,
            new CreateFlashSaleRequest { Name = $"Active Sale {Guid.NewGuid():N}", StartsAt = DateTime.UtcNow.AddHours(-1), EndsAt = DateTime.UtcNow.AddHours(1), IsActive = true });
        var flashSale = await createResponse.Content.ReadFromJsonAsync<FlashSaleDto>();

        var addItemResponse = await SendAuthorizedAsync(HttpMethod.Post, $"/api/flash-sales/{flashSale!.Id}/items", adminToken,
            new AddFlashSaleItemRequest { ProductId = product.Id, SalePrice = 150m, QuantityLimit = 5 });
        addItemResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var detailResponse = await SendAuthorizedAsync(HttpMethod.Get, $"/api/flash-sales/{flashSale.Id}", adminToken, null);
        var detail = await detailResponse.Content.ReadFromJsonAsync<FlashSaleDto>();
        detail!.Items.Should().Contain(i => i.ProductId == product.Id && i.SalePrice == 150m);

        var productResponse = await _client.GetAsync($"/api/products/{product.Id}");
        var decoratedProduct = await productResponse.Content.ReadFromJsonAsync<ProductDto>();
        decoratedProduct!.FlashSalePrice.Should().Be(150m);
        decoratedProduct.FlashSaleQuantityRemaining.Should().Be(5);
    }

    [Fact]
    public async Task AddItem_SalePriceNotLowerThanOriginal_ReturnsBadRequest()
    {
        var adminToken = await TestAuthHelper.GetAdminAccessTokenAsync(_client);
        var category = await CreateCategoryAsync(adminToken);
        var product = await CreateProductAsync(adminToken, category.Id, "Flash Sale Bad Price Product", 100m, 10);

        var createResponse = await SendAuthorizedAsync(HttpMethod.Post, "/api/flash-sales", adminToken,
            new CreateFlashSaleRequest { Name = $"Sale {Guid.NewGuid():N}", StartsAt = DateTime.UtcNow, EndsAt = DateTime.UtcNow.AddHours(2), IsActive = true });
        var flashSale = await createResponse.Content.ReadFromJsonAsync<FlashSaleDto>();

        var response = await SendAuthorizedAsync(HttpMethod.Post, $"/api/flash-sales/{flashSale!.Id}/items", adminToken,
            new AddFlashSaleItemRequest { ProductId = product.Id, SalePrice = 100m, QuantityLimit = 5 });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateItem_AsAdmin_UpdatesSalePriceAndQuantityLimit()
    {
        var adminToken = await TestAuthHelper.GetAdminAccessTokenAsync(_client);
        var category = await CreateCategoryAsync(adminToken);
        var product = await CreateProductAsync(adminToken, category.Id, "Flash Sale Update Product", 200m, 10);

        var createResponse = await SendAuthorizedAsync(HttpMethod.Post, "/api/flash-sales", adminToken,
            new CreateFlashSaleRequest { Name = $"Sale {Guid.NewGuid():N}", StartsAt = DateTime.UtcNow, EndsAt = DateTime.UtcNow.AddHours(2), IsActive = true });
        var flashSale = await createResponse.Content.ReadFromJsonAsync<FlashSaleDto>();

        var addItemResponse = await SendAuthorizedAsync(HttpMethod.Post, $"/api/flash-sales/{flashSale!.Id}/items", adminToken,
            new AddFlashSaleItemRequest { ProductId = product.Id, SalePrice = 150m, QuantityLimit = 5 });
        var withItem = await addItemResponse.Content.ReadFromJsonAsync<FlashSaleDto>();
        var item = withItem!.Items.Single();

        var updateResponse = await SendAuthorizedAsync(HttpMethod.Put, $"/api/flash-sales/{flashSale.Id}/items/{item.Id}", adminToken,
            new UpdateFlashSaleItemRequest { SalePrice = 120m, QuantityLimit = 8 });
        var updated = await updateResponse.Content.ReadFromJsonAsync<FlashSaleDto>();

        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        updated!.Items.Single().SalePrice.Should().Be(120m);
        updated.Items.Single().QuantityLimit.Should().Be(8);
    }

    [Fact]
    public async Task DeleteItem_AsAdmin_RemovesItem()
    {
        var adminToken = await TestAuthHelper.GetAdminAccessTokenAsync(_client);
        var category = await CreateCategoryAsync(adminToken);
        var product = await CreateProductAsync(adminToken, category.Id, "Flash Sale Delete Item Product", 200m, 10);

        var createResponse = await SendAuthorizedAsync(HttpMethod.Post, "/api/flash-sales", adminToken,
            new CreateFlashSaleRequest { Name = $"Sale {Guid.NewGuid():N}", StartsAt = DateTime.UtcNow, EndsAt = DateTime.UtcNow.AddHours(2), IsActive = true });
        var flashSale = await createResponse.Content.ReadFromJsonAsync<FlashSaleDto>();

        var addItemResponse = await SendAuthorizedAsync(HttpMethod.Post, $"/api/flash-sales/{flashSale!.Id}/items", adminToken,
            new AddFlashSaleItemRequest { ProductId = product.Id, SalePrice = 150m, QuantityLimit = 5 });
        var withItem = await addItemResponse.Content.ReadFromJsonAsync<FlashSaleDto>();
        var item = withItem!.Items.Single();

        var deleteResponse = await SendAuthorizedAsync(HttpMethod.Delete, $"/api/flash-sales/{flashSale.Id}/items/{item.Id}", adminToken, null);
        var afterDelete = await deleteResponse.Content.ReadFromJsonAsync<FlashSaleDto>();

        deleteResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        afterDelete!.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task Delete_AsAdmin_RemovesFlashSale()
    {
        var adminToken = await TestAuthHelper.GetAdminAccessTokenAsync(_client);
        var createResponse = await SendAuthorizedAsync(HttpMethod.Post, "/api/flash-sales", adminToken,
            new CreateFlashSaleRequest { Name = $"Sale {Guid.NewGuid():N}", StartsAt = DateTime.UtcNow, EndsAt = DateTime.UtcNow.AddHours(2), IsActive = true });
        var flashSale = await createResponse.Content.ReadFromJsonAsync<FlashSaleDto>();

        var deleteResponse = await SendAuthorizedAsync(HttpMethod.Delete, $"/api/flash-sales/{flashSale!.Id}", adminToken, null);

        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
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
