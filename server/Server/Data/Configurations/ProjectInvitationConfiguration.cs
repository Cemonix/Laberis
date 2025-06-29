using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using server.Models.Domain;
using server.Models.Domain.Enums;

namespace server.Data.Configurations;

public class ProjectInvitationConfiguration : IEntityTypeConfiguration<ProjectInvitation>
{
    public void Configure(EntityTypeBuilder<ProjectInvitation> entity)
    {
        entity.ToTable("project_invitations");

        entity.HasKey(pi => pi.Id);
        entity.Property(pi => pi.Id).HasColumnName("id").ValueGeneratedOnAdd();

        entity.Property(pi => pi.ProjectId)
            .HasColumnName("project_id")
            .IsRequired();

        entity.Property(pi => pi.Email)
            .HasColumnName("email")
            .IsRequired()
            .HasMaxLength(255);

        entity.Property(pi => pi.Role)
            .HasColumnName("role")
            .IsRequired()
            .HasColumnType("public.project_role_enum");

        entity.Property(pi => pi.InvitationToken)
            .HasColumnName("invitation_token")
            .IsRequired()
            .HasMaxLength(255);

        entity.Property(pi => pi.ExpiresAt)
            .HasColumnName("expires_at")
            .IsRequired();

        entity.Property(pi => pi.IsAccepted)
            .HasColumnName("is_accepted")
            .IsRequired()
            .HasDefaultValue(false);

        entity.Property(pi => pi.InvitedByUserId)
            .HasColumnName("invited_by_user_id")
            .IsRequired(false);

        entity.Property(pi => pi.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired()
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .ValueGeneratedOnAdd();

        entity.Property(pi => pi.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired()
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .ValueGeneratedOnAddOrUpdate();

        // Indexes
        entity.HasIndex(pi => pi.InvitationToken).IsUnique();
        entity.HasIndex(pi => new { pi.Email, pi.ProjectId });
        entity.HasIndex(pi => pi.ExpiresAt);

        // Relationships
        entity.HasOne(pi => pi.Project)
            .WithMany()
            .HasForeignKey(pi => pi.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        entity.HasOne(pi => pi.InvitedByUser)
            .WithMany()
            .HasForeignKey(pi => pi.InvitedByUserId)
            .OnDelete(DeleteBehavior.SetNull);

        // Query filter to only include unexpired invitations by default
        entity.HasQueryFilter(pi => pi.ExpiresAt > DateTime.UtcNow && !pi.IsAccepted);
    }
}
