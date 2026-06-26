using FluentAssertions;
using Moq;
using HungStore.Application.Cart;
using HungStore.Application.Cart.Dtos;
using HungStore.Domain.Entities;
using HungStore.Domain.Interfaces;

namespace HungStore.Application.UnitTests.Cart;

public class CartServiceTests
{
    private readonly Mock<ICartRepository> _cartRepositoryMock = new();
    private readonly Mock<IProductRepository> _productRepositoryMock = new();
    private readonly CartService _sut;

    public CartServiceTests()
    {
        _sut = new CartService(_cartRepositoryMock.Object, _productRepositoryMock.Object);
    }

    [Fact]
    public async Task AddItemAsync_WithUnknownProduct_ReturnsFailure()
    {
        var request = new AddCartItemRequest { ProductId = Guid.NewGuid(), Quantity = 1 };
        _productRepositoryMock.Setup(x => x.GetByIdAsync(request.ProductId)).ReturnsAsync((Product?)null);

        var result = await _sut.AddItemAsync("user-1", request);

        result.Succeeded.Should().BeFalse();
        _cartRepositoryMock.Verify(x => x.AddItemAsync(It.IsAny<CartItem>()), Times.Never);
    }

    [Fact]
    public async Task AddItemAsync_QuantityExceedsStock_ReturnsFailure()
    {
        var productId = Guid.NewGuid();
        var product = new Product { Id = productId, Name = "iPhone", Stock = 3 };
        var request = new AddCartItemRequest { ProductId = productId, Quantity = 5 };

        _productRepositoryMock.Setup(x => x.GetByIdAsync(productId)).ReturnsAsync(product);
        _cartRepositoryMock.Setup(x => x.GetByUserIdAsync("user-1")).ReturnsAsync((Domain.Entities.Cart?)null);
        _cartRepositoryMock.Setup(x => x.CreateAsync("user-1")).ReturnsAsync(new Domain.Entities.Cart { Id = Guid.NewGuid(), UserId = "user-1" });
        _cartRepositoryMock.Setup(x => x.GetItemAsync(It.IsAny<Guid>(), productId, null)).ReturnsAsync((CartItem?)null);

        var result = await _sut.AddItemAsync("user-1", request);

        result.Succeeded.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("iPhone"));
        _cartRepositoryMock.Verify(x => x.AddItemAsync(It.IsAny<CartItem>()), Times.Never);
    }

    [Fact]
    public async Task AddItemAsync_NoCartYet_CreatesCartAndAddsItem()
    {
        var userId = "user-1";
        var productId = Guid.NewGuid();
        var product = new Product { Id = productId, Name = "iPhone", Stock = 10 };
        var request = new AddCartItemRequest { ProductId = productId, Quantity = 2 };
        Domain.Entities.Cart? currentCart = null;

        _productRepositoryMock.Setup(x => x.GetByIdAsync(productId)).ReturnsAsync(product);
        _cartRepositoryMock.Setup(x => x.GetByUserIdAsync(userId)).ReturnsAsync(() => currentCart);
        _cartRepositoryMock.Setup(x => x.CreateAsync(userId)).ReturnsAsync(() =>
        {
            currentCart = new Domain.Entities.Cart { Id = Guid.NewGuid(), UserId = userId, Items = new List<CartItem>() };
            return currentCart;
        });
        _cartRepositoryMock.Setup(x => x.GetItemAsync(It.IsAny<Guid>(), productId, null)).ReturnsAsync((CartItem?)null);
        _cartRepositoryMock
            .Setup(x => x.AddItemAsync(It.IsAny<CartItem>()))
            .Callback<CartItem>(item => currentCart!.Items.Add(item))
            .Returns(Task.CompletedTask);

        var result = await _sut.AddItemAsync(userId, request);

        result.Succeeded.Should().BeTrue();
        _cartRepositoryMock.Verify(x => x.CreateAsync(userId), Times.Once);
        _cartRepositoryMock.Verify(
            x => x.AddItemAsync(It.Is<CartItem>(i => i.ProductId == productId && i.Quantity == 2 && i.ProductVariantId == null)),
            Times.Once);
    }

    [Fact]
    public async Task AddItemAsync_ExistingItem_IncreasesQuantity()
    {
        var userId = "user-1";
        var productId = Guid.NewGuid();
        var product = new Product { Id = productId, Name = "iPhone", Stock = 10 };
        var cart = new Domain.Entities.Cart { Id = Guid.NewGuid(), UserId = userId };
        var existingItem = new CartItem { CartId = cart.Id, ProductId = productId, Quantity = 3 };
        cart.Items.Add(existingItem);

        _productRepositoryMock.Setup(x => x.GetByIdAsync(productId)).ReturnsAsync(product);
        _cartRepositoryMock.Setup(x => x.GetByUserIdAsync(userId)).ReturnsAsync(cart);
        _cartRepositoryMock.Setup(x => x.GetItemAsync(cart.Id, productId, null)).ReturnsAsync(existingItem);
        _cartRepositoryMock.Setup(x => x.UpdateItemAsync(It.IsAny<CartItem>())).Returns(Task.CompletedTask);

        var result = await _sut.AddItemAsync(userId, new AddCartItemRequest { ProductId = productId, Quantity = 2 });

        result.Succeeded.Should().BeTrue();
        existingItem.Quantity.Should().Be(5);
        _cartRepositoryMock.Verify(x => x.UpdateItemAsync(It.Is<CartItem>(i => i.Quantity == 5)), Times.Once);
    }

    [Fact]
    public async Task AddItemAsync_ProductWithVariants_MissingVariantId_ReturnsFailure()
    {
        var productId = Guid.NewGuid();
        var product = new Product
        {
            Id = productId,
            Name = "Smartphone Pro",
            Variants = new List<ProductVariant> { new() { Id = Guid.NewGuid(), ProductId = productId, OptionValue1 = new ProductVariantOptionValue { Value = "Đen" }, Stock = 5 } }
        };
        _productRepositoryMock.Setup(x => x.GetByIdAsync(productId)).ReturnsAsync(product);

        var result = await _sut.AddItemAsync("user-1", new AddCartItemRequest { ProductId = productId, Quantity = 1 });

        result.Succeeded.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("phân loại"));
        _cartRepositoryMock.Verify(x => x.AddItemAsync(It.IsAny<CartItem>()), Times.Never);
    }

    [Fact]
    public async Task AddItemAsync_ProductWithVariants_UnknownVariantId_ReturnsFailure()
    {
        var productId = Guid.NewGuid();
        var product = new Product
        {
            Id = productId,
            Name = "Smartphone Pro",
            Variants = new List<ProductVariant> { new() { Id = Guid.NewGuid(), ProductId = productId, OptionValue1 = new ProductVariantOptionValue { Value = "Đen" }, Stock = 5 } }
        };
        _productRepositoryMock.Setup(x => x.GetByIdAsync(productId)).ReturnsAsync(product);

        var result = await _sut.AddItemAsync(
            "user-1",
            new AddCartItemRequest { ProductId = productId, ProductVariantId = Guid.NewGuid(), Quantity = 1 });

        result.Succeeded.Should().BeFalse();
    }

    [Fact]
    public async Task AddItemAsync_ProductWithVariants_QuantityExceedsVariantStock_ReturnsFailure()
    {
        var userId = "user-1";
        var productId = Guid.NewGuid();
        var variant = new ProductVariant { Id = Guid.NewGuid(), ProductId = productId, OptionValue1 = new ProductVariantOptionValue { Value = "Đen" }, Stock = 2 };
        var product = new Product { Id = productId, Name = "Smartphone Pro", Variants = new List<ProductVariant> { variant } };
        var cart = new Domain.Entities.Cart { Id = Guid.NewGuid(), UserId = userId };

        _productRepositoryMock.Setup(x => x.GetByIdAsync(productId)).ReturnsAsync(product);
        _cartRepositoryMock.Setup(x => x.GetByUserIdAsync(userId)).ReturnsAsync(cart);
        _cartRepositoryMock.Setup(x => x.GetItemAsync(cart.Id, productId, variant.Id)).ReturnsAsync((CartItem?)null);

        var result = await _sut.AddItemAsync(
            userId,
            new AddCartItemRequest { ProductId = productId, ProductVariantId = variant.Id, Quantity = 5 });

        result.Succeeded.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("Đen"));
        _cartRepositoryMock.Verify(x => x.AddItemAsync(It.IsAny<CartItem>()), Times.Never);
    }

    [Fact]
    public async Task AddItemAsync_ProductWithVariants_ValidVariant_AddsItemWithVariantId()
    {
        var userId = "user-1";
        var productId = Guid.NewGuid();
        var variant = new ProductVariant { Id = Guid.NewGuid(), ProductId = productId, OptionValue1 = new ProductVariantOptionValue { Value = "Trắng" }, Stock = 10 };
        var product = new Product { Id = productId, Name = "Smartphone Pro", Variants = new List<ProductVariant> { variant } };
        var cart = new Domain.Entities.Cart { Id = Guid.NewGuid(), UserId = userId, Items = new List<CartItem>() };

        _productRepositoryMock.Setup(x => x.GetByIdAsync(productId)).ReturnsAsync(product);
        _cartRepositoryMock.Setup(x => x.GetByUserIdAsync(userId)).ReturnsAsync(cart);
        _cartRepositoryMock.Setup(x => x.GetItemAsync(cart.Id, productId, variant.Id)).ReturnsAsync((CartItem?)null);
        _cartRepositoryMock
            .Setup(x => x.AddItemAsync(It.IsAny<CartItem>()))
            .Callback<CartItem>(item => cart.Items.Add(item))
            .Returns(Task.CompletedTask);

        var result = await _sut.AddItemAsync(
            userId,
            new AddCartItemRequest { ProductId = productId, ProductVariantId = variant.Id, Quantity = 2 });

        result.Succeeded.Should().BeTrue();
        _cartRepositoryMock.Verify(
            x => x.AddItemAsync(It.Is<CartItem>(i => i.ProductVariantId == variant.Id && i.Quantity == 2)),
            Times.Once);
    }

    [Fact]
    public async Task AddItemAsync_TwoDimensionVariant_ExceedsStock_LabelCombinesBothOptions()
    {
        var userId = "user-1";
        var productId = Guid.NewGuid();
        var variant = new ProductVariant
        {
            Id = Guid.NewGuid(),
            ProductId = productId,
            OptionValue1 = new ProductVariantOptionValue { Value = "Đen" },
            OptionValue2 = new ProductVariantOptionValue { Value = "256GB" },
            Stock = 2
        };
        var product = new Product { Id = productId, Name = "Smartphone Pro", Variants = new List<ProductVariant> { variant } };
        var cart = new Domain.Entities.Cart { Id = Guid.NewGuid(), UserId = userId };

        _productRepositoryMock.Setup(x => x.GetByIdAsync(productId)).ReturnsAsync(product);
        _cartRepositoryMock.Setup(x => x.GetByUserIdAsync(userId)).ReturnsAsync(cart);
        _cartRepositoryMock.Setup(x => x.GetItemAsync(cart.Id, productId, variant.Id)).ReturnsAsync((CartItem?)null);

        var result = await _sut.AddItemAsync(
            userId,
            new AddCartItemRequest { ProductId = productId, ProductVariantId = variant.Id, Quantity = 5 });

        result.Succeeded.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("Đen, 256GB"));
    }

    [Fact]
    public async Task UpdateItemAsync_ItemNotInCart_ReturnsFailure()
    {
        _cartRepositoryMock.Setup(x => x.GetByUserIdAsync("user-1")).ReturnsAsync((Domain.Entities.Cart?)null);

        var result = await _sut.UpdateItemAsync("user-1", Guid.NewGuid(), new UpdateCartItemRequest { Quantity = 1 });

        result.Succeeded.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateItemAsync_ValidCartItemId_UpdatesQuantity()
    {
        var userId = "user-1";
        var productId = Guid.NewGuid();
        var product = new Product { Id = productId, Name = "iPhone", Stock = 10 };
        var cart = new Domain.Entities.Cart { Id = Guid.NewGuid(), UserId = userId };
        var item = new CartItem { Id = Guid.NewGuid(), CartId = cart.Id, ProductId = productId, Product = product, Quantity = 1 };
        cart.Items.Add(item);

        _cartRepositoryMock.Setup(x => x.GetByUserIdAsync(userId)).ReturnsAsync(cart);
        _cartRepositoryMock.Setup(x => x.UpdateItemAsync(It.IsAny<CartItem>())).Returns(Task.CompletedTask);

        var result = await _sut.UpdateItemAsync(userId, item.Id, new UpdateCartItemRequest { Quantity = 4 });

        result.Succeeded.Should().BeTrue();
        item.Quantity.Should().Be(4);
    }

    [Fact]
    public async Task UpdateItemAsync_ExceedsStock_ReturnsFailure()
    {
        var userId = "user-1";
        var productId = Guid.NewGuid();
        var product = new Product { Id = productId, Name = "iPhone", Stock = 3 };
        var cart = new Domain.Entities.Cart { Id = Guid.NewGuid(), UserId = userId };
        var item = new CartItem { Id = Guid.NewGuid(), CartId = cart.Id, ProductId = productId, Product = product, Quantity = 1 };
        cart.Items.Add(item);

        _cartRepositoryMock.Setup(x => x.GetByUserIdAsync(userId)).ReturnsAsync(cart);

        var result = await _sut.UpdateItemAsync(userId, item.Id, new UpdateCartItemRequest { Quantity = 10 });

        result.Succeeded.Should().BeFalse();
        _cartRepositoryMock.Verify(x => x.UpdateItemAsync(It.IsAny<CartItem>()), Times.Never);
    }

    [Fact]
    public async Task RemoveItemAsync_ItemNotInCart_ReturnsFailure()
    {
        var cart = new Domain.Entities.Cart { Id = Guid.NewGuid(), UserId = "user-1" };
        _cartRepositoryMock.Setup(x => x.GetByUserIdAsync("user-1")).ReturnsAsync(cart);

        var result = await _sut.RemoveItemAsync("user-1", Guid.NewGuid());

        result.Succeeded.Should().BeFalse();
        _cartRepositoryMock.Verify(x => x.RemoveItemAsync(It.IsAny<CartItem>()), Times.Never);
    }

    [Fact]
    public async Task RemoveItemAsync_ValidCartItemId_RemovesItem()
    {
        var userId = "user-1";
        var cart = new Domain.Entities.Cart { Id = Guid.NewGuid(), UserId = userId };
        var item = new CartItem { Id = Guid.NewGuid(), CartId = cart.Id, ProductId = Guid.NewGuid(), Quantity = 1 };
        cart.Items.Add(item);

        _cartRepositoryMock.Setup(x => x.GetByUserIdAsync(userId)).ReturnsAsync(cart);
        _cartRepositoryMock.Setup(x => x.RemoveItemAsync(item)).Returns(Task.CompletedTask);

        var result = await _sut.RemoveItemAsync(userId, item.Id);

        result.Succeeded.Should().BeTrue();
        _cartRepositoryMock.Verify(x => x.RemoveItemAsync(item), Times.Once);
    }
}
