using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using HungStore.Domain.Entities;

namespace HungStore.Infrastructure.Persistence.Configurations;

public class ConversationConfiguration : IEntityTypeConfiguration<Conversation>
{
    public void Configure(EntityTypeBuilder<Conversation> builder)
    {
        builder.HasKey(c => c.Id);

        builder.Property(c => c.CustomerId)
            .IsRequired();

        builder.Property(c => c.CustomerName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(c => c.LastMessagePreview)
            .HasMaxLength(500);

        builder.HasIndex(c => c.CustomerId)
            .IsUnique();

        builder.HasMany(c => c.Messages)
            .WithOne(m => m.Conversation)
            .HasForeignKey(m => m.ConversationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
