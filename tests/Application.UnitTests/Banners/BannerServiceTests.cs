using FluentAssertions;
using Moq;
using HungStore.Application.Banners;
using HungStore.Application.Banners.Dtos;
using HungStore.Application.Products.Interfaces;
using HungStore.Domain.Entities;
using HungStore.Domain.Interfaces;

namespace HungStore.Application.UnitTests.Banners;

public class BannerServiceTests
{
    private readonly Mock<IBannerRepository> _bannerRepositoryMock = new();
    private readonly Mock<IFileStorageService> _fileStorageServiceMock = new();
    private readonly BannerService _sut;

    public BannerServiceTests()
    {
        _sut = new BannerService(_bannerRepositoryMock.Object, _fileStorageServiceMock.Object);
    }

    [Fact]
    public async Task CreateAsync_Always_ReturnsSuccessWithMappedFields()
    {
        var request = new CreateBannerRequest
        {
            Title = "Flash Sale",
            Subtitle = "Giá tốt",
            LinkUrl = "/products",
            DisplayOrder = 1,
            IsActive = true
        };
        _bannerRepositoryMock.Setup(x => x.AddAsync(It.IsAny<Banner>())).Returns(Task.CompletedTask);

        var result = await _sut.CreateAsync(request);

        result.Succeeded.Should().BeTrue();
        result.Data!.Title.Should().Be("Flash Sale");
        result.Data!.LinkUrl.Should().Be("/products");
    }

    [Fact]
    public async Task GetActiveAsync_DelegatesToRepositoryActiveQuery()
    {
        var banners = new List<Banner>
        {
            new() { Title = "A", LinkUrl = "/products", IsActive = true, DisplayOrder = 0 }
        };
        _bannerRepositoryMock.Setup(x => x.GetActiveAsync()).ReturnsAsync(banners);

        var result = await _sut.GetActiveAsync();

        result.Should().ContainSingle(b => b.Title == "A");
        _bannerRepositoryMock.Verify(x => x.GetActiveAsync(), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WithUnknownId_ReturnsFailure()
    {
        var id = Guid.NewGuid();
        _bannerRepositoryMock.Setup(x => x.GetByIdAsync(id)).ReturnsAsync((Banner?)null);

        var result = await _sut.UpdateAsync(id, new UpdateBannerRequest { Title = "X", LinkUrl = "/products" });

        result.Succeeded.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteAsync_WithUnknownId_ReturnsFailure()
    {
        var id = Guid.NewGuid();
        _bannerRepositoryMock.Setup(x => x.GetByIdAsync(id)).ReturnsAsync((Banner?)null);

        var result = await _sut.DeleteAsync(id);

        result.Succeeded.Should().BeFalse();
        _bannerRepositoryMock.Verify(x => x.DeleteAsync(It.IsAny<Banner>()), Times.Never);
    }

    [Fact]
    public async Task UpdateImageAsync_WithExistingBanner_SavesImageAndUpdatesUrl()
    {
        var id = Guid.NewGuid();
        var banner = new Banner { Id = id, Title = "Promo", LinkUrl = "/products" };
        using var content = new MemoryStream();
        _bannerRepositoryMock.Setup(x => x.GetByIdAsync(id)).ReturnsAsync(banner);
        _fileStorageServiceMock.Setup(x => x.SaveBannerImageAsync(content, "image/png")).ReturnsAsync("/uploads/banners/new.png");
        _bannerRepositoryMock.Setup(x => x.UpdateAsync(banner)).Returns(Task.CompletedTask);

        var result = await _sut.UpdateImageAsync(id, content, "image/png");

        result.Succeeded.Should().BeTrue();
        result.Data!.ImageUrl.Should().Be("/uploads/banners/new.png");
    }
}
