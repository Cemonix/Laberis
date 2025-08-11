using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using server.Models.Domain;

namespace server.Data.Configurations;

public class EmailVerificationTokenConfiguration : IEntityTypeConfiguration<EmailVerificationToken>
{
    public void Configure(EntityTypeBuilder<EmailVerificationToken> entity)
    {
        entity.ToTable("email_verification_tokens");

        entity.HasKey(evt => evt.Id);
        entity.Property(evt => evt.Id).HasColumnName("id").ValueGeneratedOnAdd();

        entity.Property(evt => evt.Token)
            .HasColumnName("token")
            .IsRequired()
            .HasMaxLength(512);

        entity.Property(evt => evt.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        entity.Property(evt => evt.Email)
            .HasColumnName("email")
            .IsRequired()
            .HasMaxLength(256);

        entity.Property(evt => evt.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired()
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .ValueGeneratedOnAdd();

        entity.Property(evt => evt.ExpiresAt)
            .HasColumnName("expires_at")
            .IsRequired();

        entity.Property(evt => evt.IsUsed)
            .HasColumnName("is_used")
            .IsRequired()
            .HasDefaultValue(false);

        entity.Property(evt => evt.UsedAt)
            .HasColumnName("used_at")
            .IsRequired(false);

        // Indexes
        entity.HasIndex(evt => evt.Token).IsUnique();
        entity.HasIndex(evt => evt.UserId);
        entity.HasIndex(evt => evt.Email);
        entity.HasIndex(evt => evt.ExpiresAt);
        entity.HasIndex(evt => new { evt.UserId, evt.IsUsed });

        // Relationships
        entity.HasOne(evt => evt.User)
            .WithMany()
            .HasForeignKey(evt => evt.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Query filter to only include valid (non-expired, non-used) tokens by default
        entity.HasQueryFilter(evt => !evt.IsUsed && evt.ExpiresAt > DateTime.UtcNow);
    }
}