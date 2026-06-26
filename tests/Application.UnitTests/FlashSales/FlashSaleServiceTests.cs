using FluentAssertions;
using Moq;
using HungStore.Application.FlashSales;
using HungStore.Application.FlashSales.Dtos;
using HungStore.Domain.Entities;
using HungStore.Domain.Interfaces;

namespace HungStore.Application.UnitTests.FlashSales;

public class FlashSaleServiceTests
{
    private readonly Mock<IFlashSaleRepository> _flashSaleRepositoryMock = new();
    private readonly Mock<IProductRepository> _productRepositoryMock = new();
    private readonly Mock<IReviewRepository> _reviewRepositoryMock = new();
    private readonly FlashSaleService _sut;

    public FlashSaleServiceTests()
    {
        _reviewRepositoryMock.Setup(x => x.GetRatingSummaryAsync(It.IsAny<Guid>())).ReturnsAsync((0, 0));
        _sut = new FlashSaleService(_flashSaleRepositoryMock.Object, _productRepositoryMock.Object, _reviewRepositoryMock.Object);
    }

    private static Product SomeProduct(decimal price = 100) => new()
    {
        Id = Guid.NewGuid(),
        Name = "Sản phẩm A",
        Price = price,
        Stock = 50
    };

    private static FlashSale SomeFlashSale() => new()
    {
        Id = Guid.NewGuid(),
        Name = "Flash Sale Giữa Tháng",
        StartsAt = DateTime.UtcNow.AddHours(-1),
        EndsAt = DateTime.UtcNow.AddHours(1),
        IsActive = true,
        Items = new List<FlashSaleItem>()
    };

