using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using HungStore.Application.Categories.Dtos;
using HungStore.Application.Products.Dtos;

namespace HungStore.API.IntegrationTests;

[Collection(IntegrationTestCollection.Name)]
public class ProductsControllerTests
{
    private readonly HttpClient _client;

    public ProductsControllerTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetById_WithUnknownId_ReturnsNotFound()
    {
        var response = await _client.GetAsync($"/api/products/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Create_AsCustomer_ReturnsForbidden()
    {
        var customerToken = await TestAuthHelper.RegisterAndGetCustomerAccessTokenAsync(_client);

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/products")
        {
            Content = JsonContent.Create(new CreateProductRequest
            {
                Name = "Should Fail",
                Description = "x",
                Price = 10,
                Stock = 1,
                CategoryId = Guid.NewGuid()
            })
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", customerToken);

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Create_AsAdmin_ThenGetById_ReturnsCreatedProduct()
    {
        var adminToken = await TestAuthHelper.GetAdminAccessTokenAsync(_client);
        var category = await CreateCategoryAsync(adminToken);

        var product = await CreateProductAsync(adminToken, category.Id, "Unique Phone", 999.99m, 5);

        var getResponse = await _client.GetAsync($"/api/products/{product.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var fetched = await getResponse.Content.ReadFromJsonAsync<ProductDto>();
        fetched!.Name.Should().Be(product.Name);
        fetched.CategoryName.Should().Be(category.Name);
    }

    [Fact]
    public async Task GetPaged_FilterByKeywordAndCategoryAndPriceRange_ReturnsOnlyMatchingProducts()
    {
        var adminToken = await TestAuthHelper.GetAdminAccessTokenAsync(_client);
        var category = await CreateCategoryAsync(adminToken);
        var otherCategory = await CreateCategoryAsync(adminToken);

        var keyword = $"Filterable-{Guid.NewGuid():N}";
        var matching = await CreateProductAsync(adminToken, category.Id, $"{keyword} Laptop", 500m, 3);
        await CreateProductAsync(adminToken, category.Id, $"{keyword} Out Of Range", 5000m, 3);
        await CreateProductAsync(adminToken, otherCategory.Id, $"{keyword} Wrong Category", 500m, 3);

        var query = $"/api/products?keyword={Uri.EscapeDataString(keyword)}&categoryId={category.Id}&minPrice=100&maxPrice=1000&page=1&pageSize=10";
        var response = await _client.GetAsync(query);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<PagedResultDto<ProductDto>>();
        result!.Items.Should().ContainSingle(p => p.Id == matching.Id);
    }

    [Fact]
    public async Task GetRelatedProducts_ReturnsOnlySameCategoryExcludingSelf()
    {
        var adminToken = await TestAuthHelper.GetAdminAccessTokenAsync(_client);
        var category = await CreateCategoryAsync(adminToken);
        var otherCategory = await CreateCategoryAsync(adminToken);

        var target = await CreateProductAsync(adminToken, category.Id, "Related Target Phone", 100m, 5);
        var sameCategory = await CreateProductAsync(adminToken, category.Id, "Related Same Category Phone", 120m, 5);
        await CreateProductAsync(adminToken, otherCategory.Id, "Related Other Category Phone", 80m, 5);

        var response = await _client.GetAsync($"/api/products/{target.Id}/related-products");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var related = await response.Content.ReadFromJsonAsync<List<ProductDto>>();
        related.Should().Contain(p => p.Id == sameCategory.Id);
        related.Should().NotContain(p => p.Id == target.Id);
    }

    [Fact]
    public async Task UploadImage_AsAdmin_SetsImageUrl()
    {
        var adminToken = await TestAuthHelper.GetAdminAccessTokenAsync(_client);
        var category = await CreateCategoryAsync(adminToken);
        var product = await CreateProductAsync(adminToken, category.Id, "Product With Image", 100m, 1);

        using var content = new MultipartFormDataContent();
        var imageBytes = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 };
        var fileContent = new ByteArrayContent(imageBytes);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
        content.Add(fileContent, "file", "test.jpg");

        using var request = new HttpRequestMessage(HttpMethod.Post, $"/api/products/{product.Id}/image")
        {
            Content = content
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        var response = await _client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var updated = await response.Content.ReadFromJsonAsync<ProductDto>();
        updated!.ImageUrl.Should().StartWith("/uploads/products/");
    }

    [Fact]
    public async Task AddVariantOption_AsAdmin_AppearsInProductVariantOptions()
    {
        var adminToken = await TestAuthHelper.GetAdminAccessTokenAsync(_client);
        var category = await CreateCategoryAsync(adminToken);
        var product = await CreateProductAsync(adminToken, category.Id, "Variant Phone", 100m, 0);

        var updated = await AddVariantOptionAsync(adminToken, product.Id, "Màu sắc", "Đen", "Trắng");

        updated.VariantOptions.Should().ContainSingle(o => o.Name == "Màu sắc" && o.DisplayOrder == 1);
        updated.VariantOptions.Single().Values.Should().HaveCount(2);
    }

    [Fact]
    public async Task AddVariantOption_AsCustomer_ReturnsForbidden()
    {
        var customerToken = await TestAuthHelper.RegisterAndGetCustomerAccessTokenAsync(_client);
        var adminToken = await TestAuthHelper.GetAdminAccessTokenAsync(_client);
        var category = await CreateCategoryAsync(adminToken);
        var product = await CreateProductAsync(adminToken, category.Id, "Variant Phone Forbidden", 100m, 0);

        using var request = new HttpRequestMessage(HttpMethod.Post, $"/api/products/{product.Id}/variant-options")
        {
            Content = JsonContent.Create(new CreateVariantOptionRequest
            {
                Name = "Màu sắc",
                Values = new List<CreateVariantOptionValueRequest> { new() { Value = "Xanh" } }
            })
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", customerToken);

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task AddVariantOption_ThirdOption_ReturnsBadRequest()
    {
        var adminToken = await TestAuthHelper.GetAdminAccessTokenAsync(_client);
        var category = await CreateCategoryAsync(adminToken);
        var product = await CreateProductAsync(adminToken, category.Id, "Variant Phone Max Options", 100m, 0);

        await AddVariantOptionAsync(adminToken, product.Id, "Màu sắc", "Đen");
        await AddVariantOptionAsync(adminToken, product.Id, "Dung lượng", "128GB");

        using var request = new HttpRequestMessage(HttpMethod.Post, $"/api/products/{product.Id}/variant-options")
        {
            Content = JsonContent.Create(new CreateVariantOptionRequest
            {
                Name = "Phiên bản",
                Values = new List<CreateVariantOptionValueRequest> { new() { Value = "X" } }
            })
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task AddVariant_TwoDimensionCombo_CreatesSkuWithCombinedStock()
    {
        var adminToken = await TestAuthHelper.GetAdminAccessTokenAsync(_client);
        var category = await CreateCategoryAsync(adminToken);
        var product = await CreateProductAsync(adminToken, category.Id, "Variant Phone Combo", 100m, 0);

        var withColors = await AddVariantOptionAsync(adminToken, product.Id, "Màu sắc", "Đen", "Trắng");
        var withStorage = await AddVariantOptionAsync(adminToken, withColors.Id, "Dung lượng", "128GB", "256GB");

        var colorOption = withStorage.VariantOptions.Single(o => o.Name == "Màu sắc");
        var storageOption = withStorage.VariantOptions.Single(o => o.Name == "Dung lượng");
        var black = colorOption.Values.Single(v => v.Value == "Đen");
        var storage256 = storageOption.Values.Single(v => v.Value == "256GB");

        using var request = new HttpRequestMessage(HttpMethod.Post, $"/api/products/{product.Id}/variants")
        {
            Content = JsonContent.Create(new CreateProductVariantRequest { OptionValue1Id = black.Id, OptionValue2Id = storage256.Id, Stock = 6 })
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        var response = await _client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var updated = await response.Content.ReadFromJsonAsync<ProductDto>();
        updated!.Variants.Should().ContainSingle(v => v.OptionValue1Text == "Đen" && v.OptionValue2Text == "256GB" && v.Stock == 6);
        updated.Stock.Should().Be(6);
    }

    [Fact]
    public async Task DeleteVariantOptionValue_InUseBySku_ReturnsBadRequest()
    {
        var adminToken = await TestAuthHelper.GetAdminAccessTokenAsync(_client);
        var category = await CreateCategoryAsync(adminToken);
        var product = await CreateProductAsync(adminToken, category.Id, "Variant Phone Delete Guard", 100m, 0);

        var withColors = await AddVariantOptionAsync(adminToken, product.Id, "Màu sắc", "Đen");
        var colorOption = withColors.VariantOptions.Single(o => o.Name == "Màu sắc");
        var black = colorOption.Values.Single(v => v.Value == "Đen");

        using var createVariantRequest = new HttpRequestMessage(HttpMethod.Post, $"/api/products/{product.Id}/variants")
        {
            Content = JsonContent.Create(new CreateProductVariantRequest { OptionValue1Id = black.Id, Stock = 4 })
        };
        createVariantRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        await _client.SendAsync(createVariantRequest);

        using var deleteRequest = new HttpRequestMessage(
            HttpMethod.Delete,
            $"/api/products/{product.Id}/variant-options/{colorOption.Id}/values/{black.Id}");
        deleteRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        var response = await _client.SendAsync(deleteRequest);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    private async Task<ProductDto> AddVariantOptionAsync(string adminToken, Guid productId, string optionName, params string[] values)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, $"/api/products/{productId}/variant-options")
        {
            Content = JsonContent.Create(new CreateVariantOptionRequest
            {
                Name = optionName,
                Values = values.Select(v => new CreateVariantOptionValueRequest { Value = v }).ToList()
            })
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        var response = await _client.SendAsync(request);
        return (await response.Content.ReadFromJsonAsync<ProductDto>())!;
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

public class PagedResultDto<T>
{
    public List<T> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
}
