using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ShopeeClone.Domain.Entities;

namespace ShopeeClone.Infrastructure.Persistence.Configurations;

public class ProductVariantConfiguration : IEntityTypeConfiguration<ProductVariant>
{
    public void Configure(EntityTypeBuilder<ProductVariant> builder)
    {
        builder.HasKey(v => v.Id);

        builder.HasOne(v => v.Product)
            .WithMany(p => p.Variants)
            .HasForeignKey(v => v.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(v => v.OptionValue1)
            .WithMany()
            .HasForeignKey(v => v.OptionValue1Id)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(v => v.OptionValue2)
            .WithMany()
            .HasForeignKey(v => v.OptionValue2Id)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(v => new { v.ProductId, v.OptionValue1Id, v.OptionValue2Id })
            .IsUnique();
    }
}
