using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using ShopeeClone.Application.Cart.Dtos;
using ShopeeClone.Application.Categories.Dtos;
using ShopeeClone.Application.Products.Dtos;
using ShopeeClone.Application.Vouchers.Dtos;
using ShopeeClone.Domain.Enums;

namespace ShopeeClone.API.IntegrationTests;

[Collection(IntegrationTestCollection.Name)]
public class VouchersControllerTests
{
    private readonly HttpClient _client;

    public VouchersControllerTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Create_AsCustomer_ReturnsForbidden()
    {
        var customerToken = await TestAuthHelper.RegisterAndGetCustomerAccessTokenAsync(_client);

        var response = await SendAuthorizedAsync(HttpMethod.Post, "/api/vouchers", customerToken,
            new CreateVoucherRequest { Code = "SHOULDFAIL", DiscountValue = 10, ExpiresAt = DateTime.UtcNow.AddDays(1) });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Create_AsAdmin_ThenGetPaged_ReturnsCreatedVoucher()
    {
        var adminToken = await TestAuthHelper.GetAdminAccessTokenAsync(_client);
        var code = $"ADMIN-{Guid.NewGuid():N}";

        var createResponse = await SendAuthorizedAsync(HttpMethod.Post, "/api/vouchers", adminToken,
            new CreateVoucherRequest { Code = code, DiscountType = VoucherDiscountType.FixedAmount, DiscountValue = 50, ExpiresAt = DateTime.UtcNow.AddDays(1) });
        createResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var pagedResponse = await SendAuthorizedAsync(HttpMethod.Get, "/api/vouchers?page=1&pageSize=50", adminToken, null);
        var page = await pagedResponse.Content.ReadFromJsonAsync<PagedResultDto<VoucherDto>>();

        page!.Items.Should().Contain(v => v.Code == code);
    }

    [Fact]
    public async Task Create_DuplicateCode_ReturnsBadRequest()
    {
        var adminToken = await TestAuthHelper.GetAdminAccessTokenAsync(_client);
        var code = $"DUP-{Guid.NewGuid():N}";

        await SendAuthorizedAsync(HttpMethod.Post, "/api/vouchers", adminToken,
            new CreateVoucherRequest { Code = code, DiscountValue = 10, ExpiresAt = DateTime.UtcNow.AddDays(1) });

        var response = await SendAuthorizedAsync(HttpMethod.Post, "/api/vouchers", adminToken,
            new CreateVoucherRequest { Code = code, DiscountValue = 5, ExpiresAt = DateTime.UtcNow.AddDays(1) });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Validate_NotAuthenticated_ReturnsUnauthorized()
    {
        var response = await _client.PostAsJsonAsync("/api/vouchers/validate", new ValidateVoucherRequest { Code = "ANY" });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Validate_UnknownCode_ReturnsBadRequest()
    {
        var customerToken = await TestAuthHelper.RegisterAndGetCustomerAccessTokenAsync(_client);

        var response = await SendAuthorizedAsync(HttpMethod.Post, "/api/vouchers/validate", customerToken,
            new ValidateVoucherRequest { Code = "DOES-NOT-EXIST" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Validate_ValidCodeWithCartItems_ReturnsDiscountPreview()
    {
        var customerToken = await TestAuthHelper.RegisterAndGetCustomerAccessTokenAsync(_client);
        var adminToken = await TestAuthHelper.GetAdminAccessTokenAsync(_client);
        var category = await CreateCategoryAsync(adminToken);
        var product = await CreateProductAsync(adminToken, category.Id, "Voucher Preview Test Product", 100m, 10);
        var code = $"PREVIEW-{Guid.NewGuid():N}";

        await SendAuthorizedAsync(HttpMethod.Post, "/api/vouchers", adminToken,
            new CreateVoucherRequest { Code = code, DiscountType = VoucherDiscountType.Percentage, DiscountValue = 10, ExpiresAt = DateTime.UtcNow.AddDays(1) });
        await SendAuthorizedAsync(HttpMethod.Post, "/api/cart/items", customerToken,
            new AddCartItemRequest { ProductId = product.Id, Quantity = 2 });

        var response = await SendAuthorizedAsync(HttpMethod.Post, "/api/vouchers/validate", customerToken,
            new ValidateVoucherRequest { Code = code });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<VoucherValidationResultDto>();
        result!.Subtotal.Should().Be(200m);
        result.DiscountAmount.Should().Be(20m);
        result.FinalTotal.Should().Be(180m);
    }

    [Fact]
    public async Task Delete_AsAdmin_RemovesVoucher()
    {
        var adminToken = await TestAuthHelper.GetAdminAccessTokenAsync(_client);
        var code = $"DELETE-{Guid.NewGuid():N}";
        var createResponse = await SendAuthorizedAsync(HttpMethod.Post, "/api/vouchers", adminToken,
            new CreateVoucherRequest { Code = code, DiscountValue = 10, ExpiresAt = DateTime.UtcNow.AddDays(1) });
        var created = await createResponse.Content.ReadFromJsonAsync<VoucherDto>();

        var deleteResponse = await SendAuthorizedAsync(HttpMethod.Delete, $"/api/vouchers/{created!.Id}", adminToken, null);

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
