using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using HungStore.Domain.Entities;

namespace HungStore.Infrastructure.Persistence.Configurations;

public class WishlistConfiguration : IEntityTypeConfiguration<Wishlist>
{
    public void Configure(EntityTypeBuilder<Wishlist> builder)
    {
        builder.HasKey(w => w.Id);

        builder.Property(w => w.UserId)
            .IsRequired();

        builder.HasIndex(w => w.UserId)
            .IsUnique();
    }
}