    [Fact]
    public async Task GetActiveFlashSaleAsync_NoActiveSale_ReturnsNull()
    {
        _flashSaleRepositoryMock.Setup(x => x.GetActiveAsync(It.IsAny<DateTime>())).ReturnsAsync((FlashSale?)null);

        var result = await _sut.GetActiveFlashSaleAsync();

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetActiveFlashSaleAsync_HasActiveSale_MapsItems()
    {
        var product = SomeProduct(200);
        var flashSale = SomeFlashSale();
        flashSale.Items.Add(new FlashSaleItem
        {
            Id = Guid.NewGuid(),
            FlashSaleId = flashSale.Id,
            ProductId = product.Id,
            Product = product,
            SalePrice = 150,
            QuantityLimit = 10,
            QuantitySold = 4
        });
        _flashSaleRepositoryMock.Setup(x => x.GetActiveAsync(It.IsAny<DateTime>())).ReturnsAsync(flashSale);
        _reviewRepositoryMock.Setup(x => x.GetRatingSummaryAsync(product.Id)).ReturnsAsync((4.5, 12));

        var result = await _sut.GetActiveFlashSaleAsync();

        result.Should().NotBeNull();
        result!.Items.Should().HaveCount(1);
        result.Items[0].SalePrice.Should().Be(150);
        result.Items[0].OriginalPrice.Should().Be(200);
        result.Items[0].SoldPercentage.Should().Be(40);
        result.Items[0].AverageRating.Should().Be(4.5);
        result.Items[0].ReviewCount.Should().Be(12);
    }

    [Fact]
    public async Task CreateAsync_EndsBeforeStarts_ReturnsFailure()
    {
        var request = new CreateFlashSaleRequest
        {
            Name = "Test",
            StartsAt = DateTime.UtcNow.AddHours(2),
            EndsAt = DateTime.UtcNow
        };

        var result = await _sut.CreateAsync(request);

        result.Succeeded.Should().BeFalse();
    }

    [Fact]
    public async Task CreateAsync_ValidRequest_Succeeds()
    {
        var request = new CreateFlashSaleRequest
        {
            Name = "Test",
            StartsAt = DateTime.UtcNow,
            EndsAt = DateTime.UtcNow.AddHours(2),
            IsActive = true
        };

        var result = await _sut.CreateAsync(request);

        result.Succeeded.Should().BeTrue();
        _flashSaleRepositoryMock.Verify(x => x.AddAsync(It.IsAny<FlashSale>()), Times.Once);
    }

    [Fact]
    public async Task AddItemAsync_FlashSaleNotFound_ReturnsFailure()
    {
        _flashSaleRepositoryMock.Setup(x => x.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((FlashSale?)null);

        var result = await _sut.AddItemAsync(Guid.NewGuid(), new AddFlashSaleItemRequest { ProductId = Guid.NewGuid(), SalePrice = 10, QuantityLimit = 5 });

        result.Succeeded.Should().BeFalse();
    }

    [Fact]
    public async Task AddItemAsync_ProductNotFound_ReturnsFailure()
    {
        var flashSale = SomeFlashSale();
        _flashSaleRepositoryMock.Setup(x => x.GetByIdAsync(flashSale.Id)).ReturnsAsync(flashSale);
        _productRepositoryMock.Setup(x => x.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Product?)null);

        var result = await _sut.AddItemAsync(flashSale.Id, new AddFlashSaleItemRequest { ProductId = Guid.NewGuid(), SalePrice = 10, QuantityLimit = 5 });

        result.Succeeded.Should().BeFalse();
    }

    [Fact]
    public async Task AddItemAsync_SalePriceNotLowerThanOriginal_ReturnsFailure()
    {
        var product = SomeProduct(100);
        var flashSale = SomeFlashSale();
        _flashSaleRepositoryMock.Setup(x => x.GetByIdAsync(flashSale.Id)).ReturnsAsync(flashSale);
        _productRepositoryMock.Setup(x => x.GetByIdAsync(product.Id)).ReturnsAsync(product);

        var result = await _sut.AddItemAsync(flashSale.Id, new AddFlashSaleItemRequest { ProductId = product.Id, SalePrice = 100, QuantityLimit = 5 });

        result.Succeeded.Should().BeFalse();
    }

    [Fact]
    public async Task AddItemAsync_ProductAlreadyInSale_ReturnsFailure()
    {
        var product = SomeProduct(100);
        var flashSale = SomeFlashSale();
        flashSale.Items.Add(new FlashSaleItem { ProductId = product.Id, Product = product, SalePrice = 80, QuantityLimit = 5 });
        _flashSaleRepositoryMock.Setup(x => x.GetByIdAsync(flashSale.Id)).ReturnsAsync(flashSale);
        _productRepositoryMock.Setup(x => x.GetByIdAsync(product.Id)).ReturnsAsync(product);

        var result = await _sut.AddItemAsync(flashSale.Id, new AddFlashSaleItemRequest { ProductId = product.Id, SalePrice = 50, QuantityLimit = 5 });

        result.Succeeded.Should().BeFalse();
    }

    [Fact]
    public async Task AddItemAsync_ValidRequest_Succeeds()
    {
        var product = SomeProduct(100);
        var flashSale = SomeFlashSale();
        _flashSaleRepositoryMock.Setup(x => x.GetByIdAsync(flashSale.Id)).ReturnsAsync(flashSale);
        _productRepositoryMock.Setup(x => x.GetByIdAsync(product.Id)).ReturnsAsync(product);

        var result = await _sut.AddItemAsync(flashSale.Id, new AddFlashSaleItemRequest { ProductId = product.Id, SalePrice = 80, QuantityLimit = 5 });

        result.Succeeded.Should().BeTrue();
        _flashSaleRepositoryMock.Verify(x => x.AddItemAsync(It.IsAny<FlashSaleItem>()), Times.Once);
    }

    [Fact]
    public async Task UpdateItemAsync_ItemNotFound_ReturnsFailure()
    {
        var flashSale = SomeFlashSale();
        _flashSaleRepositoryMock.Setup(x => x.GetByIdAsync(flashSale.Id)).ReturnsAsync(flashSale);
        _flashSaleRepositoryMock.Setup(x => x.GetItemAsync(flashSale.Id, It.IsAny<Guid>())).ReturnsAsync((FlashSaleItem?)null);

        var result = await _sut.UpdateItemAsync(flashSale.Id, Guid.NewGuid(), new UpdateFlashSaleItemRequest { SalePrice = 10, QuantityLimit = 5 });

        result.Succeeded.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateItemAsync_ValidRequest_UpdatesValues()
    {
        var product = SomeProduct(100);
        var flashSale = SomeFlashSale();
        var item = new FlashSaleItem { Id = Guid.NewGuid(), FlashSaleId = flashSale.Id, ProductId = product.Id, Product = product, SalePrice = 80, QuantityLimit = 5 };
        flashSale.Items.Add(item);
        _flashSaleRepositoryMock.Setup(x => x.GetByIdAsync(flashSale.Id)).ReturnsAsync(flashSale);
        _flashSaleRepositoryMock.Setup(x => x.GetItemAsync(flashSale.Id, item.Id)).ReturnsAsync(item);

        var result = await _sut.UpdateItemAsync(flashSale.Id, item.Id, new UpdateFlashSaleItemRequest { SalePrice = 70, QuantityLimit = 8 });

        result.Succeeded.Should().BeTrue();
        item.SalePrice.Should().Be(70);
        item.QuantityLimit.Should().Be(8);
    }

    [Fact]
    public async Task DeleteItemAsync_ValidRequest_RemovesItem()
    {
        var product = SomeProduct(100);
        var flashSale = SomeFlashSale();
        var item = new FlashSaleItem { Id = Guid.NewGuid(), FlashSaleId = flashSale.Id, ProductId = product.Id, Product = product, SalePrice = 80, QuantityLimit = 5 };
        flashSale.Items.Add(item);
        _flashSaleRepositoryMock.Setup(x => x.GetByIdAsync(flashSale.Id)).ReturnsAsync(flashSale);
        _flashSaleRepositoryMock.Setup(x => x.GetItemAsync(flashSale.Id, item.Id)).ReturnsAsync(item);

        var result = await _sut.DeleteItemAsync(flashSale.Id, item.Id);

        result.Succeeded.Should().BeTrue();
        result.Data!.Items.Should().BeEmpty();
        _flashSaleRepositoryMock.Verify(x => x.DeleteItemAsync(item), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_FlashSaleNotFound_ReturnsFailure()
    {
        _flashSaleRepositoryMock.Setup(x => x.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((FlashSale?)null);

        var result = await _sut.DeleteAsync(Guid.NewGuid());

        result.Succeeded.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteAsync_ValidRequest_Succeeds()
    {
        var flashSale = SomeFlashSale();
        _flashSaleRepositoryMock.Setup(x => x.GetByIdAsync(flashSale.Id)).ReturnsAsync(flashSale);

        var result = await _sut.DeleteAsync(flashSale.Id);

        result.Succeeded.Should().BeTrue();
        _flashSaleRepositoryMock.Verify(x => x.DeleteAsync(flashSale), Times.Once);
    }
}
