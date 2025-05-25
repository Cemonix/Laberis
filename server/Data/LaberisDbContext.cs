using System;
using Microsoft.EntityFrameworkCore;
using server.Models.Domain;
using server.Models.Domain.Enums;

namespace server.Data;

public class LaberisDbContext : DbContext
{
    public DbSet<Project> Projects { get; set; }
    public DbSet<LabelScheme> LabelSchemes { get; set; }
    public DbSet<Label> Labels { get; set; }
    public DbSet<ProjectMember> ProjectMembers { get; set; }
    public DbSet<DataSource> DataSources { get; set; }

    public LaberisDbContext(DbContextOptions<LaberisDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasPostgresEnum<ProjectType>("public", "project_type_enum");
        modelBuilder.HasPostgresEnum<ProjectStatus>("public", "project_status_enum");
        modelBuilder.HasPostgresEnum<ProjectRole>("public", "project_role_enum");
        modelBuilder.HasPostgresEnum<DataSourceType>("public", "data_source_type_enum");
        modelBuilder.HasPostgresEnum<DataSourceStatus>("public", "data_source_status_enum");

        modelBuilder.Entity<Project>(entity =>
        {
            entity.ToTable("projects");

            entity.HasKey(p => p.ProjectId);
            entity.Property(p => p.ProjectId).HasColumnName("project_id").ValueGeneratedOnAdd();

            entity.Property(p => p.Name)
                .HasColumnName("name")
                .IsRequired()
                .HasMaxLength(255);

            entity.Property(p => p.Description)
                .HasColumnName("description")
                .IsRequired(false);

            entity.Property(p => p.ProjectType)
                .HasColumnName("project_type")
                .IsRequired()
                .HasDefaultValue(ProjectType.OTHER)
                .HasColumnType("project_type_enum");

            entity.Property(p => p.Status)
                .HasColumnName("status")
                .IsRequired()
                .HasDefaultValue(ProjectStatus.ACTIVE)
                .HasColumnType("project_status_enum");

            entity.Property(p => p.OwnerId)
                .HasColumnName("owner_id")
                .IsRequired(false);

            entity.Property(p => p.AnnotationGuidelinesUrl)
                .HasColumnName("annotation_guidelines_url")
                .IsRequired(false);

            entity.Property(p => p.CreatedAt)
                .HasColumnName("created_at")
                .IsRequired()
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .ValueGeneratedOnAdd();

            entity.Property(p => p.UpdatedAt)
                .HasColumnName("updated_at")
                .IsRequired()
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .ValueGeneratedOnAddOrUpdate();

            // Relationships
            entity.HasOne(p => p.Owner)
                .WithMany()
                .HasForeignKey(p => p.OwnerId)
                .OnDelete(DeleteBehavior.SetNull);

            // One-to-Many relationships
            entity.HasMany(p => p.LabelSchemes)
                .WithOne(l => l.Project)
                .HasForeignKey(l => l.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(p => p.DataSources)
                .WithOne(ds => ds.Project)
                .HasForeignKey(ds => ds.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(p => p.Assets)
                .WithOne(a => a.Project)
                .HasForeignKey(a => a.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(p => p.Workflows)
                .WithOne(w => w.Project)
                .HasForeignKey(w => w.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(p => p.ProjectMembers)
                .WithOne(pm => pm.Project)
                .HasForeignKey(pm => pm.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ProjectMember>(entity =>
        {
            entity.ToTable("project_members");

            entity.HasKey(pm => pm.ProjectMemberId);
            entity.Property(pm => pm.ProjectMemberId).HasColumnName("project_member_id").ValueGeneratedOnAdd();

            entity.Property(pm => pm.Role)
                .HasColumnName("role")
                .IsRequired()
                .HasColumnType("project_role_enum");

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
        });

        modelBuilder.Entity<DataSource>(entity =>
        {
            entity.ToTable("data_sources");

            entity.HasKey(ds => ds.DataSourceId);
            entity.Property(ds => ds.DataSourceId).HasColumnName("data_source_id").ValueGeneratedOnAdd();

            entity.Property(ds => ds.Name)
                .HasColumnName("name")
                .IsRequired()
                .HasMaxLength(255);

            entity.Property(ds => ds.Description)
                .HasColumnName("description")
                .IsRequired(false);

            entity.Property(ds => ds.SourceType)
                .HasColumnName("source_type")
                .IsRequired()
                .HasColumnType("data_source_type_enum");

            entity.Property(ds => ds.ConnectionDetails)
                .HasColumnName("connection_details")
                .HasColumnType("jsonb")
                .IsRequired(false);

            entity.Property(ds => ds.Status)
                .HasColumnName("status")
                .IsRequired()
                .HasDefaultValue(DataSourceStatus.ACTIVE)
                .HasColumnType("data_source_status_enum");

            entity.Property(ds => ds.IsDefault)
                .HasColumnName("is_default")
                .IsRequired()
                .HasDefaultValue(false);

            entity.Property(ds => ds.CreatedAt)
                .HasColumnName("created_at")
                .IsRequired()
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .ValueGeneratedOnAdd();

            entity.Property(ds => ds.UpdatedAt)
                .HasColumnName("updated_at")
                .IsRequired()
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .ValueGeneratedOnAddOrUpdate();

            entity.Property(ds => ds.ProjectId).HasColumnName("project_id");

            // Relationship: DataSource to Project (Many-to-One)
            entity.HasOne(ds => ds.Project)
                .WithMany(p => p.DataSources)
                .HasForeignKey(ds => ds.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            // Relationship: DataSource to Assets (One-to-Many)
            entity.HasMany(ds => ds.Assets)
                .WithOne(a => a.DataSource)
                .HasForeignKey(a => a.DataSourceId)
                .OnDelete(DeleteBehavior.SetNull);
        });
        
        modelBuilder.Entity<LabelScheme>(entity =>
        {
            entity.ToTable("label_schemes");

            entity.HasKey(ls => ls.LabelSchemeId);
            entity.Property(ls => ls.LabelSchemeId).HasColumnName("label_scheme_id").ValueGeneratedOnAdd();

            entity.Property(ls => ls.Name)
                .HasColumnName("name")
                .IsRequired()
                .HasMaxLength(255);

            entity.Property(ls => ls.Description)
                .HasColumnName("description")
                .IsRequired(false);

            entity.Property(ls => ls.IsDefault)
                .HasColumnName("is_default")
                .IsRequired()
                .HasDefaultValue(false);

            entity.Property(ls => ls.CreatedAt)
                .HasColumnName("created_at")
                .IsRequired()
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .ValueGeneratedOnAdd();

            entity.Property(ls => ls.UpdatedAt)
                .HasColumnName("updated_at")
                .IsRequired()
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .ValueGeneratedOnAddOrUpdate();

            entity.Property(ls => ls.ProjectId).HasColumnName("project_id");

            // Unique constraint for Name within a Project
            entity.HasIndex(ls => new { ls.ProjectId, ls.Name }).IsUnique();

            // Relationship: LabelScheme to Labels (One-to-Many)
            entity.HasMany(ls => ls.Labels)
                .WithOne(l => l.LabelScheme)
                .HasForeignKey(l => l.LabelSchemeId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Label>(entity =>
        {
            entity.ToTable("labels");

            entity.HasKey(l => l.LabelId);
            entity.Property(l => l.LabelId).HasColumnName("label_id").ValueGeneratedOnAdd();

            entity.Property(l => l.Name)
                .HasColumnName("name")
                .IsRequired()
                .HasMaxLength(255);

            entity.Property(l => l.Color)
                .HasColumnName("color")
                .HasMaxLength(7) // For #RRGGBB
                .IsRequired(false);

            entity.Property(l => l.Description)
                .HasColumnName("description")
                .IsRequired(false);

            entity.Property(l => l.Metadata)
                .HasColumnName("metadata")
                .HasColumnType("jsonb")
                .IsRequired(false);

            entity.Property(l => l.CreatedAt)
                .HasColumnName("created_at")
                .IsRequired()
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .ValueGeneratedOnAdd();

            entity.Property(l => l.UpdatedAt)
                .HasColumnName("updated_at")
                .IsRequired()
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .ValueGeneratedOnAddOrUpdate();

            entity.Property(l => l.LabelSchemeId).HasColumnName("label_scheme_id");

            // Unique constraint for Name within a LabelScheme
            entity.HasIndex(l => new { l.LabelSchemeId, l.Name }).IsUnique();
        });
    }
}
