using DocumentManagementSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DocumentManagementSystem.Persistence;

public sealed class DocumentManagementDbContext(DbContextOptions<DocumentManagementDbContext> options)
    : DbContext(options)
{
    public DbSet<Document> Documents => Set<Document>();
    public DbSet<Collection> Collections => Set<Collection>();
    public DbSet<CollectionDocument> CollectionDocuments => Set<CollectionDocument>();
    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Document>(entity =>
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

        modelBuilder.Entity<Collection>(entity =>
        {
            entity.ToTable("collections");
            entity.HasKey(collection => collection.Id);
            entity.Property(collection => collection.Name).IsRequired().HasMaxLength(200);
        });


        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("users");
            entity.HasKey(user => user.Id);
            entity.Property(user => user.Username).IsRequired().HasMaxLength(50);
            entity.Property(user => user.NormalizedUsername).IsRequired().HasMaxLength(50);
            entity.Property(user => user.PasswordHash).IsRequired().HasMaxLength(512);
            entity.Property(user => user.CreatedAt).IsRequired();
            entity.HasIndex(user => user.NormalizedUsername).IsUnique();
        });

        modelBuilder.Entity<CollectionDocument>(entity =>
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