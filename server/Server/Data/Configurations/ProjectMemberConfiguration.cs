using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using server.Models.Domain;
using server.Models.Domain.Enums;

namespace server.Data.Configurations;

public class ProjectMemberConfiguration : IEntityTypeConfiguration<ProjectMember>
{
    public void Configure(EntityTypeBuilder<ProjectMember> entity)
    {
        entity.ToTable("project_members");

        entity.HasKey(pm => pm.ProjectMemberId);
        entity.Property(pm => pm.ProjectMemberId).HasColumnName("project_member_id").ValueGeneratedOnAdd();

        entity.Property(pm => pm.Role)
            .HasColumnName("role")
            .IsRequired()
            .HasColumnType("public.project_role_enum");

        entity.Property(pm => pm.InvitedAt)
            .HasColumnName("invited_at")
            .IsRequired()
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        entity.Property(pm => pm.JoinedAt)
            .HasColumnName("joined_at")
            .IsRequired(false);

        entity.Property(pm => pm.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired()
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .ValueGeneratedOnAdd();

        entity.Property(pm => pm.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired()
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .ValueGeneratedOnAddOrUpdate();

        entity.Property(pm => pm.ProjectId).HasColumnName("project_id");
        entity.Property(pm => pm.UserId).HasColumnName("user_id").IsRequired();

        // Unique constraint for (ProjectId, UserId)
        entity.HasIndex(pm => new { pm.ProjectId, pm.UserId }).IsUnique();

        // Relationship: ProjectMember to Project (Many-to-One)
        entity.HasOne(pm => pm.Project)
            .WithMany(p => p.ProjectMembers)
            .HasForeignKey(pm => pm.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        // Relationship: ProjectMember to User (Many-to-One)
        entity.HasOne(pm => pm.User)
            .WithMany()
            .HasForeignKey(pm => pm.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        entity.HasQueryFilter(ds => ds.Project.Status != ProjectStatus.PENDING_DELETION);
    }
}
