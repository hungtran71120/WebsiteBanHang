using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using ShopeeClone.Application.Cart.Dtos;
using ShopeeClone.Application.Categories.Dtos;
using ShopeeClone.Application.Products.Dtos;

namespace ShopeeClone.API.IntegrationTests;

[Collection(IntegrationTestCollection.Name)]
public class CartControllerTests
{
    private readonly HttpClient _client;

    public CartControllerTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetCart_NotAuthenticated_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync("/api/cart");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task AddItem_ThenGetCart_ReturnsItemWithCorrectTotal()
    {
        var customerToken = await TestAuthHelper.RegisterAndGetCustomerAccessTokenAsync(_client);
        var adminToken = await TestAuthHelper.GetAdminAccessTokenAsync(_client);
        var category = await CreateCategoryAsync(adminToken);
        var product = await CreateProductAsync(adminToken, category.Id, "Cart Test Product", 100m, 10);

        var addResponse = await SendAuthorizedAsync(HttpMethod.Post, "/api/cart/items", customerToken,
            new AddCartItemRequest { ProductId = product.Id, Quantity = 3 });
        addResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var getResponse = await SendAuthorizedAsync(HttpMethod.Get, "/api/cart", customerToken, null);
        var cart = await getResponse.Content.ReadFromJsonAsync<CartDtoResponse>();

        cart!.Items.Should().ContainSingle(i => i.ProductId == product.Id && i.Quantity == 3);
        cart.TotalAmount.Should().Be(300m);
    }

