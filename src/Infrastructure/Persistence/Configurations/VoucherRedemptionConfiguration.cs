using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using HungStore.Domain.Entities;

namespace HungStore.Infrastructure.Persistence.Configurations;

public class VoucherRedemptionConfiguration : IEntityTypeConfiguration<VoucherRedemption>
{
    public void Configure(EntityTypeBuilder<VoucherRedemption> builder)
    {
        builder.HasKey(r => r.Id);

        builder.Property(r => r.UserId)
            .IsRequired();

        builder.HasOne(r => r.Voucher)
            .WithMany()
            .HasForeignKey(r => r.VoucherId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(r => new { r.VoucherId, r.UserId });
    }
}
