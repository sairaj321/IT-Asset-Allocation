using Its.Domain.Entities;
using Its.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Its.Infrastructure.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<Department> Departments => Set<Department>();
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<AssetCategory> AssetCategories => Set<AssetCategory>();
    public DbSet<ITAsset> Assets => Set<ITAsset>();
    public DbSet<AssetRequest> AssetRequests => Set<AssetRequest>();
    public DbSet<AssetRequestHistory> RequestHistories => Set<AssetRequestHistory>();
    public DbSet<AssetAllocation> AssetAllocations => Set<AssetAllocation>();
    public DbSet<AssetRepair> AssetRepairs => Set<AssetRepair>();
    public DbSet<AssetReplacement> AssetReplacements => Set<AssetReplacement>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Departments
        modelBuilder.Entity<Department>(entity =>
        {
            entity.HasKey(e => e.DepartmentId);
            entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
            entity.HasIndex(e => e.Name).IsUnique();
        });

        // Employees
        modelBuilder.Entity<Employee>(entity =>
        {
            entity.HasKey(e => e.EmployeeId);
            entity.Property(e => e.EmployeeCode).HasMaxLength(20).IsRequired();
            entity.HasIndex(e => e.EmployeeCode).IsUnique();
            entity.Property(e => e.FirstName).HasMaxLength(100).IsRequired();
            entity.Property(e => e.LastName).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Email).HasMaxLength(150).IsRequired();
            entity.HasIndex(e => e.Email).IsUnique();
            entity.Property(e => e.Mobile).HasMaxLength(20);
            entity.Property(e => e.Designation).HasMaxLength(100);

            entity.HasOne(e => e.Department)
                .WithMany(d => d.Employees)
                .HasForeignKey(e => e.DepartmentId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // Users
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.UserId);
            entity.Property(e => e.Username).HasMaxLength(80).IsRequired();
            entity.HasIndex(e => e.Username).IsUnique();
            entity.Property(e => e.PasswordHash).HasMaxLength(255).IsRequired();

            entity.HasOne(e => e.Employee)
                .WithOne(emp => emp.User)
                .HasForeignKey<User>(e => e.EmployeeId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Roles
        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.RoleId);
            entity.Property(e => e.Name).HasMaxLength(50).IsRequired();
            entity.HasIndex(e => e.Name).IsUnique();
        });

        // Permissions
        modelBuilder.Entity<Permission>(entity =>
        {
            entity.HasKey(e => e.PermissionId);
            entity.Property(e => e.Code).HasMaxLength(80).IsRequired();
            entity.HasIndex(e => e.Code).IsUnique();
        });

        // UserRoles
        modelBuilder.Entity<UserRole>(entity =>
        {
            entity.HasKey(e => new { e.UserId, e.RoleId });
            entity.HasOne(e => e.User).WithMany(u => u.UserRoles).HasForeignKey(e => e.UserId);
            entity.HasOne(e => e.Role).WithMany(r => r.UserRoles).HasForeignKey(e => e.RoleId);
        });

        // RolePermissions
        modelBuilder.Entity<RolePermission>(entity =>
        {
            entity.HasKey(e => new { e.RoleId, e.PermissionId });
            entity.HasOne(e => e.Role).WithMany(r => r.RolePermissions).HasForeignKey(e => e.RoleId);
            entity.HasOne(e => e.Permission).WithMany(p => p.RolePermissions).HasForeignKey(e => e.PermissionId);
        });

        // RefreshTokens
        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.HasKey(e => e.TokenId);
            entity.Property(e => e.TokenHash).HasMaxLength(255).IsRequired();
            entity.HasIndex(e => e.TokenHash);
            entity.HasOne(e => e.User).WithMany(u => u.RefreshTokens).HasForeignKey(e => e.UserId);
        });

        // AssetCategories
        modelBuilder.Entity<AssetCategory>(entity =>
        {
            entity.HasKey(e => e.AssetCategoryId);
            entity.Property(e => e.CategoryName).HasMaxLength(100).IsRequired();
            entity.HasIndex(e => e.CategoryName).IsUnique();
            entity.Property(e => e.Description).HasMaxLength(300);
        });

        // ITAssets
        modelBuilder.Entity<ITAsset>(entity =>
        {
            entity.HasKey(e => e.AssetId);
            entity.Property(e => e.AssetCode).HasMaxLength(30).IsRequired();
            entity.HasIndex(e => e.AssetCode).IsUnique();
            entity.Property(e => e.AssetName).HasMaxLength(150).IsRequired();
            entity.Property(e => e.SerialNumber).HasMaxLength(100).IsRequired();
            entity.HasIndex(e => e.SerialNumber).IsUnique();
            entity.Property(e => e.Brand).HasMaxLength(100);
            entity.Property(e => e.Model).HasMaxLength(100);
            entity.Property(e => e.PurchaseCost).HasPrecision(12, 2);
            entity.Property(e => e.Location).HasMaxLength(100);
            entity.Property(e => e.Status).HasConversion<string>().HasMaxLength(30);
            entity.Property(e => e.Condition).HasConversion<string>().HasMaxLength(30);
            entity.HasIndex(e => e.Status);

            // Optimistic concurrency token
            entity.Property(e => e.Version).IsConcurrencyToken();

            entity.HasOne(e => e.Category)
                .WithMany(c => c.Assets)
                .HasForeignKey(e => e.AssetCategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.CurrentEmployee)
                .WithMany()
                .HasForeignKey(e => e.CurrentEmployeeId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // AssetRequests
        modelBuilder.Entity<AssetRequest>(entity =>
        {
            entity.HasKey(e => e.RequestId);
            entity.Property(e => e.RequestNumber).HasMaxLength(20).IsRequired();
            entity.HasIndex(e => e.RequestNumber).IsUnique();
            entity.Property(e => e.Reason).HasMaxLength(500).IsRequired();
            entity.Property(e => e.Priority).HasConversion<string>().HasMaxLength(20);
            entity.Property(e => e.Status).HasConversion<string>().HasMaxLength(30);
            entity.Property(e => e.RequestType).HasConversion<string>().HasMaxLength(20);
            entity.Property(e => e.ManagerRemarks).HasMaxLength(500);
            entity.Property(e => e.RejectionReason).HasMaxLength(500);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.EmployeeId);
            entity.HasIndex(e => e.AssignedExecutiveId);

            entity.HasOne(e => e.Employee).WithMany(emp => emp.AssetRequests).HasForeignKey(e => e.EmployeeId);
            entity.HasOne(e => e.AssetCategory).WithMany(c => c.AssetRequests).HasForeignKey(e => e.AssetCategoryId);
            entity.HasOne(e => e.AssignedExecutive).WithMany(u => u.AssignedRequests).HasForeignKey(e => e.AssignedExecutiveId).OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(e => e.AllocatedAsset).WithMany().HasForeignKey(e => e.AllocatedAssetId).OnDelete(DeleteBehavior.SetNull);
        });

        // AssetRequestHistory
        modelBuilder.Entity<AssetRequestHistory>(entity =>
        {
            entity.HasKey(e => e.HistoryId);
            entity.Property(e => e.FromStatus).HasConversion<string>().HasMaxLength(30);
            entity.Property(e => e.ToStatus).HasConversion<string>().HasMaxLength(30);
            entity.Property(e => e.Remarks).HasMaxLength(500);

            entity.HasOne(e => e.Request).WithMany(r => r.RequestHistories).HasForeignKey(e => e.RequestId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.ActionByUser).WithMany().HasForeignKey(e => e.ActionBy).OnDelete(DeleteBehavior.SetNull);
        });

        // AssetAllocations
        modelBuilder.Entity<AssetAllocation>(entity =>
        {
            entity.HasKey(e => e.AllocationId);
            entity.Property(e => e.AllocationStatus).HasConversion<string>().HasMaxLength(20);
            entity.Property(e => e.ConditionAtAllocation).HasConversion<string>().HasMaxLength(30);
            entity.Property(e => e.ConditionAtReturn).HasConversion<string>().HasMaxLength(30);
            entity.Property(e => e.Remarks).HasMaxLength(500);

            // Partial unique index: only 1 Active allocation per AssetId
            entity.HasIndex(e => new { e.AssetId, e.AllocationStatus });

            entity.HasOne(e => e.Asset).WithMany(a => a.AssetAllocations).HasForeignKey(e => e.AssetId);
            entity.HasOne(e => e.Employee).WithMany(emp => emp.AssetAllocations).HasForeignKey(e => e.EmployeeId);
            entity.HasOne(e => e.Request).WithMany(r => r.Allocations).HasForeignKey(e => e.RequestId).OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(e => e.AllocatedByUser).WithMany().HasForeignKey(e => e.AllocatedBy);
        });

        // AssetRepairs
        modelBuilder.Entity<AssetRepair>(entity =>
        {
            entity.HasKey(e => e.RepairId);
            entity.Property(e => e.IssueDescription).HasMaxLength(1000).IsRequired();
            entity.Property(e => e.Status).HasConversion<string>().HasMaxLength(30);
            entity.Property(e => e.VendorName).HasMaxLength(150);
            entity.Property(e => e.RepairCost).HasPrecision(12, 2);
            entity.Property(e => e.ConditionAfterRepair).HasConversion<string>().HasMaxLength(30);

            entity.HasOne(e => e.Asset).WithMany(a => a.AssetRepairs).HasForeignKey(e => e.AssetId);
            entity.HasOne(e => e.ReportedByEmployee).WithMany(emp => emp.AssetRepairs).HasForeignKey(e => e.ReportedByEmployeeId);
            entity.HasOne(e => e.HandledByExecutive).WithMany(u => u.HandledRepairs).HasForeignKey(e => e.HandledByExecutiveId).OnDelete(DeleteBehavior.SetNull);
        });

        // AssetReplacements
        modelBuilder.Entity<AssetReplacement>(entity =>
        {
            entity.HasKey(e => e.ReplacementId);
            entity.Property(e => e.Reason).HasMaxLength(500).IsRequired();
            entity.Property(e => e.Status).HasConversion<string>().HasMaxLength(30);
            entity.Property(e => e.RejectionReason).HasMaxLength(500);

            entity.HasOne(e => e.OldAsset).WithMany(a => a.ReplacementsAsOld).HasForeignKey(e => e.OldAssetId);
            entity.HasOne(e => e.NewAsset).WithMany(a => a.ReplacementsAsNew).HasForeignKey(e => e.NewAssetId).OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(e => e.Employee).WithMany(emp => emp.AssetReplacements).HasForeignKey(e => e.EmployeeId);
            entity.HasOne(e => e.ApprovedByUser).WithMany().HasForeignKey(e => e.ApprovedBy).OnDelete(DeleteBehavior.SetNull);
        });

        // Notifications
        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasKey(e => e.NotificationId);
            entity.Property(e => e.Title).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Message).HasMaxLength(1000).IsRequired();
            entity.Property(e => e.Type).HasMaxLength(50).IsRequired();
            entity.Property(e => e.RelatedEntity).HasMaxLength(100);
            entity.HasIndex(e => new { e.UserId, e.IsRead });

            entity.HasOne(e => e.User).WithMany(u => u.Notifications).HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        // AuditLogs
        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.HasKey(e => e.AuditId);
            entity.Property(e => e.Action).HasMaxLength(100).IsRequired();
            entity.Property(e => e.EntityName).HasMaxLength(100).IsRequired();
            entity.Property(e => e.EntityId).HasMaxLength(50);
            entity.Property(e => e.IpAddress).HasMaxLength(50);

            entity.HasOne(e => e.User).WithMany().HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.SetNull);
        });
    }
}
