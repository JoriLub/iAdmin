using iAdmin.Common.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using iAdmin.Common.Enums;

namespace iAdmin.Data.Context;

public class IAdminDbContext : DbContext
{
    public IAdminDbContext(DbContextOptions<IAdminDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<AdminSession> AdminSessions { get; set; }
    public DbSet<UpdateHistory> UpdateHistory { get; set; }
    public DbSet<AuditLog> AuditLogs { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Users configuration
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.UserId);
            entity.HasIndex(e => e.Username).IsUnique();
            entity.HasIndex(e => e.Email).IsUnique();
            
            entity.Property(e => e.Username)
                .HasMaxLength(255)
                .IsRequired();
            
            entity.Property(e => e.Email)
                .HasMaxLength(255)
                .IsRequired();
            
            entity.Property(e => e.PasswordHash)
                .IsRequired();
        });

        // AdminSessions configuration
        modelBuilder.Entity<AdminSession>(entity =>
        {
            entity.HasKey(e => e.SessionId);
            entity.HasIndex(e => new { e.UserId, e.StartTime });
            
            entity.Property(e => e.IpAddress)
                .HasMaxLength(45) // IPv6 max length
                .IsRequired();
            
            entity.Property(e => e.Status)
                .HasConversion(new EnumToNumberConverter<AdminSessionStatus, int>());
            
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // UpdateHistory configuration
        modelBuilder.Entity<UpdateHistory>(entity =>
        {
            entity.HasKey(e => e.UpdateId);
            entity.HasIndex(e => e.AppliedAt).IsDescending();
            
            entity.Property(e => e.Version)
                .HasMaxLength(50)
                .IsRequired();
            
            entity.Property(e => e.ChecksumSha256)
                .HasMaxLength(64)
                .IsRequired();
            
            entity.Property(e => e.Status)
                .HasConversion(new EnumToNumberConverter<UpdateStatus, int>());
        });

        // AuditLog configuration
        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.HasKey(e => e.AuditId);
            entity.HasIndex(e => e.ChangedAt).IsDescending();
            entity.HasIndex(e => new { e.UserId, e.ChangedAt });
            
            entity.Property(e => e.UserName)
                .HasMaxLength(255)
                .IsRequired();
            
            entity.Property(e => e.ActionType)
                .HasConversion(new EnumToNumberConverter<AuditActionType, int>());
            
            entity.Property(e => e.ChangedBy)
                .HasMaxLength(255)
                .IsRequired();
            
            entity.Property(e => e.IpAddress)
                .HasMaxLength(45)
                .IsRequired();
        });
    }
}
