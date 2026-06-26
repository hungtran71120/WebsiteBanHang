using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using HungStore.Domain.Entities;

namespace HungStore.Infrastructure.Persistence.Configurations;

public class ChatMessageConfiguration : IEntityTypeConfiguration<ChatMessage>
{
    public void Configure(EntityTypeBuilder<ChatMessage> builder)
    {
        builder.HasKey(m => m.Id);

        builder.Property(m => m.SenderId)
            .IsRequired();

        builder.Property(m => m.SenderName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(m => m.Content)
            .IsRequired()
            .HasMaxLength(2000);
    }
}
