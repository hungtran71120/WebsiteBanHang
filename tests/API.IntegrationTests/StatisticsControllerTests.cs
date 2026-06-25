using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using ShopeeClone.Application.Cart.Dtos;
using ShopeeClone.Application.Categories.Dtos;
using ShopeeClone.Application.Orders.Dtos;
using ShopeeClone.Application.Products.Dtos;
using ShopeeClone.Application.Statistics.Dtos;
using ShopeeClone.Domain.Enums;

namespace ShopeeClone.API.IntegrationTests;

[Collection(IntegrationTestCollection.Name)]
public class StatisticsControllerTests
{
    private readonly HttpClient _client;

    public StatisticsControllerTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetDashboard_AsCustomer_ReturnsForbidden()
    {
        var customerToken = await TestAuthHelper.RegisterAndGetCustomerAccessTokenAsync(_client);

        var response = await SendAuthorizedAsync(HttpMethod.Get, "/api/statistics/dashboard", customerToken, null);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetDashboard_AsAdmin_ReflectsCreatedOrder()
    {
        var customerToken = await TestAuthHelper.RegisterAndGetCustomerAccessTokenAsync(_client);
        var adminToken = await TestAuthHelper.GetAdminAccessTokenAsync(_client);
        var category = await CreateCategoryAsync(adminToken);
        var product = await CreateProductAsync(adminToken, category.Id, "Stats Test Product", 300m, 10);

        var before = await GetDashboardAsync(adminToken);

        await SendAuthorizedAsync(HttpMethod.Post, "/api/cart/items", customerToken,
            new AddCartItemRequest { ProductId = product.Id, Quantity = 1 });
        await SendAuthorizedAsync(HttpMethod.Post, "/api/orders", customerToken,
            new CreateOrderRequest { ShippingAddress = "addr", PaymentMethod = PaymentMethod.MockPaid });

        var after = await GetDashboardAsync(adminToken);

        after.TotalOrders.Should().Be(before.TotalOrders + 1);
        after.TotalRevenue.Should().Be(before.TotalRevenue + 300m);
        after.OrdersByStatus[nameof(OrderStatus.Pending)].Should().Be(before.OrdersByStatus[nameof(OrderStatus.Pending)] + 1);
    }

    private async Task<DashboardStatisticsDto> GetDashboardAsync(string adminToken)
    {
        var response = await SendAuthorizedAsync(HttpMethod.Get, "/api/statistics/dashboard", adminToken, null);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        return (await response.Content.ReadFromJsonAsync<DashboardStatisticsDto>())!;
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
