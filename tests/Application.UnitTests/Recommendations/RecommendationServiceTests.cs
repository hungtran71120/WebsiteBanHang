using FluentAssertions;
using Moq;
using HungStore.Application.Recommendations;
using HungStore.Domain.Entities;
using HungStore.Domain.Interfaces;

namespace HungStore.Application.UnitTests.Recommendations;

public class RecommendationServiceTests
{
    private readonly Mock<IProductRepository> _productRepositoryMock = new();
    private readonly Mock<IOrderRepository> _orderRepositoryMock = new();
    private readonly Mock<IReviewRepository> _reviewRepositoryMock = new();
    private readonly RecommendationService _sut;

    public RecommendationServiceTests()
    {
        _sut = new RecommendationService(_productRepositoryMock.Object, _orderRepositoryMock.Object, _reviewRepositoryMock.Object);
        _reviewRepositoryMock.Setup(x => x.GetRatingSummaryAsync(It.IsAny<Guid>())).ReturnsAsync((0, 0));
    }

    [Fact]
    public async Task GetRelatedProductsAsync_UnknownProduct_ReturnsEmpty()
    {
        var productId = Guid.NewGuid();
        _productRepositoryMock.Setup(x => x.GetByIdAsync(productId)).ReturnsAsync((Product?)null);

        var result = await _sut.GetRelatedProductsAsync(productId);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetRelatedProductsAsync_ExistingProduct_ReturnsSameCategoryProducts()
    {
        var categoryId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var product = new Product { Id = productId, Name = "Phone", CategoryId = categoryId };
        var related = new List<Product> { new() { Id = Guid.NewGuid(), Name = "Other Phone", CategoryId = categoryId } };

        _productRepositoryMock.Setup(x => x.GetByIdAsync(productId)).ReturnsAsync(product);
        _productRepositoryMock.Setup(x => x.GetByCategoryAsync(categoryId, productId, 10)).ReturnsAsync(related);

        var result = await _sut.GetRelatedProductsAsync(productId);

        result.Should().ContainSingle(p => p.Name == "Other Phone");
    }

    [Fact]
    public async Task GetPersonalizedRecommendationsAsync_AnonymousUser_FallsBackToTopRated()
    {
        var topRated = new List<Product> { new() { Id = Guid.NewGuid(), Name = "Best Seller" } };
        _productRepositoryMock.Setup(x => x.GetTopRatedAsync(Array.Empty<Guid>(), 10)).ReturnsAsync(topRated);

        var result = await _sut.GetPersonalizedRecommendationsAsync(null);

        result.Should().ContainSingle(p => p.Name == "Best Seller");
        _orderRepositoryMock.Verify(x => x.GetPurchasedCategoryIdsAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task GetPersonalizedRecommendationsAsync_UserWithNoPurchaseHistory_FallsBackToTopRated()
    {
        var userId = "user-1";
        var topRated = new List<Product> { new() { Id = Guid.NewGuid(), Name = "Best Seller" } };
        _orderRepositoryMock.Setup(x => x.GetPurchasedProductIdsAsync(userId)).ReturnsAsync(Array.Empty<Guid>());
        _orderRepositoryMock.Setup(x => x.GetPurchasedCategoryIdsAsync(userId)).ReturnsAsync(Array.Empty<Guid>());
        _productRepositoryMock.Setup(x => x.GetTopRatedAsync(Array.Empty<Guid>(), 10)).ReturnsAsync(topRated);

        var result = await _sut.GetPersonalizedRecommendationsAsync(userId);

        result.Should().ContainSingle(p => p.Name == "Best Seller");
    }

    [Fact]
    public async Task GetPersonalizedRecommendationsAsync_UserWithPurchaseHistory_PrefersPurchasedCategories()
    {
        var userId = "user-1";
        var purchasedCategoryId = Guid.NewGuid();
        var purchasedProductIds = new List<Guid> { Guid.NewGuid() };
        var candidateInCategory = new Product { Id = Guid.NewGuid(), Name = "Matching Category", CategoryId = purchasedCategoryId };
        var candidateOtherCategory = new Product { Id = Guid.NewGuid(), Name = "Other Category", CategoryId = Guid.NewGuid() };

        _orderRepositoryMock.Setup(x => x.GetPurchasedProductIdsAsync(userId)).ReturnsAsync(purchasedProductIds);
        _orderRepositoryMock.Setup(x => x.GetPurchasedCategoryIdsAsync(userId)).ReturnsAsync(new[] { purchasedCategoryId });
        _productRepositoryMock
            .Setup(x => x.GetTopRatedAsync(purchasedProductIds, 30))
            .ReturnsAsync(new List<Product> { candidateOtherCategory, candidateInCategory });

        var result = await _sut.GetPersonalizedRecommendationsAsync(userId);

        result.Should().ContainSingle(p => p.Name == "Matching Category");
        _productRepositoryMock.Verify(x => x.GetTopRatedAsync(purchasedProductIds, 10), Times.Never);
    }

    [Fact]
    public async Task GetPersonalizedRecommendationsAsync_NoCandidatesInPurchasedCategories_FallsBackToTopRated()
    {
        var userId = "user-1";
        var purchasedCategoryId = Guid.NewGuid();
        var purchasedProductIds = new List<Guid> { Guid.NewGuid() };
        var fallbackProduct = new Product { Id = Guid.NewGuid(), Name = "Fallback Product", CategoryId = Guid.NewGuid() };

        _orderRepositoryMock.Setup(x => x.GetPurchasedProductIdsAsync(userId)).ReturnsAsync(purchasedProductIds);
        _orderRepositoryMock.Setup(x => x.GetPurchasedCategoryIdsAsync(userId)).ReturnsAsync(new[] { purchasedCategoryId });
        _productRepositoryMock
            .Setup(x => x.GetTopRatedAsync(purchasedProductIds, 30))
            .ReturnsAsync(new List<Product> { new() { Id = Guid.NewGuid(), Name = "Unrelated", CategoryId = Guid.NewGuid() } });
        _productRepositoryMock
            .Setup(x => x.GetTopRatedAsync(purchasedProductIds, 10))
            .ReturnsAsync(new List<Product> { fallbackProduct });

        var result = await _sut.GetPersonalizedRecommendationsAsync(userId);

        result.Should().ContainSingle(p => p.Name == "Fallback Product");
    }
}
