using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using ShopeeClone.Application.Cart.Dtos;
using ShopeeClone.Application.Categories.Dtos;
using ShopeeClone.Application.Common;
using ShopeeClone.Application.Notifications.Dtos;
using ShopeeClone.Application.Orders.Dtos;
using ShopeeClone.Application.Products.Dtos;
using ShopeeClone.Domain.Enums;

namespace ShopeeClone.API.IntegrationTests;

[Collection(IntegrationTestCollection.Name)]
public class NotificationsControllerTests
{
    private readonly HttpClient _client;

    public NotificationsControllerTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task UpdateOrderStatus_CreatesNotificationForOwner_AndIncrementsUnreadCount()
    {
        var customerToken = await TestAuthHelper.RegisterAndGetCustomerAccessTokenAsync(_client);
        var adminToken = await TestAuthHelper.GetAdminAccessTokenAsync(_client);
        var order = await CreateOrderAsync(customerToken, adminToken);

        await SendAuthorizedAsync(HttpMethod.Put, $"/api/orders/{order.Id}/status", adminToken,
            new UpdateOrderStatusRequest { Status = OrderStatus.Confirmed });

        var listResponse = await SendAuthorizedAsync(HttpMethod.Get, "/api/notifications", customerToken, null);
        listResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var list = await listResponse.Content.ReadFromJsonAsync<PagedResultDto<NotificationDto>>();
        list!.Items.Should().ContainSingle(n => n.OrderId == order.Id && !n.IsRead);

        var countResponse = await SendAuthorizedAsync(HttpMethod.Get, "/api/notifications/unread-count", customerToken, null);
        var count = await countResponse.Content.ReadFromJsonAsync<int>();
        count.Should().Be(1);
    }

    [Fact]
    public async Task MarkAsRead_OwnNotification_ResetsUnreadCount()
    {
        var customerToken = await TestAuthHelper.RegisterAndGetCustomerAccessTokenAsync(_client);
        var adminToken = await TestAuthHelper.GetAdminAccessTokenAsync(_client);
        var order = await CreateOrderAsync(customerToken, adminToken);
        await SendAuthorizedAsync(HttpMethod.Put, $"/api/orders/{order.Id}/status", adminToken,
            new UpdateOrderStatusRequest { Status = OrderStatus.Confirmed });
        var listResponse = await SendAuthorizedAsync(HttpMethod.Get, "/api/notifications", customerToken, null);
        var list = await listResponse.Content.ReadFromJsonAsync<PagedResultDto<NotificationDto>>();
        var notification = list!.Items.Single(n => n.OrderId == order.Id);

        var readResponse = await SendAuthorizedAsync(HttpMethod.Put, $"/api/notifications/{notification.Id}/read", customerToken, null);

        readResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
        var countResponse = await SendAuthorizedAsync(HttpMethod.Get, "/api/notifications/unread-count", customerToken, null);
        var count = await countResponse.Content.ReadFromJsonAsync<int>();
        count.Should().Be(0);
    }

    [Fact]
    public async Task MarkAsRead_AnotherUsersNotification_ReturnsNotFound()
    {
        var customerToken = await TestAuthHelper.RegisterAndGetCustomerAccessTokenAsync(_client);
        var intruderToken = await TestAuthHelper.RegisterAndGetCustomerAccessTokenAsync(_client);
        var adminToken = await TestAuthHelper.GetAdminAccessTokenAsync(_client);
        var order = await CreateOrderAsync(customerToken, adminToken);
        await SendAuthorizedAsync(HttpMethod.Put, $"/api/orders/{order.Id}/status", adminToken,
            new UpdateOrderStatusRequest { Status = OrderStatus.Confirmed });
        var listResponse = await SendAuthorizedAsync(HttpMethod.Get, "/api/notifications", customerToken, null);
        var list = await listResponse.Content.ReadFromJsonAsync<PagedResultDto<NotificationDto>>();
        var notification = list!.Items.Single(n => n.OrderId == order.Id);

        var response = await SendAuthorizedAsync(HttpMethod.Put, $"/api/notifications/{notification.Id}/read", intruderToken, null);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task MarkAllAsRead_ResetsUnreadCountToZero()
    {
        var customerToken = await TestAuthHelper.RegisterAndGetCustomerAccessTokenAsync(_client);
        var adminToken = await TestAuthHelper.GetAdminAccessTokenAsync(_client);
        var order = await CreateOrderAsync(customerToken, adminToken);
        await SendAuthorizedAsync(HttpMethod.Put, $"/api/orders/{order.Id}/status", adminToken,
            new UpdateOrderStatusRequest { Status = OrderStatus.Confirmed });
        await SendAuthorizedAsync(HttpMethod.Put, $"/api/orders/{order.Id}/status", adminToken,
            new UpdateOrderStatusRequest { Status = OrderStatus.Shipped });

        var response = await SendAuthorizedAsync(HttpMethod.Put, "/api/notifications/read-all", customerToken, null);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        var countResponse = await SendAuthorizedAsync(HttpMethod.Get, "/api/notifications/unread-count", customerToken, null);
        var count = await countResponse.Content.ReadFromJsonAsync<int>();
        count.Should().Be(0);
    }

    private async Task<OrderDto> CreateOrderAsync(string customerToken, string adminToken)
    {
        var category = await CreateCategoryAsync(adminToken);
        var product = await CreateProductAsync(adminToken, category.Id, $"Notification Test Product {Guid.NewGuid():N}", 100m, 10);

        await SendAuthorizedAsync(HttpMethod.Post, "/api/cart/items", customerToken,
            new AddCartItemRequest { ProductId = product.Id, Quantity = 1 });
        var createResponse = await SendAuthorizedAsync(HttpMethod.Post, "/api/orders", customerToken,
            new CreateOrderRequest { ShippingAddress = "addr", PaymentMethod = PaymentMethod.Cod });

        return (await createResponse.Content.ReadFromJsonAsync<OrderDto>())!;
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
