using FluentAssertions;
using Moq;
using HungStore.Application.Products;
using HungStore.Application.Products.Dtos;
using HungStore.Application.Products.Interfaces;
using HungStore.Domain.Entities;
using HungStore.Domain.Interfaces;

namespace HungStore.Application.UnitTests.Products;

public class ProductServiceTests
{
    private readonly Mock<IProductRepository> _productRepositoryMock = new();
    private readonly Mock<ICategoryRepository> _categoryRepositoryMock = new();
    private readonly Mock<IFileStorageService> _fileStorageServiceMock = new();
    private readonly Mock<IReviewRepository> _reviewRepositoryMock = new();
    private readonly Mock<IFlashSaleRepository> _flashSaleRepositoryMock = new();
    private readonly Mock<IOrderRepository> _orderRepositoryMock = new();
    private readonly ProductService _sut;

    public ProductServiceTests()
    {
        _flashSaleRepositoryMock
            .Setup(x => x.GetActiveItemsForProductsAsync(It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<DateTime>()))
            .ReturnsAsync(new Dictionary<Guid, FlashSaleItem>());
        _reviewRepositoryMock
            .Setup(x => x.GetRatingSummariesAsync(It.IsAny<IReadOnlyCollection<Guid>>()))
            .ReturnsAsync(new Dictionary<Guid, (double AverageRating, int ReviewCount)>());
        _orderRepositoryMock
            .Setup(x => x.GetSoldCountsAsync(It.IsAny<IReadOnlyCollection<Guid>>()))
            .ReturnsAsync(new Dictionary<Guid, int>());

        _sut = new ProductService(_productRepositoryMock.Object, _categoryRepositoryMock.Object, _fileStorageServiceMock.Object, _reviewRepositoryMock.Object, _flashSaleRepositoryMock.Object, _orderRepositoryMock.Object);
    }

    [Fact]
    public async Task GetPagedAsync_MapsItemsAndComputesTotalPages()
    {
        var categoryId = Guid.NewGuid();
        var category = new Category { Id = categoryId, Name = "Phones" };
        var products = new List<Product>
        {
            new() { Name = "Product A", CategoryId = categoryId, Category = category, Price = 100 },
            new() { Name = "Product B", CategoryId = categoryId, Category = category, Price = 200 }
        };

        var filter = new ProductFilterRequest { Page = 1, PageSize = 2 };
        _productRepositoryMock
            .Setup(x => x.GetPagedAsync(filter.Keyword, filter.CategoryId, filter.MinPrice, filter.MaxPrice, filter.SortBy, filter.Page, filter.PageSize))
            .ReturnsAsync((products, 5));

        var result = await _sut.GetPagedAsync(filter);

        result.Items.Should().HaveCount(2);
        result.Items[0].CategoryName.Should().Be("Phones");
        result.TotalCount.Should().Be(5);
        result.TotalPages.Should().Be(3);
    }

    [Fact]
    public async Task GetPagedAsync_DecoratesItemsWithRatingAndSoldCount()
    {
        var categoryId = Guid.NewGuid();
        var category = new Category { Id = categoryId, Name = "Phones" };
        var productA = new Product { Id = Guid.NewGuid(), Name = "Product A", CategoryId = categoryId, Category = category, Price = 100 };
        var productB = new Product { Id = Guid.NewGuid(), Name = "Product B", CategoryId = categoryId, Category = category, Price = 200 };

        var filter = new ProductFilterRequest { Page = 1, PageSize = 2 };
        _productRepositoryMock
            .Setup(x => x.GetPagedAsync(filter.Keyword, filter.CategoryId, filter.MinPrice, filter.MaxPrice, filter.SortBy, filter.Page, filter.PageSize))
            .ReturnsAsync((new List<Product> { productA, productB }, 2));
        _reviewRepositoryMock
            .Setup(x => x.GetRatingSummariesAsync(It.IsAny<IReadOnlyCollection<Guid>>()))
            .ReturnsAsync(new Dictionary<Guid, (double AverageRating, int ReviewCount)> { [productA.Id] = (4.0, 10) });
        _orderRepositoryMock
            .Setup(x => x.GetSoldCountsAsync(It.IsAny<IReadOnlyCollection<Guid>>()))
            .ReturnsAsync(new Dictionary<Guid, int> { [productA.Id] = 25 });

        var result = await _sut.GetPagedAsync(filter);

        var dtoA = result.Items.Single(i => i.Id == productA.Id);
        var dtoB = result.Items.Single(i => i.Id == productB.Id);
        dtoA.AverageRating.Should().Be(4.0);
        dtoA.ReviewCount.Should().Be(10);
        dtoA.SoldCount.Should().Be(25);
        dtoB.SoldCount.Should().Be(0);
    }

