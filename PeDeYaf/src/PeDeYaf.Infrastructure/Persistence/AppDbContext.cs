using System;
using Microsoft.EntityFrameworkCore;
using PeDeYaf.Domain.Entities;

namespace PeDeYaf.Infrastructure.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Document> Documents => Set<Document>();
    public DbSet<Folder> Folders => Set<Folder>();
    public DbSet<Annotation> Annotations => Set<Annotation>();
    public DbSet<ShareLink> ShareLinks => Set<ShareLink>();
    public DbSet<DocumentVersion> DocumentVersions => Set<DocumentVersion>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>().HasKey(u => u.Id);
        modelBuilder.Entity<User>().HasIndex(u => u.Phone).IsUnique();

        modelBuilder.Entity<Document>().HasKey(d => d.Id);
        modelBuilder.Entity<Document>().HasIndex(d => d.S3Key).IsUnique();
        
        modelBuilder.Entity<Document>()
            .HasOne(d => d.User)
            .WithMany()
            .HasForeignKey(d => d.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Document>()
            .HasOne(d => d.Folder)
            .WithMany()
            .HasForeignKey(d => d.FolderId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Folder>().HasKey(f => f.Id);
        modelBuilder.Entity<Annotation>().HasKey(a => a.Id);
        modelBuilder.Entity<ShareLink>().HasKey(s => s.Id);
        modelBuilder.Entity<DocumentVersion>().HasKey(v => v.Id);
        
        modelBuilder.Entity<RefreshToken>().HasKey(r => r.Id);
        modelBuilder.Entity<RefreshToken>().HasIndex(r => r.Token).IsUnique();
        modelBuilder.Entity<RefreshToken>()
            .HasOne<User>()
            .WithMany()
            .HasForeignKey(r => r.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
