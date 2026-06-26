using FluentAssertions;
using Moq;
using HungStore.Application.Vouchers;
using HungStore.Application.Vouchers.Dtos;
using HungStore.Domain.Entities;
using HungStore.Domain.Enums;
using HungStore.Domain.Interfaces;

namespace HungStore.Application.UnitTests.Vouchers;

public class VoucherServiceTests
{
    private readonly Mock<IVoucherRepository> _voucherRepositoryMock = new();
    private readonly VoucherService _sut;

    public VoucherServiceTests()
    {
        _sut = new VoucherService(_voucherRepositoryMock.Object);
    }

    private static Voucher ValidVoucher(VoucherDiscountType type = VoucherDiscountType.Percentage, decimal value = 10) => new()
    {
        Id = Guid.NewGuid(),
        Code = "SALE10",
        DiscountType = type,
        DiscountValue = value,
        MinOrderAmount = 0,
        MaxUsagePerUser = 1,
        ExpiresAt = DateTime.UtcNow.AddDays(1),
        IsActive = true
    };

    [Fact]
    public async Task ValidateAsync_UnknownCode_ReturnsFailure()
    {
        _voucherRepositoryMock.Setup(x => x.GetByCodeAsync("BADCODE")).ReturnsAsync((Voucher?)null);

        var result = await _sut.ValidateAsync("BADCODE", "user-1", 100);

        result.Succeeded.Should().BeFalse();
    }

    [Fact]
    public async Task ValidateAsync_InactiveVoucher_ReturnsFailure()
    {
        var voucher = ValidVoucher();
        voucher.IsActive = false;
        _voucherRepositoryMock.Setup(x => x.GetByCodeAsync(voucher.Code)).ReturnsAsync(voucher);

        var result = await _sut.ValidateAsync(voucher.Code, "user-1", 100);

        result.Succeeded.Should().BeFalse();
    }

