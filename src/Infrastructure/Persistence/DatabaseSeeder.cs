using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MySociety.Application.Common.Interfaces;
using MySociety.Application.Financial;
using MySociety.Domain.Entities;
using MySociety.Domain.Enums;

namespace MySociety.Infrastructure.Persistence;

public class DatabaseSeeder
{
    private readonly AppDbContext _dbContext;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ILedgerService _ledgerService;
    private readonly ILogger<DatabaseSeeder> _logger;

    public DatabaseSeeder(
        AppDbContext dbContext,
        IPasswordHasher passwordHasher,
        ILedgerService ledgerService,
        ILogger<DatabaseSeeder> logger)
    {
        _dbContext = dbContext;
        _passwordHasher = passwordHasher;
        _ledgerService = ledgerService;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        if (await _dbContext.Groups.AnyAsync(cancellationToken))
        {
            return;
        }

        _logger.LogInformation("Seeding database with demo data...");

        var passwordHash = _passwordHasher.Hash("Password123!");

        var demoUser = new User
        {
            Id = Guid.Parse("11111111-1111-1111-1111-111111111101"),
            Username = "demo",
            Email = "demo@example.com",
            Name = "Demo User",
            PasswordHash = passwordHash,
            CreatedAt = DateTime.UtcNow
        };

        var memberUser = new User
        {
            Id = Guid.Parse("11111111-1111-1111-1111-111111111102"),
            Username = "user_9000000002",
            Email = "9000000002@invite.local",
            Name = "Amit Jain",
            Phone = "9000000002",
            PasswordHash = passwordHash,
            CreatedAt = DateTime.UtcNow
        };

        var memberUser2 = new User
        {
            Id = Guid.Parse("11111111-1111-1111-1111-111111111103"),
            Username = "user_9000000003",
            Email = "9000000003@invite.local",
            Name = "Priya Sharma",
            Phone = "9000000003",
            PasswordHash = passwordHash,
            CreatedAt = DateTime.UtcNow
        };

        var group = new Group
        {
            Id = Guid.Parse("22222222-2222-2222-2222-222222222201"),
            Name = "Sunrise RWA",
            Type = GroupType.Rwa,
            ContributionModel = ContributionModel.Fixed,
            ContributionAmount = 2000m,
            ContributionFrequency = ContributionFrequency.Monthly,
            OpeningCorpusBalance = 500000m,
            CreatedByUserId = demoUser.Id,
            CreatedAt = DateTime.UtcNow
        };

        var adminMember = new Member
        {
            Id = Guid.Parse("33333333-3333-3333-3333-333333333301"),
            GroupId = group.Id,
            UserId = demoUser.Id,
            Role = MemberRole.Admin,
            CorpusAmount = 100000m,
            CorpusPaidAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };

        var member1 = new Member
        {
            Id = Guid.Parse("33333333-3333-3333-3333-333333333302"),
            GroupId = group.Id,
            UserId = memberUser.Id,
            Role = MemberRole.Member,
            CorpusAmount = 100000m,
            CorpusPaidAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };

        var member2 = new Member
        {
            Id = Guid.Parse("33333333-3333-3333-3333-333333333303"),
            GroupId = group.Id,
            UserId = memberUser2.Id,
            Role = MemberRole.Member,
            CorpusAmount = 100000m,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.Users.AddRange(demoUser, memberUser, memberUser2);
        _dbContext.Groups.Add(group);
        _dbContext.Members.AddRange(adminMember, member1, member2);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _ledgerService.RecordOpeningBalanceAsync(member1.Id, group.Id, -1000m, cancellationToken);
        await _ledgerService.RecordOpeningBalanceAsync(member2.Id, group.Id, 500m, cancellationToken);

        _logger.LogInformation(
            "Seed complete. Demo user: demo / Password123!; invite members sign in with phone. Admin member id: {MemberId}",
            adminMember.Id);
    }
}
