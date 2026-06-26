using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using HungStore.Domain.Entities;

namespace HungStore.Infrastructure.Persistence.Configurations;

public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.HasKey(n => n.Id);

        builder.Property(n => n.UserId)
            .IsRequired();

        builder.Property(n => n.Message)
            .IsRequired()
            .HasMaxLength(500);

        builder.HasIndex(n => new { n.UserId, n.IsRead });
    }
}
