using iAdmin.Common.Models;
using iAdmin.Data.Context;
using Microsoft.EntityFrameworkCore;

namespace iAdmin.Data.Repositories;

public interface IUserRepository : IRepository<User>
{
    Task<User?> GetByUsernameAsync(string username);
    Task<User?> GetByEmailAsync(string email);
}

public class UserRepository : Repository<User>, IUserRepository
{
    public UserRepository(IAdminDbContext context) : base(context)
    {
    }

    public async Task<User?> GetByUsernameAsync(string username)
    {
        return await DbSet.FirstOrDefaultAsync(u => u.Username == username);
    }

    public async Task<User?> GetByEmailAsync(string email)
    {
        return await DbSet.FirstOrDefaultAsync(u => u.Email == email);
    }
}

public interface IAdminSessionRepository : IRepository<AdminSession>
{
    Task<AdminSession?> GetActiveSessionAsync(Guid userId);
    Task<IEnumerable<AdminSession>> GetUserSessionsAsync(Guid userId, int limit = 10);
}

public class AdminSessionRepository : Repository<AdminSession>, IAdminSessionRepository
{
    public AdminSessionRepository(IAdminDbContext context) : base(context)
    {
    }

    public async Task<AdminSession?> GetActiveSessionAsync(Guid userId)
    {
        return await DbSet
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.UserId == userId && 
                s.Status == Common.Enums.AdminSessionStatus.Active);
    }

    public async Task<IEnumerable<AdminSession>> GetUserSessionsAsync(Guid userId, int limit = 10)
    {
        return await DbSet
            .AsNoTracking()
            .Where(s => s.UserId == userId)
            .OrderByDescending(s => s.StartTime)
            .Take(limit)
            .ToListAsync();
    }
}

public interface IAuditLogRepository : IRepository<AuditLog>
{
    Task<IEnumerable<AuditLog>> GetUserAuditLogsAsync(Guid userId, int days = 30, int limit = 100);
    Task<IEnumerable<AuditLog>> GetActionLogsAsync(Common.Enums.AuditActionType actionType, int days = 30);
}

public class AuditLogRepository : Repository<AuditLog>, IAuditLogRepository
{
    public AuditLogRepository(IAdminDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<AuditLog>> GetUserAuditLogsAsync(Guid userId, int days = 30, int limit = 100)
    {
        var since = DateTime.UtcNow.AddDays(-days);
        return await DbSet
            .AsNoTracking()
            .Where(a => a.UserId == userId && a.ChangedAt >= since)
            .OrderByDescending(a => a.ChangedAt)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<IEnumerable<AuditLog>> GetActionLogsAsync(Common.Enums.AuditActionType actionType, int days = 30)
    {
        var since = DateTime.UtcNow.AddDays(-days);
        return await DbSet
            .AsNoTracking()
            .Where(a => a.ActionType == actionType && a.ChangedAt >= since)
            .OrderByDescending(a => a.ChangedAt)
            .ToListAsync();
    }
}

public interface IUpdateHistoryRepository : IRepository<UpdateHistory>
{
    Task<UpdateHistory?> GetLatestAppliedAsync();
    Task<IEnumerable<UpdateHistory>> GetRecentUpdatesAsync(int days = 30);
}

public class UpdateHistoryRepository : Repository<UpdateHistory>, IUpdateHistoryRepository
{
    public UpdateHistoryRepository(IAdminDbContext context) : base(context)
    {
    }

    public async Task<UpdateHistory?> GetLatestAppliedAsync()
    {
        return await DbSet
            .AsNoTracking()
            .OrderByDescending(u => u.AppliedAt)
            .FirstOrDefaultAsync();
    }

    public async Task<IEnumerable<UpdateHistory>> GetRecentUpdatesAsync(int days = 30)
    {
        var since = DateTime.UtcNow.AddDays(-days);
        return await DbSet
            .AsNoTracking()
            .Where(u => u.AppliedAt >= since)
            .OrderByDescending(u => u.AppliedAt)
            .ToListAsync();
    }
}
