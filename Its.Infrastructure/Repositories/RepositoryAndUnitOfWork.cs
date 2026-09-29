using Its.Application.Interfaces;
using Its.Domain.Entities;
using Its.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System.Linq.Expressions;

namespace Its.Infrastructure.Repositories;

public class Repository<T> : IRepository<T> where T : class
{
    protected readonly ApplicationDbContext _context;
    protected readonly DbSet<T> _dbSet;

    public Repository(ApplicationDbContext context)
    {
        _context = context;
        _dbSet = context.Set<T>();
    }

    public async Task<T?> GetByIdAsync(object id, CancellationToken cancellationToken = default)
    {
        return await _dbSet.FindAsync(new[] { id }, cancellationToken);
    }

    public async Task<IReadOnlyList<T>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet.ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<T>> FindAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
    {
        return await _dbSet.Where(predicate).ToListAsync(cancellationToken);
    }

    public async Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
    {
        return await _dbSet.FirstOrDefaultAsync(predicate, cancellationToken);
    }

    public async Task<bool> AnyAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
    {
        return await _dbSet.AnyAsync(predicate, cancellationToken);
    }

    public async Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null, CancellationToken cancellationToken = default)
    {
        return predicate != null
            ? await _dbSet.CountAsync(predicate, cancellationToken)
            : await _dbSet.CountAsync(cancellationToken);
    }

    public async Task AddAsync(T entity, CancellationToken cancellationToken = default)
    {
        await _dbSet.AddAsync(entity, cancellationToken);
    }

    public async Task AddRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default)
    {
        await _dbSet.AddRangeAsync(entities, cancellationToken);
    }

    public void Update(T entity)
    {
        _dbSet.Update(entity);
    }

    public void Remove(T entity)
    {
        _dbSet.Remove(entity);
    }

    public IQueryable<T> Query()
    {
        return _dbSet.AsQueryable();
    }
}

public class UnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext _context;
    private IDbContextTransaction? _currentTransaction;

    public IRepository<Department> Departments { get; }
    public IRepository<Employee> Employees { get; }
    public IRepository<User> Users { get; }
    public IRepository<Role> Roles { get; }
    public IRepository<Permission> Permissions { get; }
    public IRepository<UserRole> UserRoles { get; }
    public IRepository<RolePermission> RolePermissions { get; }
    public IRepository<RefreshToken> RefreshTokens { get; }
    public IRepository<AssetCategory> AssetCategories { get; }
    public IRepository<ITAsset> Assets { get; }
    public IRepository<AssetRequest> AssetRequests { get; }
    public IRepository<AssetRequestHistory> RequestHistories { get; }
    public IRepository<AssetAllocation> AssetAllocations { get; }
    public IRepository<AssetRepair> AssetRepairs { get; }
    public IRepository<AssetReplacement> AssetReplacements { get; }
    public IRepository<Notification> Notifications { get; }
    public IRepository<AuditLog> AuditLogs { get; }

    public UnitOfWork(ApplicationDbContext context)
    {
        _context = context;
        Departments = new Repository<Department>(context);
        Employees = new Repository<Employee>(context);
        Users = new Repository<User>(context);
        Roles = new Repository<Role>(context);
        Permissions = new Repository<Permission>(context);
        UserRoles = new Repository<UserRole>(context);
        RolePermissions = new Repository<RolePermission>(context);
        RefreshTokens = new Repository<RefreshToken>(context);
        AssetCategories = new Repository<AssetCategory>(context);
        Assets = new Repository<ITAsset>(context);
        AssetRequests = new Repository<AssetRequest>(context);
        RequestHistories = new Repository<AssetRequestHistory>(context);
        AssetAllocations = new Repository<AssetAllocation>(context);
        AssetRepairs = new Repository<AssetRepair>(context);
        AssetReplacements = new Repository<AssetReplacement>(context);
        Notifications = new Repository<Notification>(context);
        AuditLogs = new Repository<AuditLog>(context);
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_currentTransaction != null) return;
        _currentTransaction = await _context.Database.BeginTransactionAsync(cancellationToken);
    }

    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_currentTransaction == null) return;
        try
        {
            await _context.SaveChangesAsync(cancellationToken);
            await _currentTransaction.CommitAsync(cancellationToken);
        }
        finally
        {
            await _currentTransaction.DisposeAsync();
            _currentTransaction = null;
        }
    }

    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_currentTransaction == null) return;
        try
        {
            await _currentTransaction.RollbackAsync(cancellationToken);
        }
        finally
        {
            await _currentTransaction.DisposeAsync();
            _currentTransaction = null;
        }
    }

    public void Dispose()
    {
        _currentTransaction?.Dispose();
        _context.Dispose();
        GC.SuppressFinalize(this);
    }

    public async Task ExecuteInTransactionAsync(
    Func<Task> operation,
    CancellationToken cancellationToken = default)
    {
        var strategy = _context.Database.CreateExecutionStrategy();

        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction =
                await _context.Database.BeginTransactionAsync(
                    cancellationToken);

            try
            {
                await operation();

                await _context.SaveChangesAsync(cancellationToken);

                await transaction.CommitAsync(cancellationToken);
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        });
    }

}
