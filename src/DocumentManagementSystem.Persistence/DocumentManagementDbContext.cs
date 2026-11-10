using DocumentManagementSystem.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace DocumentManagementSystem.Persistence;

public sealed class DocumentManagementDbContext(DbContextOptions<DocumentManagementDbContext> options)
    : DbContext(options)
{
    public DbSet<DocumentEntity> Documents => Set<DocumentEntity>();
    public DbSet<CollectionEntity> Collections => Set<CollectionEntity>();
    public DbSet<CollectionDocumentEntity> CollectionDocuments => Set<CollectionDocumentEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DocumentEntity>(entity =>
        {
            entity.ToTable("documents");
            entity.HasKey(document => document.Id);
            entity.Property(document => document.FileName).IsRequired().HasMaxLength(255);
            entity.Property(document => document.ContentType).IsRequired().HasMaxLength(100);
            entity.Property(document => document.FileSize).IsRequired();
            entity.Property(document => document.Content).IsRequired();
            entity.Property(document => document.Title).HasMaxLength(200);
            entity.Property(document => document.Description).HasMaxLength(1000);
            entity.Property(document => document.Status).IsRequired();
            entity.Property(document => document.CreatedAt).IsRequired();
            entity.Property(document => document.UpdatedAt)
                .HasColumnName("updatedat")
                .IsRequired();
        });

        modelBuilder.Entity<CollectionEntity>(entity =>
        {
            entity.ToTable("collections");
            entity.HasKey(collection => collection.Id);
            entity.Property(collection => collection.Name).IsRequired().HasMaxLength(200);
        });

        modelBuilder.Entity<CollectionDocumentEntity>(entity =>
        {
            entity.ToTable("collection_documents");
            entity.HasKey(link => new { link.CollectionId, link.DocumentId });
            entity.HasOne(link => link.Collection)
                .WithMany(collection => collection.CollectionDocuments)
                .HasForeignKey(link => link.CollectionId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(link => link.Document)
                .WithMany(document => document.CollectionDocuments)
                .HasForeignKey(link => link.DocumentId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
