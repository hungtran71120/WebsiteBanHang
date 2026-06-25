using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ShopeeClone.Domain.Entities;

namespace ShopeeClone.Infrastructure.Persistence.Configurations;

public class VoucherConfiguration : IEntityTypeConfiguration<Voucher>
{
    public void Configure(EntityTypeBuilder<Voucher> builder)
    {
        builder.HasKey(v => v.Id);

        builder.Property(v => v.Code)
            .IsRequired();

        builder.HasIndex(v => v.Code)
            .IsUnique();

        builder.Property(v => v.DiscountValue).HasPrecision(18, 2);
        builder.Property(v => v.MinOrderAmount).HasPrecision(18, 2);
    }
}
