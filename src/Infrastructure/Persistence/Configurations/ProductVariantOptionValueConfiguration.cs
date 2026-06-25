using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ShopeeClone.Domain.Entities;

namespace ShopeeClone.Infrastructure.Persistence.Configurations;

public class ProductVariantOptionValueConfiguration : IEntityTypeConfiguration<ProductVariantOptionValue>
{
    public void Configure(EntityTypeBuilder<ProductVariantOptionValue> builder)
    {
        builder.HasKey(v => v.Id);

        builder.Property(v => v.Value)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(v => v.ImageUrl)
            .HasMaxLength(500);

        builder.HasOne(v => v.ProductVariantOption)
            .WithMany(o => o.Values)
            .HasForeignKey(v => v.ProductVariantOptionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(v => new { v.ProductVariantOptionId, v.Value })
            .IsUnique();
    }
}
