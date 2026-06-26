using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using HungStore.Application.Cart.Dtos;
using HungStore.Application.Categories.Dtos;
using HungStore.Application.FlashSales.Dtos;
using HungStore.Application.Orders.Dtos;
using HungStore.Application.Products.Dtos;
using HungStore.Application.Vouchers.Dtos;
using HungStore.Domain.Enums;

namespace HungStore.API.IntegrationTests;

[Collection(IntegrationTestCollection.Name)]
public class OrdersControllerTests
{
    private readonly HttpClient _client;

    public OrdersControllerTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Create_WithEmptyCart_ReturnsBadRequest()
    {
        var customerToken = await TestAuthHelper.RegisterAndGetCustomerAccessTokenAsync(_client);

        var response = await SendAuthorizedAsync(HttpMethod.Post, "/api/orders", customerToken,
            new CreateOrderRequest { ShippingAddress = "123 Test St", PaymentMethod = PaymentMethod.Cod });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Create_WithItemsInCart_ReducesStockAndClearsCartAndAppearsInHistory()
    {
        var customerToken = await TestAuthHelper.RegisterAndGetCustomerAccessTokenAsync(_client);
        var adminToken = await TestAuthHelper.GetAdminAccessTokenAsync(_client);
        var category = await CreateCategoryAsync(adminToken);
        var product = await CreateProductAsync(adminToken, category.Id, "Order Test Product", 200m, 10);

        await SendAuthorizedAsync(HttpMethod.Post, "/api/cart/items", customerToken,
            new AddCartItemRequest { ProductId = product.Id, Quantity = 3 });

        var createResponse = await SendAuthorizedAsync(HttpMethod.Post, "/api/orders", customerToken,
            new CreateOrderRequest { ShippingAddress = "123 Test St", PaymentMethod = PaymentMethod.MockPaid });
        createResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var order = await createResponse.Content.ReadFromJsonAsync<OrderDto>();
        order!.TotalAmount.Should().Be(600m);
        order.IsPaid.Should().BeTrue();
        order.Status.Should().Be(nameof(OrderStatus.Pending));

        var productResponse = await _client.GetAsync($"/api/products/{product.Id}");
        var updatedProduct = await productResponse.Content.ReadFromJsonAsync<ProductDto>();
        updatedProduct!.Stock.Should().Be(7);

        var cartResponse = await SendAuthorizedAsync(HttpMethod.Get, "/api/cart", customerToken, null);
        var cart = await cartResponse.Content.ReadFromJsonAsync<CartDtoResponse>();
        cart!.Items.Should().BeEmpty();

        var historyResponse = await SendAuthorizedAsync(HttpMethod.Get, "/api/orders", customerToken, null);
        var history = await historyResponse.Content.ReadFromJsonAsync<PagedResultDto<OrderSummaryDto>>();
        history!.Items.Should().ContainSingle(o => o.Id == order.Id);
    }

    [Fact]
    public async Task GetById_OtherUsersOrder_ReturnsNotFound()
    {
        var ownerToken = await TestAuthHelper.RegisterAndGetCustomerAccessTokenAsync(_client);
        var otherToken = await TestAuthHelper.RegisterAndGetCustomerAccessTokenAsync(_client);
        var adminToken = await TestAuthHelper.GetAdminAccessTokenAsync(_client);
        var category = await CreateCategoryAsync(adminToken);
        var product = await CreateProductAsync(adminToken, category.Id, "Privacy Test Product", 100m, 5);

        await SendAuthorizedAsync(HttpMethod.Post, "/api/cart/items", ownerToken,
            new AddCartItemRequest { ProductId = product.Id, Quantity = 1 });
        var createResponse = await SendAuthorizedAsync(HttpMethod.Post, "/api/orders", ownerToken,
            new CreateOrderRequest { ShippingAddress = "addr", PaymentMethod = PaymentMethod.Cod });
        var order = await createResponse.Content.ReadFromJsonAsync<OrderDto>();

        var getResponse = await SendAuthorizedAsync(HttpMethod.Get, $"/api/orders/{order!.Id}", otherToken, null);

        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetAll_AsCustomer_ReturnsForbidden()
    {
        var customerToken = await TestAuthHelper.RegisterAndGetCustomerAccessTokenAsync(_client);

        var response = await SendAuthorizedAsync(HttpMethod.Get, "/api/orders/all", customerToken, null);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetAll_AsAdmin_ReturnsOrdersAcrossUsers()
    {
        var customerToken = await TestAuthHelper.RegisterAndGetCustomerAccessTokenAsync(_client);
        var adminToken = await TestAuthHelper.GetAdminAccessTokenAsync(_client);
        var category = await CreateCategoryAsync(adminToken);
        var product = await CreateProductAsync(adminToken, category.Id, "Admin List Test Product", 100m, 10);

        await SendAuthorizedAsync(HttpMethod.Post, "/api/cart/items", customerToken,
            new AddCartItemRequest { ProductId = product.Id, Quantity = 1 });
        var createResponse = await SendAuthorizedAsync(HttpMethod.Post, "/api/orders", customerToken,
            new CreateOrderRequest { ShippingAddress = "addr", PaymentMethod = PaymentMethod.Cod });
        var order = await createResponse.Content.ReadFromJsonAsync<OrderDto>();

        var response = await SendAuthorizedAsync(HttpMethod.Get, "/api/orders/all", adminToken, null);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var page = await response.Content.ReadFromJsonAsync<PagedResultDto<AdminOrderSummaryDto>>();

        page!.Items.Should().Contain(o => o.Id == order!.Id);
    }

    [Fact]
    public async Task UpdateStatus_AsCustomer_ReturnsForbidden()
    {
        var customerToken = await TestAuthHelper.RegisterAndGetCustomerAccessTokenAsync(_client);

        var response = await SendAuthorizedAsync(HttpMethod.Put, $"/api/orders/{Guid.NewGuid()}/status", customerToken,
            new UpdateOrderStatusRequest { Status = OrderStatus.Confirmed });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task UpdateStatus_ValidTransition_UpdatesOrderStatus()
    {
        var customerToken = await TestAuthHelper.RegisterAndGetCustomerAccessTokenAsync(_client);
        var adminToken = await TestAuthHelper.GetAdminAccessTokenAsync(_client);
        var category = await CreateCategoryAsync(adminToken);
        var product = await CreateProductAsync(adminToken, category.Id, "Status Transition Test Product", 100m, 10);

        await SendAuthorizedAsync(HttpMethod.Post, "/api/cart/items", customerToken,
            new AddCartItemRequest { ProductId = product.Id, Quantity = 1 });
        var createResponse = await SendAuthorizedAsync(HttpMethod.Post, "/api/orders", customerToken,
            new CreateOrderRequest { ShippingAddress = "addr", PaymentMethod = PaymentMethod.Cod });
        var order = await createResponse.Content.ReadFromJsonAsync<OrderDto>();

        var response = await SendAuthorizedAsync(HttpMethod.Put, $"/api/orders/{order!.Id}/status", adminToken,
            new UpdateOrderStatusRequest { Status = OrderStatus.Confirmed });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await response.Content.ReadFromJsonAsync<OrderDto>();
        updated!.Status.Should().Be(nameof(OrderStatus.Confirmed));
    }

    [Fact]
    public async Task UpdateStatus_InvalidTransition_ReturnsBadRequest()
    {
        var customerToken = await TestAuthHelper.RegisterAndGetCustomerAccessTokenAsync(_client);
        var adminToken = await TestAuthHelper.GetAdminAccessTokenAsync(_client);
        var category = await CreateCategoryAsync(adminToken);
        var product = await CreateProductAsync(adminToken, category.Id, "Invalid Status Transition Product", 100m, 10);

        await SendAuthorizedAsync(HttpMethod.Post, "/api/cart/items", customerToken,
            new AddCartItemRequest { ProductId = product.Id, Quantity = 1 });
        var createResponse = await SendAuthorizedAsync(HttpMethod.Post, "/api/orders", customerToken,
            new CreateOrderRequest { ShippingAddress = "addr", PaymentMethod = PaymentMethod.Cod });
        var order = await createResponse.Content.ReadFromJsonAsync<OrderDto>();

        var response = await SendAuthorizedAsync(HttpMethod.Put, $"/api/orders/{order!.Id}/status", adminToken,
            new UpdateOrderStatusRequest { Status = OrderStatus.Delivered });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Create_WithValidVoucherCode_AppliesDiscount()
    {
        var customerToken = await TestAuthHelper.RegisterAndGetCustomerAccessTokenAsync(_client);
        var adminToken = await TestAuthHelper.GetAdminAccessTokenAsync(_client);
        var category = await CreateCategoryAsync(adminToken);
        var product = await CreateProductAsync(adminToken, category.Id, "Voucher Order Test Product", 200m, 10);
        var voucher = await CreateVoucherAsync(adminToken, $"ORDER10-{Guid.NewGuid():N}", VoucherDiscountType.Percentage, 10);

        await SendAuthorizedAsync(HttpMethod.Post, "/api/cart/items", customerToken,
            new AddCartItemRequest { ProductId = product.Id, Quantity = 1 });

        var createResponse = await SendAuthorizedAsync(HttpMethod.Post, "/api/orders", customerToken,
            new CreateOrderRequest { ShippingAddress = "123 Test St", PaymentMethod = PaymentMethod.Cod, VoucherCode = voucher.Code });
        createResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var order = await createResponse.Content.ReadFromJsonAsync<OrderDto>();
        order!.Subtotal.Should().Be(200m);
        order.DiscountAmount.Should().Be(20m);
        order.TotalAmount.Should().Be(180m);
        order.VoucherCode.Should().Be(voucher.Code);
    }

    [Fact]
    public async Task Create_WithInvalidVoucherCode_ReturnsBadRequest()
    {
        var customerToken = await TestAuthHelper.RegisterAndGetCustomerAccessTokenAsync(_client);
        var adminToken = await TestAuthHelper.GetAdminAccessTokenAsync(_client);
        var category = await CreateCategoryAsync(adminToken);
        var product = await CreateProductAsync(adminToken, category.Id, "Invalid Voucher Order Test Product", 200m, 10);

        await SendAuthorizedAsync(HttpMethod.Post, "/api/cart/items", customerToken,
            new AddCartItemRequest { ProductId = product.Id, Quantity = 1 });

        var response = await SendAuthorizedAsync(HttpMethod.Post, "/api/orders", customerToken,
            new CreateOrderRequest { ShippingAddress = "123 Test St", PaymentMethod = PaymentMethod.Cod, VoucherCode = "DOES-NOT-EXIST" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Create_ProductInActiveFlashSale_SnapshotsSalePriceAndIncrementsQuantitySold()
    {
        var customerToken = await TestAuthHelper.RegisterAndGetCustomerAccessTokenAsync(_client);
        var adminToken = await TestAuthHelper.GetAdminAccessTokenAsync(_client);
        var category = await CreateCategoryAsync(adminToken);
        var product = await CreateProductAsync(adminToken, category.Id, "Flash Sale Order Test Product", 200m, 10);

        using var createFlashSaleRequest = new HttpRequestMessage(HttpMethod.Post, "/api/flash-sales")
        {
            Content = JsonContent.Create(new CreateFlashSaleRequest
            {
                Name = $"Order Sale {Guid.NewGuid():N}",
                StartsAt = DateTime.UtcNow.AddHours(-1),
                EndsAt = DateTime.UtcNow.AddHours(1),
                IsActive = true
            })
        };
        createFlashSaleRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        var flashSaleResponse = await _client.SendAsync(createFlashSaleRequest);
        var flashSale = await flashSaleResponse.Content.ReadFromJsonAsync<FlashSaleDto>();

        await SendAuthorizedAsync(HttpMethod.Post, $"/api/flash-sales/{flashSale!.Id}/items", adminToken,
            new AddFlashSaleItemRequest { ProductId = product.Id, SalePrice = 150m, QuantityLimit = 5 });

        await SendAuthorizedAsync(HttpMethod.Post, "/api/cart/items", customerToken,
            new AddCartItemRequest { ProductId = product.Id, Quantity = 2 });

        var createResponse = await SendAuthorizedAsync(HttpMethod.Post, "/api/orders", customerToken,
            new CreateOrderRequest { ShippingAddress = "123 Test St", PaymentMethod = PaymentMethod.Cod });
        createResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var order = await createResponse.Content.ReadFromJsonAsync<OrderDto>();
        order!.Items.Single().UnitPrice.Should().Be(150m);
        order.TotalAmount.Should().Be(300m);

        var flashSaleDetailResponse = await SendAuthorizedAsync(HttpMethod.Get, $"/api/flash-sales/{flashSale.Id}", adminToken, null);
        var flashSaleDetail = await flashSaleDetailResponse.Content.ReadFromJsonAsync<FlashSaleDto>();
        flashSaleDetail!.Items.Single(i => i.ProductId == product.Id).QuantitySold.Should().Be(2);
    }

    private async Task<VoucherDto> CreateVoucherAsync(string adminToken, string code, VoucherDiscountType type, decimal value)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/vouchers")
        {
            Content = JsonContent.Create(new CreateVoucherRequest
            {
                Code = code,
                DiscountType = type,
                DiscountValue = value,
                ExpiresAt = DateTime.UtcNow.AddDays(1)
            })
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        var response = await _client.SendAsync(request);
        return (await response.Content.ReadFromJsonAsync<VoucherDto>())!;
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
