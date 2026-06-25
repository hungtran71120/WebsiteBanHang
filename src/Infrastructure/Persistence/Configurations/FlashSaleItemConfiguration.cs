using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ShopeeClone.Domain.Entities;

namespace ShopeeClone.Infrastructure.Persistence.Configurations;

public class FlashSaleItemConfiguration : IEntityTypeConfiguration<FlashSaleItem>
{
    public void Configure(EntityTypeBuilder<FlashSaleItem> builder)
    {
        builder.HasKey(i => i.Id);

        builder.Property(i => i.SalePrice)
            .HasPrecision(18, 2);

        builder.HasOne(i => i.FlashSale)
            .WithMany(f => f.Items)
            .HasForeignKey(i => i.FlashSaleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(i => i.Product)
            .WithMany()
            .HasForeignKey(i => i.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(i => new { i.FlashSaleId, i.ProductId })
            .IsUnique();
    }
}