    [Fact]
    public async Task GetByIdAsync_IncludesAverageRatingAndSoldCount()
    {
        var categoryId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var product = new Product { Id = productId, Name = "Product A", CategoryId = categoryId, Category = new Category { Id = categoryId, Name = "Phones" } };
        _productRepositoryMock.Setup(x => x.GetByIdAsync(productId)).ReturnsAsync(product);
        _reviewRepositoryMock
            .Setup(x => x.GetRatingSummariesAsync(It.IsAny<IReadOnlyCollection<Guid>>()))
            .ReturnsAsync(new Dictionary<Guid, (double AverageRating, int ReviewCount)> { [productId] = (4.5, 2) });
        _orderRepositoryMock
            .Setup(x => x.GetSoldCountsAsync(It.IsAny<IReadOnlyCollection<Guid>>()))
            .ReturnsAsync(new Dictionary<Guid, int> { [productId] = 37 });

        var result = await _sut.GetByIdAsync(productId);

        result.Succeeded.Should().BeTrue();
        result.Data!.AverageRating.Should().Be(4.5);
        result.Data!.ReviewCount.Should().Be(2);
        result.Data!.SoldCount.Should().Be(37);
    }

    [Fact]
    public async Task GetByIdAsync_ProductInActiveFlashSale_DecoratesFlashSaleFields()
    {
        var categoryId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var product = new Product { Id = productId, Name = "Product A", Price = 1000, CategoryId = categoryId, Category = new Category { Id = categoryId, Name = "Phones" } };
        var flashSale = new FlashSale { Id = Guid.NewGuid(), Name = "Sale", StartsAt = DateTime.UtcNow.AddHours(-1), EndsAt = DateTime.UtcNow.AddHours(1) };
        var flashSaleItem = new FlashSaleItem { ProductId = productId, FlashSale = flashSale, SalePrice = 700, QuantityLimit = 10, QuantitySold = 3 };
        _productRepositoryMock.Setup(x => x.GetByIdAsync(productId)).ReturnsAsync(product);
        _flashSaleRepositoryMock
            .Setup(x => x.GetActiveItemsForProductsAsync(It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<DateTime>()))
            .ReturnsAsync(new Dictionary<Guid, FlashSaleItem> { [productId] = flashSaleItem });

        var result = await _sut.GetByIdAsync(productId);

        result.Succeeded.Should().BeTrue();
        result.Data!.FlashSalePrice.Should().Be(700);
        result.Data!.FlashSaleQuantityRemaining.Should().Be(7);
        result.Data!.FlashSaleEndsAt.Should().Be(flashSale.EndsAt);
    }

    [Fact]
    public async Task CreateAsync_WithUnknownCategory_ReturnsFailure()
    {
        var request = new CreateProductRequest { Name = "New Product", Price = 10, CategoryId = Guid.NewGuid() };
        _categoryRepositoryMock.Setup(x => x.GetByIdAsync(request.CategoryId)).ReturnsAsync((Category?)null);

        var result = await _sut.CreateAsync(request);

        result.Succeeded.Should().BeFalse();
    }

    [Fact]
    public async Task CreateAsync_WithValidCategory_ReturnsSuccess()
    {
        var categoryId = Guid.NewGuid();
        var category = new Category { Id = categoryId, Name = "Phones" };
        var request = new CreateProductRequest { Name = "New Product", Price = 10, Stock = 5, CategoryId = categoryId };

        _categoryRepositoryMock.Setup(x => x.GetByIdAsync(categoryId)).ReturnsAsync(category);
        _productRepositoryMock.Setup(x => x.AddAsync(It.IsAny<Product>())).Returns(Task.CompletedTask);

        var result = await _sut.CreateAsync(request);

        result.Succeeded.Should().BeTrue();
        result.Data!.CategoryName.Should().Be("Phones");
    }

