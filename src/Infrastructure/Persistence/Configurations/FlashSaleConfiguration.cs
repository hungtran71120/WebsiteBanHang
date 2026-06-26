using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using HungStore.Domain.Entities;

namespace HungStore.Infrastructure.Persistence.Configurations;

public class FlashSaleConfiguration : IEntityTypeConfiguration<FlashSale>
{
    public void Configure(EntityTypeBuilder<FlashSale> builder)
    {
        builder.HasKey(f => f.Id);

        builder.Property(f => f.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.HasIndex(f => new { f.StartsAt, f.EndsAt });
    }
}
