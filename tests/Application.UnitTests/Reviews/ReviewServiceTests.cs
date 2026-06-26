using FluentAssertions;
using Moq;
using HungStore.Application.Auth.Dtos;
using HungStore.Application.Auth.Interfaces;
using HungStore.Application.Reviews;
using HungStore.Application.Reviews.Dtos;
using HungStore.Domain.Entities;
using HungStore.Domain.Interfaces;

namespace HungStore.Application.UnitTests.Reviews;

public class ReviewServiceTests
{
    private readonly Mock<IReviewRepository> _reviewRepositoryMock = new();
    private readonly Mock<IProductRepository> _productRepositoryMock = new();
    private readonly Mock<IOrderRepository> _orderRepositoryMock = new();
    private readonly Mock<IIdentityService> _identityServiceMock = new();
    private readonly ReviewService _sut;

    public ReviewServiceTests()
    {
        _sut = new ReviewService(
            _reviewRepositoryMock.Object,
            _productRepositoryMock.Object,
            _orderRepositoryMock.Object,
            _identityServiceMock.Object);
    }

    private static readonly CreateReviewRequest DefaultRequest = new() { Rating = 5, Comment = "Sản phẩm rất tốt." };

    [Fact]
    public async Task CreateAsync_UnknownProduct_ReturnsFailure()
    {
        var productId = Guid.NewGuid();
        _productRepositoryMock.Setup(x => x.GetByIdAsync(productId)).ReturnsAsync((Product?)null);

        var result = await _sut.CreateAsync("user-1", productId, DefaultRequest);

        result.Succeeded.Should().BeFalse();
        _reviewRepositoryMock.Verify(x => x.AddAsync(It.IsAny<Review>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_NotPurchased_ReturnsFailure()
    {
        var productId = Guid.NewGuid();
        _productRepositoryMock.Setup(x => x.GetByIdAsync(productId)).ReturnsAsync(new Product { Id = productId });
        _orderRepositoryMock.Setup(x => x.HasDeliveredOrderForProductAsync("user-1", productId)).ReturnsAsync(false);

        var result = await _sut.CreateAsync("user-1", productId, DefaultRequest);

        result.Succeeded.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("mua"));
        _reviewRepositoryMock.Verify(x => x.AddAsync(It.IsAny<Review>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_AlreadyReviewed_ReturnsFailure()
    {
        var productId = Guid.NewGuid();
        _productRepositoryMock.Setup(x => x.GetByIdAsync(productId)).ReturnsAsync(new Product { Id = productId });
        _orderRepositoryMock.Setup(x => x.HasDeliveredOrderForProductAsync("user-1", productId)).ReturnsAsync(true);
        _reviewRepositoryMock.Setup(x => x.GetByProductAndUserAsync(productId, "user-1"))
            .ReturnsAsync(new Review { ProductId = productId, UserId = "user-1" });

        var result = await _sut.CreateAsync("user-1", productId, DefaultRequest);

        result.Succeeded.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("đã đánh giá"));
        _reviewRepositoryMock.Verify(x => x.AddAsync(It.IsAny<Review>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_Valid_SnapshotsReviewerNameAndSucceeds()
    {
        var productId = Guid.NewGuid();
        _productRepositoryMock.Setup(x => x.GetByIdAsync(productId)).ReturnsAsync(new Product { Id = productId });
        _orderRepositoryMock.Setup(x => x.HasDeliveredOrderForProductAsync("user-1", productId)).ReturnsAsync(true);
        _reviewRepositoryMock.Setup(x => x.GetByProductAndUserAsync(productId, "user-1")).ReturnsAsync((Review?)null);
        _identityServiceMock.Setup(x => x.GetUserByIdAsync("user-1")).ReturnsAsync(new UserDto { Id = "user-1", FullName = "Nguyễn Văn A" });
        _reviewRepositoryMock.Setup(x => x.AddAsync(It.IsAny<Review>())).Returns(Task.CompletedTask);

        var result = await _sut.CreateAsync("user-1", productId, DefaultRequest);

        result.Succeeded.Should().BeTrue();
        result.Data!.UserName.Should().Be("Nguyễn Văn A");
        result.Data!.Rating.Should().Be(5);
        _reviewRepositoryMock.Verify(x => x.AddAsync(It.Is<Review>(r => r.ProductId == productId && r.UserId == "user-1")), Times.Once);
    }

    [Fact]
    public async Task GetByProductIdAsync_MapsItemsAndComputesTotalPages()
    {
        var productId = Guid.NewGuid();
        var reviews = new List<Review>
        {
            new() { ProductId = productId, UserId = "user-1", UserName = "A", Rating = 5, Comment = "Tốt" },
            new() { ProductId = productId, UserId = "user-2", UserName = "B", Rating = 4, Comment = "Khá tốt" }
        };
        _reviewRepositoryMock.Setup(x => x.GetByProductIdPagedAsync(productId, 1, 10)).ReturnsAsync((reviews, 2));

        var result = await _sut.GetByProductIdAsync(productId, 1, 10);

        result.Items.Should().HaveCount(2);
        result.TotalCount.Should().Be(2);
    }

    [Fact]
    public async Task UpdateAsync_NotOwner_ReturnsFailure()
    {
        var review = new Review { Id = Guid.NewGuid(), UserId = "owner" };
        _reviewRepositoryMock.Setup(x => x.GetByIdAsync(review.Id)).ReturnsAsync(review);

        var result = await _sut.UpdateAsync("someone-else", review.Id, new UpdateReviewRequest { Rating = 3, Comment = "Sửa lại." });

        result.Succeeded.Should().BeFalse();
        _reviewRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<Review>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_Owner_UpdatesRatingAndComment()
    {
        var review = new Review { Id = Guid.NewGuid(), UserId = "owner", Rating = 5, Comment = "Tốt" };
        _reviewRepositoryMock.Setup(x => x.GetByIdAsync(review.Id)).ReturnsAsync(review);
        _reviewRepositoryMock.Setup(x => x.UpdateAsync(It.IsAny<Review>())).Returns(Task.CompletedTask);

        var result = await _sut.UpdateAsync("owner", review.Id, new UpdateReviewRequest { Rating = 3, Comment = "Sửa lại, dùng vài hôm thấy bình thường." });

        result.Succeeded.Should().BeTrue();
        result.Data!.Rating.Should().Be(3);
        result.Data!.Comment.Should().Be("Sửa lại, dùng vài hôm thấy bình thường.");
    }

    [Fact]
    public async Task DeleteAsync_NotOwner_ReturnsFailure()
    {
        var review = new Review { Id = Guid.NewGuid(), UserId = "owner" };
        _reviewRepositoryMock.Setup(x => x.GetByIdAsync(review.Id)).ReturnsAsync(review);

        var result = await _sut.DeleteAsync("someone-else", review.Id);

        result.Succeeded.Should().BeFalse();
        _reviewRepositoryMock.Verify(x => x.DeleteAsync(It.IsAny<Review>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_Owner_Succeeds()
    {
        var review = new Review { Id = Guid.NewGuid(), UserId = "owner" };
        _reviewRepositoryMock.Setup(x => x.GetByIdAsync(review.Id)).ReturnsAsync(review);
        _reviewRepositoryMock.Setup(x => x.DeleteAsync(review)).Returns(Task.CompletedTask);

        var result = await _sut.DeleteAsync("owner", review.Id);

        result.Succeeded.Should().BeTrue();
        _reviewRepositoryMock.Verify(x => x.DeleteAsync(review), Times.Once);
    }
}
