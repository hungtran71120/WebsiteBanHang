using FluentAssertions;
using Moq;
using HungStore.Application.Wishlist;
using HungStore.Domain.Entities;
using HungStore.Domain.Interfaces;

namespace HungStore.Application.UnitTests.Wishlist;

public class WishlistServiceTests
{
    private readonly Mock<IWishlistRepository> _wishlistRepositoryMock = new();
    private readonly Mock<IProductRepository> _productRepositoryMock = new();
    private readonly WishlistService _sut;

    public WishlistServiceTests()
    {
        _sut = new WishlistService(_wishlistRepositoryMock.Object, _productRepositoryMock.Object);
    }

    [Fact]
    public async Task AddAsync_UnknownProduct_ReturnsFailure()
    {
        var productId = Guid.NewGuid();
        _productRepositoryMock.Setup(x => x.GetByIdAsync(productId)).ReturnsAsync((Product?)null);

        var result = await _sut.AddAsync("user-1", productId);

        result.Succeeded.Should().BeFalse();
        _wishlistRepositoryMock.Verify(x => x.AddItemAsync(It.IsAny<WishlistItem>()), Times.Never);
    }

    [Fact]
    public async Task AddAsync_NewProduct_AddsItem()
    {
        var userId = "user-1";
        var productId = Guid.NewGuid();
        var product = new Product { Id = productId, Name = "iPhone" };
        var wishlist = new Domain.Entities.Wishlist { Id = Guid.NewGuid(), UserId = userId };

        _productRepositoryMock.Setup(x => x.GetByIdAsync(productId)).ReturnsAsync(product);
        _wishlistRepositoryMock.Setup(x => x.GetOrCreateAsync(userId)).ReturnsAsync(wishlist);
        _wishlistRepositoryMock.Setup(x => x.GetItemAsync(wishlist.Id, productId)).ReturnsAsync((WishlistItem?)null);
        _wishlistRepositoryMock.Setup(x => x.GetByUserIdAsync(userId)).ReturnsAsync(wishlist);

        var result = await _sut.AddAsync(userId, productId);

        result.Succeeded.Should().BeTrue();
        _wishlistRepositoryMock.Verify(
            x => x.AddItemAsync(It.Is<WishlistItem>(i => i.WishlistId == wishlist.Id && i.ProductId == productId)),
            Times.Once);
    }

    [Fact]
    public async Task AddAsync_AlreadyInWishlist_DoesNotAddDuplicate()
    {
        var userId = "user-1";
        var productId = Guid.NewGuid();
        var product = new Product { Id = productId, Name = "iPhone" };
        var wishlist = new Domain.Entities.Wishlist { Id = Guid.NewGuid(), UserId = userId };
        var existingItem = new WishlistItem { WishlistId = wishlist.Id, ProductId = productId };

        _productRepositoryMock.Setup(x => x.GetByIdAsync(productId)).ReturnsAsync(product);
        _wishlistRepositoryMock.Setup(x => x.GetOrCreateAsync(userId)).ReturnsAsync(wishlist);
        _wishlistRepositoryMock.Setup(x => x.GetItemAsync(wishlist.Id, productId)).ReturnsAsync(existingItem);
        _wishlistRepositoryMock.Setup(x => x.GetByUserIdAsync(userId)).ReturnsAsync(wishlist);

        var result = await _sut.AddAsync(userId, productId);

        result.Succeeded.Should().BeTrue();
        _wishlistRepositoryMock.Verify(x => x.AddItemAsync(It.IsAny<WishlistItem>()), Times.Never);
    }

    [Fact]
    public async Task RemoveAsync_ItemNotInWishlist_ReturnsFailure()
    {
        var userId = "user-1";
        var wishlist = new Domain.Entities.Wishlist { Id = Guid.NewGuid(), UserId = userId };
        _wishlistRepositoryMock.Setup(x => x.GetByUserIdAsync(userId)).ReturnsAsync(wishlist);

        var result = await _sut.RemoveAsync(userId, Guid.NewGuid());

        result.Succeeded.Should().BeFalse();
        _wishlistRepositoryMock.Verify(x => x.RemoveItemAsync(It.IsAny<WishlistItem>()), Times.Never);
    }

    [Fact]
    public async Task RemoveAsync_ValidProductId_RemovesItem()
    {
        var userId = "user-1";
        var productId = Guid.NewGuid();
        var wishlist = new Domain.Entities.Wishlist { Id = Guid.NewGuid(), UserId = userId };
        var item = new WishlistItem { WishlistId = wishlist.Id, ProductId = productId };
        wishlist.Items.Add(item);

        _wishlistRepositoryMock.Setup(x => x.GetByUserIdAsync(userId)).ReturnsAsync(wishlist);
        _wishlistRepositoryMock.Setup(x => x.RemoveItemAsync(item)).Returns(Task.CompletedTask);

        var result = await _sut.RemoveAsync(userId, productId);

        result.Succeeded.Should().BeTrue();
        _wishlistRepositoryMock.Verify(x => x.RemoveItemAsync(item), Times.Once);
    }
}
