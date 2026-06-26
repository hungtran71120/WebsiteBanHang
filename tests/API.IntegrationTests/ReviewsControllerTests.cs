using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using HungStore.Application.Cart.Dtos;
using HungStore.Application.Categories.Dtos;
using HungStore.Application.Orders.Dtos;
using HungStore.Application.Products.Dtos;
using HungStore.Application.Reviews.Dtos;
using HungStore.Domain.Enums;
using HungStore.Infrastructure.Persistence;

namespace HungStore.API.IntegrationTests;

[Collection(IntegrationTestCollection.Name)]
public class ReviewsControllerTests
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;

    public ReviewsControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Create_WithoutPurchase_ReturnsBadRequest()
    {
        var customerToken = await TestAuthHelper.RegisterAndGetCustomerAccessTokenAsync(_client);
        var adminToken = await TestAuthHelper.GetAdminAccessTokenAsync(_client);
        var category = await CreateCategoryAsync(adminToken);
        var product = await CreateProductAsync(adminToken, category.Id, "Review Test Product A", 100m, 5);

        var response = await SendAuthorizedAsync(HttpMethod.Post, $"/api/products/{product.Id}/reviews", customerToken,
            new CreateReviewRequest { Rating = 5, Comment = "Rất tốt." });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Create_AfterDeliveredOrder_SucceedsAndUpdatesProductAverageRating()
    {
        var customerToken = await TestAuthHelper.RegisterAndGetCustomerAccessTokenAsync(_client);
        var adminToken = await TestAuthHelper.GetAdminAccessTokenAsync(_client);
        var category = await CreateCategoryAsync(adminToken);
        var product = await CreateProductAsync(adminToken, category.Id, "Review Test Product B", 100m, 5);

        var order = await PurchaseAndMarkDeliveredAsync(customerToken, product.Id);

        var response = await SendAuthorizedAsync(HttpMethod.Post, $"/api/products/{product.Id}/reviews", customerToken,
            new CreateReviewRequest { Rating = 4, Comment = "Dùng tốt sau vài ngày." });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var review = await response.Content.ReadFromJsonAsync<ReviewDto>();
        review!.Rating.Should().Be(4);

        var productResponse = await _client.GetAsync($"/api/products/{product.Id}");
        var updatedProduct = await productResponse.Content.ReadFromJsonAsync<ProductDto>();
        updatedProduct!.AverageRating.Should().Be(4);
        updatedProduct.ReviewCount.Should().Be(1);

        order.Should().NotBeNull();
    }

    [Fact]
    public async Task Create_DuplicateReview_ReturnsBadRequest()
    {
        var customerToken = await TestAuthHelper.RegisterAndGetCustomerAccessTokenAsync(_client);
        var adminToken = await TestAuthHelper.GetAdminAccessTokenAsync(_client);
        var category = await CreateCategoryAsync(adminToken);
        var product = await CreateProductAsync(adminToken, category.Id, "Review Test Product C", 100m, 5);
        await PurchaseAndMarkDeliveredAsync(customerToken, product.Id);

        var first = await SendAuthorizedAsync(HttpMethod.Post, $"/api/products/{product.Id}/reviews", customerToken,
            new CreateReviewRequest { Rating = 5, Comment = "Lần đầu." });
        first.StatusCode.Should().Be(HttpStatusCode.OK);

        var second = await SendAuthorizedAsync(HttpMethod.Post, $"/api/products/{product.Id}/reviews", customerToken,
            new CreateReviewRequest { Rating = 3, Comment = "Đánh giá lần 2." });

        second.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Update_NotOwner_ReturnsBadRequest()
    {
        var ownerToken = await TestAuthHelper.RegisterAndGetCustomerAccessTokenAsync(_client);
        var otherToken = await TestAuthHelper.RegisterAndGetCustomerAccessTokenAsync(_client);
        var adminToken = await TestAuthHelper.GetAdminAccessTokenAsync(_client);
        var category = await CreateCategoryAsync(adminToken);
        var product = await CreateProductAsync(adminToken, category.Id, "Review Test Product D", 100m, 5);
        await PurchaseAndMarkDeliveredAsync(ownerToken, product.Id);

        var createResponse = await SendAuthorizedAsync(HttpMethod.Post, $"/api/products/{product.Id}/reviews", ownerToken,
            new CreateReviewRequest { Rating = 5, Comment = "Của tôi." });
        var review = await createResponse.Content.ReadFromJsonAsync<ReviewDto>();

        var updateResponse = await SendAuthorizedAsync(HttpMethod.Put, $"/api/reviews/{review!.Id}", otherToken,
            new UpdateReviewRequest { Rating = 1, Comment = "Sửa của người khác." });

        updateResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Update_Owner_ChangesRatingAndComment()
    {
        var customerToken = await TestAuthHelper.RegisterAndGetCustomerAccessTokenAsync(_client);
        var adminToken = await TestAuthHelper.GetAdminAccessTokenAsync(_client);
        var category = await CreateCategoryAsync(adminToken);
        var product = await CreateProductAsync(adminToken, category.Id, "Review Test Product E", 100m, 5);
        await PurchaseAndMarkDeliveredAsync(customerToken, product.Id);

        var createResponse = await SendAuthorizedAsync(HttpMethod.Post, $"/api/products/{product.Id}/reviews", customerToken,
            new CreateReviewRequest { Rating = 5, Comment = "Ban đầu tốt." });
        var review = await createResponse.Content.ReadFromJsonAsync<ReviewDto>();

        var updateResponse = await SendAuthorizedAsync(HttpMethod.Put, $"/api/reviews/{review!.Id}", customerToken,
            new UpdateReviewRequest { Rating = 2, Comment = "Dùng lâu thấy không tốt như mong đợi." });

        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await updateResponse.Content.ReadFromJsonAsync<ReviewDto>();
        updated!.Rating.Should().Be(2);
    }

    [Fact]
    public async Task Delete_Owner_RemovesReviewFromProductList()
    {
        var customerToken = await TestAuthHelper.RegisterAndGetCustomerAccessTokenAsync(_client);
        var adminToken = await TestAuthHelper.GetAdminAccessTokenAsync(_client);
        var category = await CreateCategoryAsync(adminToken);
        var product = await CreateProductAsync(adminToken, category.Id, "Review Test Product F", 100m, 5);
        await PurchaseAndMarkDeliveredAsync(customerToken, product.Id);

        var createResponse = await SendAuthorizedAsync(HttpMethod.Post, $"/api/products/{product.Id}/reviews", customerToken,
            new CreateReviewRequest { Rating = 5, Comment = "Sẽ bị xóa." });
        var review = await createResponse.Content.ReadFromJsonAsync<ReviewDto>();

        var deleteResponse = await SendAuthorizedAsync(HttpMethod.Delete, $"/api/reviews/{review!.Id}", customerToken, null);
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var listResponse = await _client.GetAsync($"/api/products/{product.Id}/reviews");
        var list = await listResponse.Content.ReadFromJsonAsync<PagedResultDto<ReviewDto>>();
        list!.Items.Should().BeEmpty();
    }

    private async Task<OrderDto> PurchaseAndMarkDeliveredAsync(string customerToken, Guid productId)
    {
        await SendAuthorizedAsync(HttpMethod.Post, "/api/cart/items", customerToken,
            new AddCartItemRequest { ProductId = productId, Quantity = 1 });

        var createResponse = await SendAuthorizedAsync(HttpMethod.Post, "/api/orders", customerToken,
            new CreateOrderRequest { ShippingAddress = "123 Test St", PaymentMethod = PaymentMethod.MockPaid });
        var order = (await createResponse.Content.ReadFromJsonAsync<OrderDto>())!;

        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var dbOrder = await context.Orders.FirstAsync(o => o.Id == order.Id);
        dbOrder.Status = OrderStatus.Delivered;
        await context.SaveChangesAsync();

        return order;
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
