using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using HungStore.Domain.Entities;

namespace HungStore.Infrastructure.Persistence.Configurations;

public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.HasKey(o => o.Id);

        builder.Property(o => o.UserId)
            .IsRequired();

        builder.Property(o => o.ShippingAddress)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(o => o.Subtotal)
            .HasPrecision(18, 2);

        builder.Property(o => o.DiscountAmount)
            .HasPrecision(18, 2);

        builder.Property(o => o.TotalAmount)
            .HasPrecision(18, 2);

        builder.HasOne<Voucher>()
            .WithMany()
            .HasForeignKey(o => o.VoucherId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(o => o.UserId);
    }
}