    [Fact]
    public async Task UpdateImageAsync_WithUnknownProduct_ReturnsFailure()
    {
        var id = Guid.NewGuid();
        _productRepositoryMock.Setup(x => x.GetByIdAsync(id)).ReturnsAsync((Product?)null);

        var result = await _sut.UpdateImageAsync(id, Stream.Null, "image/png");

        result.Succeeded.Should().BeFalse();
        _fileStorageServiceMock.Verify(x => x.SaveProductImageAsync(It.IsAny<Stream>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task AddVariantOptionAsync_ThirdOption_ReturnsFailure()
    {
        var productId = Guid.NewGuid();
        var product = new Product
        {
            Id = productId,
            VariantOptions = new List<ProductVariantOption>
            {
                new() { ProductId = productId, Name = "Màu sắc", DisplayOrder = 1 },
                new() { ProductId = productId, Name = "Dung lượng", DisplayOrder = 2 }
            }
        };
        _productRepositoryMock.Setup(x => x.GetByIdAsync(productId)).ReturnsAsync(product);

        var result = await _sut.AddVariantOptionAsync(
            productId,
            new CreateVariantOptionRequest { Name = "Phiên bản", Values = new List<CreateVariantOptionValueRequest> { new() { Value = "A" } } });

        result.Succeeded.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("tối đa 2"));
        _productRepositoryMock.Verify(x => x.AddVariantOptionAsync(It.IsAny<ProductVariantOption>()), Times.Never);
    }

    [Fact]
    public async Task AddVariantOptionAsync_DuplicateName_ReturnsFailure()
    {
        var productId = Guid.NewGuid();
        var product = new Product
        {
            Id = productId,
            VariantOptions = new List<ProductVariantOption> { new() { ProductId = productId, Name = "Màu sắc", DisplayOrder = 1 } }
        };
        _productRepositoryMock.Setup(x => x.GetByIdAsync(productId)).ReturnsAsync(product);

        var result = await _sut.AddVariantOptionAsync(
            productId,
            new CreateVariantOptionRequest { Name = "màu sắc", Values = new List<CreateVariantOptionValueRequest> { new() { Value = "Đen" } } });

        result.Succeeded.Should().BeFalse();
    }

    [Fact]
    public async Task AddVariantOptionAsync_Valid_CreatesOptionWithDisplayOrder()
    {
        var productId = Guid.NewGuid();
        var product = new Product { Id = productId };
        _productRepositoryMock.Setup(x => x.GetByIdAsync(productId)).ReturnsAsync(product);
        _productRepositoryMock.Setup(x => x.AddVariantOptionAsync(It.IsAny<ProductVariantOption>())).Returns(Task.CompletedTask);

        var result = await _sut.AddVariantOptionAsync(
            productId,
            new CreateVariantOptionRequest
            {
                Name = "Màu sắc",
                Values = new List<CreateVariantOptionValueRequest> { new() { Value = "Đen" }, new() { Value = "Trắng" } }
            });

        result.Succeeded.Should().BeTrue();
        _productRepositoryMock.Verify(
            x => x.AddVariantOptionAsync(It.Is<ProductVariantOption>(o => o.DisplayOrder == 1 && o.Values.Count == 2)),
            Times.Once);
    }

    [Fact]
    public async Task AddVariantAsync_NoOptionsYet_ReturnsFailure()
    {
        var productId = Guid.NewGuid();
        var product = new Product { Id = productId };
        _productRepositoryMock.Setup(x => x.GetByIdAsync(productId)).ReturnsAsync(product);

        var result = await _sut.AddVariantAsync(productId, new CreateProductVariantRequest { OptionValue1Id = Guid.NewGuid(), Stock = 5 });

        result.Succeeded.Should().BeFalse();
    }

    [Fact]
    public async Task AddVariantAsync_MissingSecondDimension_ReturnsFailure()
    {
        var productId = Guid.NewGuid();
        var value1 = new ProductVariantOptionValue { Id = Guid.NewGuid(), Value = "Đen" };
        var option1 = new ProductVariantOption { ProductId = productId, Name = "Màu sắc", DisplayOrder = 1, Values = new List<ProductVariantOptionValue> { value1 } };
        var option2 = new ProductVariantOption { ProductId = productId, Name = "Dung lượng", DisplayOrder = 2, Values = new List<ProductVariantOptionValue> { new() { Id = Guid.NewGuid(), Value = "128GB" } } };
        var product = new Product { Id = productId, VariantOptions = new List<ProductVariantOption> { option1, option2 } };
        _productRepositoryMock.Setup(x => x.GetByIdAsync(productId)).ReturnsAsync(product);

        var result = await _sut.AddVariantAsync(productId, new CreateProductVariantRequest { OptionValue1Id = value1.Id, Stock = 5 });

        result.Succeeded.Should().BeFalse();
    }

    [Fact]
    public async Task AddVariantAsync_DuplicateCombination_ReturnsFailure()
    {
        var productId = Guid.NewGuid();
        var value1 = new ProductVariantOptionValue { Id = Guid.NewGuid(), Value = "Đen" };
        var option1 = new ProductVariantOption { ProductId = productId, Name = "Màu sắc", DisplayOrder = 1, Values = new List<ProductVariantOptionValue> { value1 } };
        var existingVariant = new ProductVariant { ProductId = productId, OptionValue1Id = value1.Id, Stock = 5 };
        var product = new Product
        {
            Id = productId,
            VariantOptions = new List<ProductVariantOption> { option1 },
            Variants = new List<ProductVariant> { existingVariant }
        };
        _productRepositoryMock.Setup(x => x.GetByIdAsync(productId)).ReturnsAsync(product);

        var result = await _sut.AddVariantAsync(productId, new CreateProductVariantRequest { OptionValue1Id = value1.Id, Stock = 3 });

        result.Succeeded.Should().BeFalse();
        _productRepositoryMock.Verify(x => x.AddVariantAsync(It.IsAny<ProductVariant>()), Times.Never);
    }

    [Fact]
    public async Task AddVariantAsync_ValidTwoDimensionCombo_CreatesVariant()
    {
        var productId = Guid.NewGuid();
        var value1 = new ProductVariantOptionValue { Id = Guid.NewGuid(), Value = "Đen" };
        var value2 = new ProductVariantOptionValue { Id = Guid.NewGuid(), Value = "256GB" };
        var option1 = new ProductVariantOption { ProductId = productId, Name = "Màu sắc", DisplayOrder = 1, Values = new List<ProductVariantOptionValue> { value1 } };
        var option2 = new ProductVariantOption { ProductId = productId, Name = "Dung lượng", DisplayOrder = 2, Values = new List<ProductVariantOptionValue> { value2 } };
        var product = new Product { Id = productId, VariantOptions = new List<ProductVariantOption> { option1, option2 } };
        _productRepositoryMock.Setup(x => x.GetByIdAsync(productId)).ReturnsAsync(product);
        _productRepositoryMock.Setup(x => x.AddVariantAsync(It.IsAny<ProductVariant>())).Returns(Task.CompletedTask);

        var result = await _sut.AddVariantAsync(productId, new CreateProductVariantRequest { OptionValue1Id = value1.Id, OptionValue2Id = value2.Id, Stock = 7 });

        result.Succeeded.Should().BeTrue();
        _productRepositoryMock.Verify(
            x => x.AddVariantAsync(It.Is<ProductVariant>(v => v.OptionValue1Id == value1.Id && v.OptionValue2Id == value2.Id && v.Stock == 7)),
            Times.Once);
    }

    [Fact]
    public async Task DeleteVariantOptionValueAsync_InUseBySku_ReturnsFailure()
    {
        var productId = Guid.NewGuid();
        var optionId = Guid.NewGuid();
        var value = new ProductVariantOptionValue { Id = Guid.NewGuid(), ProductVariantOptionId = optionId, Value = "Đen" };
        var option = new ProductVariantOption { Id = optionId, ProductId = productId, Name = "Màu sắc", Values = new List<ProductVariantOptionValue> { value } };
        var variant = new ProductVariant { ProductId = productId, OptionValue1Id = value.Id, Stock = 1 };
        var product = new Product { Id = productId, Variants = new List<ProductVariant> { variant } };

        _productRepositoryMock.Setup(x => x.GetByIdAsync(productId)).ReturnsAsync(product);
        _productRepositoryMock.Setup(x => x.GetVariantOptionAsync(productId, optionId)).ReturnsAsync(option);

        var result = await _sut.DeleteVariantOptionValueAsync(productId, optionId, value.Id);

        result.Succeeded.Should().BeFalse();
        _productRepositoryMock.Verify(x => x.DeleteVariantOptionValueAsync(It.IsAny<ProductVariantOptionValue>()), Times.Never);
    }

    [Fact]
    public async Task DeleteVariantOptionAsync_StillHasValues_ReturnsFailure()
    {
        var productId = Guid.NewGuid();
        var optionId = Guid.NewGuid();
        var option = new ProductVariantOption
        {
            Id = optionId,
            ProductId = productId,
            Name = "Màu sắc",
            Values = new List<ProductVariantOptionValue> { new() { Value = "Đen" } }
        };
        var product = new Product { Id = productId };

        _productRepositoryMock.Setup(x => x.GetByIdAsync(productId)).ReturnsAsync(product);
        _productRepositoryMock.Setup(x => x.GetVariantOptionAsync(productId, optionId)).ReturnsAsync(option);

        var result = await _sut.DeleteVariantOptionAsync(productId, optionId);

        result.Succeeded.Should().BeFalse();
        _productRepositoryMock.Verify(x => x.DeleteVariantOptionAsync(It.IsAny<ProductVariantOption>()), Times.Never);
    }
}
