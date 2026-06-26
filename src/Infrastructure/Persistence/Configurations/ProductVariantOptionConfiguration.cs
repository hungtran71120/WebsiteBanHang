using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using HungStore.Domain.Entities;

namespace HungStore.Infrastructure.Persistence.Configurations;

public class ProductVariantOptionConfiguration : IEntityTypeConfiguration<ProductVariantOption>
{
    public void Configure(EntityTypeBuilder<ProductVariantOption> builder)
    {
        builder.HasKey(o => o.Id);

        builder.Property(o => o.Name)
            .IsRequired()
            .HasMaxLength(50);

        builder.HasOne(o => o.Product)
            .WithMany(p => p.VariantOptions)
            .HasForeignKey(o => o.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(o => new { o.ProductId, o.Name })
            .IsUnique();
    }
}
