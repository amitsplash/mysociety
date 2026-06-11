using Microsoft.EntityFrameworkCore;
using MySociety.Application.Common.Interfaces;
using MySociety.Domain.Entities;
using MySociety.Infrastructure.Persistence;

namespace MySociety.Infrastructure.Repositories;

public class UserRepository : IUserRepository
{
    private readonly AppDbContext _dbContext;

    public UserRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return _dbContext.Users.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public Task<User?> GetByPhoneAsync(string phone, CancellationToken cancellationToken)
    {
        return _dbContext.Users.FirstOrDefaultAsync(x => x.Phone == phone, cancellationToken);
    }

    public Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken)
    {
        return _dbContext.Users.FirstOrDefaultAsync(x => x.Username == username, cancellationToken);
    }

    public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken)
    {
        return _dbContext.Users.FirstOrDefaultAsync(x => x.Email == email, cancellationToken);
    }

    public Task<User?> GetByUsernameOrPhoneWithMembershipsAsync(
        string usernameOrPhone,
        CancellationToken cancellationToken)
    {
        var key = usernameOrPhone.Trim();
        return _dbContext.Users
            .Include(x => x.Memberships)
            .ThenInclude(x => x.Group)
            .FirstOrDefaultAsync(x => x.Username == key || x.Phone == key, cancellationToken);
    }

    public Task<User?> GetByPhoneWithMembershipsAsync(string phone, CancellationToken cancellationToken)
    {
        return _dbContext.Users
            .Include(x => x.Memberships)
            .ThenInclude(x => x.Group)
            .FirstOrDefaultAsync(x => x.Phone == phone, cancellationToken);
    }

    public Task<User?> GetByUsernameWithMembershipsAsync(string username, CancellationToken cancellationToken)
    {
        return _dbContext.Users
            .Include(x => x.Memberships)
            .ThenInclude(x => x.Group)
            .FirstOrDefaultAsync(x => x.Username == username, cancellationToken);
    }

    public Task<User?> GetByEmailWithMembershipsAsync(string email, CancellationToken cancellationToken)
    {
        var normalized = email.Trim().ToLowerInvariant();
        return _dbContext.Users
            .Include(x => x.Memberships)
            .ThenInclude(x => x.Group)
            .FirstOrDefaultAsync(x => x.Email == normalized, cancellationToken);
    }

    public Task<bool> ExistsByUsernameAsync(string username, CancellationToken cancellationToken)
    {
        return _dbContext.Users.AnyAsync(x => x.Username == username, cancellationToken);
    }

    public Task<bool> ExistsByPhoneAsync(string phone, CancellationToken cancellationToken)
    {
        return _dbContext.Users.AnyAsync(x => x.Phone == phone, cancellationToken);
    }

    public Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken)
    {
        return _dbContext.Users.AnyAsync(x => x.Email == email, cancellationToken);
    }

    public async Task AddAsync(User user, CancellationToken cancellationToken)
    {
        await _dbContext.Users.AddAsync(user, cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}