    [Fact]
    public async Task AddItem_QuantityExceedsStock_ReturnsBadRequest()
    {
        var customerToken = await TestAuthHelper.RegisterAndGetCustomerAccessTokenAsync(_client);
        var adminToken = await TestAuthHelper.GetAdminAccessTokenAsync(_client);
        var category = await CreateCategoryAsync(adminToken);
        var product = await CreateProductAsync(adminToken, category.Id, "Low Stock Product", 100m, 2);

        var response = await SendAuthorizedAsync(HttpMethod.Post, "/api/cart/items", customerToken,
            new AddCartItemRequest { ProductId = product.Id, Quantity = 5 });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateItem_ChangesQuantity()
    {
        var customerToken = await TestAuthHelper.RegisterAndGetCustomerAccessTokenAsync(_client);
        var adminToken = await TestAuthHelper.GetAdminAccessTokenAsync(_client);
        var category = await CreateCategoryAsync(adminToken);
        var product = await CreateProductAsync(adminToken, category.Id, "Update Qty Product", 100m, 10);

        var addResponse = await SendAuthorizedAsync(HttpMethod.Post, "/api/cart/items", customerToken,
            new AddCartItemRequest { ProductId = product.Id, Quantity = 1 });
        var addedCart = await addResponse.Content.ReadFromJsonAsync<CartDtoResponse>();
        var cartItemId = addedCart!.Items.Single().Id;

        var updateResponse = await SendAuthorizedAsync(HttpMethod.Put, $"/api/cart/items/{cartItemId}", customerToken,
            new UpdateCartItemRequest { Quantity = 4 });
        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var cart = await updateResponse.Content.ReadFromJsonAsync<CartDtoResponse>();
        cart!.Items.Should().ContainSingle(i => i.ProductId == product.Id && i.Quantity == 4);
    }

    [Fact]
    public async Task RemoveItem_RemovesFromCart()
    {
        var customerToken = await TestAuthHelper.RegisterAndGetCustomerAccessTokenAsync(_client);
        var adminToken = await TestAuthHelper.GetAdminAccessTokenAsync(_client);
        var category = await CreateCategoryAsync(adminToken);
        var product = await CreateProductAsync(adminToken, category.Id, "Removable Product", 100m, 10);

        var addResponse = await SendAuthorizedAsync(HttpMethod.Post, "/api/cart/items", customerToken,
            new AddCartItemRequest { ProductId = product.Id, Quantity = 1 });
        var addedCart = await addResponse.Content.ReadFromJsonAsync<CartDtoResponse>();
        var cartItemId = addedCart!.Items.Single().Id;

        var removeResponse = await SendAuthorizedAsync(HttpMethod.Delete, $"/api/cart/items/{cartItemId}", customerToken, null);
        removeResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var cart = await removeResponse.Content.ReadFromJsonAsync<CartDtoResponse>();
        cart!.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task AddItem_ProductWithVariants_MissingColor_ReturnsBadRequest()
    {
        var customerToken = await TestAuthHelper.RegisterAndGetCustomerAccessTokenAsync(_client);
        var adminToken = await TestAuthHelper.GetAdminAccessTokenAsync(_client);
        var category = await CreateCategoryAsync(adminToken);
        var product = await CreateProductAsync(adminToken, category.Id, "Variant Product", 100m, 0);
        await CreateColorVariantAsync(adminToken, product.Id, "Đen", 5);

        var response = await SendAuthorizedAsync(HttpMethod.Post, "/api/cart/items", customerToken,
            new AddCartItemRequest { ProductId = product.Id, Quantity = 1 });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task AddItem_DifferentColorsOfSameProduct_CreateSeparateCartLines()
    {
        var customerToken = await TestAuthHelper.RegisterAndGetCustomerAccessTokenAsync(_client);
        var adminToken = await TestAuthHelper.GetAdminAccessTokenAsync(_client);
        var category = await CreateCategoryAsync(adminToken);
        var product = await CreateProductAsync(adminToken, category.Id, "Multi Color Product", 100m, 0);

        using var optionRequest = new HttpRequestMessage(HttpMethod.Post, $"/api/products/{product.Id}/variant-options")
        {
            Content = JsonContent.Create(new CreateVariantOptionRequest
            {
                Name = "Màu sắc",
                Values = new List<CreateVariantOptionValueRequest> { new() { Value = "Đen" }, new() { Value = "Trắng" } }
            })
        };
        optionRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        var optionResponse = await _client.SendAsync(optionRequest);
        var withOption = await optionResponse.Content.ReadFromJsonAsync<ProductDto>();
        var colorOption = withOption!.VariantOptions.Single(o => o.Name == "Màu sắc");
        var blackValueId = colorOption.Values.Single(v => v.Value == "Đen").Id;
        var whiteValueId = colorOption.Values.Single(v => v.Value == "Trắng").Id;

        var black = await CreateSkuAsync(adminToken, product.Id, blackValueId, null, 5);
        var white = await CreateSkuAsync(adminToken, product.Id, whiteValueId, null, 5);

        await SendAuthorizedAsync(HttpMethod.Post, "/api/cart/items", customerToken,
            new AddCartItemRequest { ProductId = product.Id, ProductVariantId = black.Id, Quantity = 1 });
        var response = await SendAuthorizedAsync(HttpMethod.Post, "/api/cart/items", customerToken,
            new AddCartItemRequest { ProductId = product.Id, ProductVariantId = white.Id, Quantity = 1 });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var cart = await response.Content.ReadFromJsonAsync<CartDtoResponse>();
        cart!.Items.Should().HaveCount(2);
        cart.Items.Should().Contain(i => i.VariantDescription == "Đen");
        cart.Items.Should().Contain(i => i.VariantDescription == "Trắng");
    }

    private async Task<ProductVariantDto> CreateColorVariantAsync(string adminToken, Guid productId, string colorName, int stock)
    {
        using var optionRequest = new HttpRequestMessage(HttpMethod.Post, $"/api/products/{productId}/variant-options")
        {
            Content = JsonContent.Create(new CreateVariantOptionRequest
            {
                Name = "Màu sắc",
                Values = new List<CreateVariantOptionValueRequest> { new() { Value = colorName } }
            })
        };
        optionRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        var optionResponse = await _client.SendAsync(optionRequest);
        var withOption = await optionResponse.Content.ReadFromJsonAsync<ProductDto>();
        var valueId = withOption!.VariantOptions.Single().Values.Single().Id;

        return await CreateSkuAsync(adminToken, productId, valueId, null, stock);
    }

    private async Task<ProductVariantDto> CreateSkuAsync(string adminToken, Guid productId, Guid optionValue1Id, Guid? optionValue2Id, int stock)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, $"/api/products/{productId}/variants")
        {
            Content = JsonContent.Create(new CreateProductVariantRequest { OptionValue1Id = optionValue1Id, OptionValue2Id = optionValue2Id, Stock = stock })
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        var response = await _client.SendAsync(request);
        var product = await response.Content.ReadFromJsonAsync<ProductDto>();
        return product!.Variants.Single(v => v.OptionValue1Id == optionValue1Id && v.OptionValue2Id == optionValue2Id);
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

public class CartDtoResponse
{
    public List<CartItemDto> Items { get; set; } = new();
    public decimal TotalAmount { get; set; }
}