    [Fact]
    public async Task ValidateAsync_ExpiredVoucher_ReturnsFailure()
    {
        var voucher = ValidVoucher();
        voucher.ExpiresAt = DateTime.UtcNow.AddDays(-1);
        _voucherRepositoryMock.Setup(x => x.GetByCodeAsync(voucher.Code)).ReturnsAsync(voucher);

        var result = await _sut.ValidateAsync(voucher.Code, "user-1", 100);

        result.Succeeded.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("hết hạn"));
    }

    [Fact]
    public async Task ValidateAsync_BelowMinOrderAmount_ReturnsFailure()
    {
        var voucher = ValidVoucher();
        voucher.MinOrderAmount = 500;
        _voucherRepositoryMock.Setup(x => x.GetByCodeAsync(voucher.Code)).ReturnsAsync(voucher);

        var result = await _sut.ValidateAsync(voucher.Code, "user-1", 100);

        result.Succeeded.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("tối thiểu"));
    }

    [Fact]
    public async Task ValidateAsync_UsageCapReached_ReturnsFailure()
    {
        var voucher = ValidVoucher();
        voucher.MaxUsageCount = 5;
        voucher.UsedCount = 5;
        _voucherRepositoryMock.Setup(x => x.GetByCodeAsync(voucher.Code)).ReturnsAsync(voucher);

        var result = await _sut.ValidateAsync(voucher.Code, "user-1", 100);

        result.Succeeded.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("hết lượt"));
    }

    [Fact]
    public async Task ValidateAsync_PerUserCapReached_ReturnsFailure()
    {
        var voucher = ValidVoucher();
        voucher.MaxUsagePerUser = 1;
        _voucherRepositoryMock.Setup(x => x.GetByCodeAsync(voucher.Code)).ReturnsAsync(voucher);
        _voucherRepositoryMock.Setup(x => x.GetUserRedemptionCountAsync(voucher.Id, "user-1")).ReturnsAsync(1);

        var result = await _sut.ValidateAsync(voucher.Code, "user-1", 100);

        result.Succeeded.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("sử dụng hết"));
    }

    [Fact]
    public async Task ValidateAsync_PercentageDiscount_ComputesCorrectAmount()
    {
        var voucher = ValidVoucher(VoucherDiscountType.Percentage, 10);
        _voucherRepositoryMock.Setup(x => x.GetByCodeAsync(voucher.Code)).ReturnsAsync(voucher);
        _voucherRepositoryMock.Setup(x => x.GetUserRedemptionCountAsync(voucher.Id, "user-1")).ReturnsAsync(0);

        var result = await _sut.ValidateAsync(voucher.Code, "user-1", 200);

        result.Succeeded.Should().BeTrue();
        result.Data!.DiscountAmount.Should().Be(20);
        result.Data.FinalTotal.Should().Be(180);
    }

    [Fact]
    public async Task ValidateAsync_FixedAmountDiscount_CannotExceedSubtotal()
    {
        var voucher = ValidVoucher(VoucherDiscountType.FixedAmount, 500);
        _voucherRepositoryMock.Setup(x => x.GetByCodeAsync(voucher.Code)).ReturnsAsync(voucher);
        _voucherRepositoryMock.Setup(x => x.GetUserRedemptionCountAsync(voucher.Id, "user-1")).ReturnsAsync(0);

        var result = await _sut.ValidateAsync(voucher.Code, "user-1", 100);

        result.Succeeded.Should().BeTrue();
        result.Data!.DiscountAmount.Should().Be(100);
        result.Data.FinalTotal.Should().Be(0);
    }

    [Fact]
    public async Task RedeemAsync_IncrementsUsedCountAndAddsRedemption()
    {
        var voucher = ValidVoucher();
        voucher.UsedCount = 2;
        var orderId = Guid.NewGuid();
        _voucherRepositoryMock.Setup(x => x.GetByIdAsync(voucher.Id)).ReturnsAsync(voucher);

        await _sut.RedeemAsync(voucher.Id, "user-1", orderId);

        voucher.UsedCount.Should().Be(3);
        _voucherRepositoryMock.Verify(x => x.UpdateAsync(It.Is<Voucher>(v => v.UsedCount == 3)), Times.Once);
        _voucherRepositoryMock.Verify(
            x => x.AddRedemptionAsync(It.Is<VoucherRedemption>(r => r.VoucherId == voucher.Id && r.UserId == "user-1" && r.OrderId == orderId)),
            Times.Once);
    }

    [Fact]
    public async Task CreateAsync_DuplicateCode_ReturnsFailure()
    {
        var existing = ValidVoucher();
        _voucherRepositoryMock.Setup(x => x.GetByCodeAsync(existing.Code)).ReturnsAsync(existing);

        var result = await _sut.CreateAsync(new CreateVoucherRequest { Code = existing.Code, DiscountValue = 5, ExpiresAt = DateTime.UtcNow.AddDays(1) });

        result.Succeeded.Should().BeFalse();
        _voucherRepositoryMock.Verify(x => x.AddAsync(It.IsAny<Voucher>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_NewCode_AddsVoucher()
    {
        _voucherRepositoryMock.Setup(x => x.GetByCodeAsync("NEW10")).ReturnsAsync((Voucher?)null);

        var result = await _sut.CreateAsync(new CreateVoucherRequest
        {
            Code = "NEW10",
            DiscountType = VoucherDiscountType.Percentage,
            DiscountValue = 10,
            ExpiresAt = DateTime.UtcNow.AddDays(1)
        });

        result.Succeeded.Should().BeTrue();
        _voucherRepositoryMock.Verify(x => x.AddAsync(It.Is<Voucher>(v => v.Code == "NEW10")), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_UnknownId_ReturnsFailure()
    {
        _voucherRepositoryMock.Setup(x => x.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Voucher?)null);

        var result = await _sut.DeleteAsync(Guid.NewGuid());

        result.Succeeded.Should().BeFalse();
    }
}
