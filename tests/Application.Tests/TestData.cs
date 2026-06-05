using MySociety.Application.Groups;
using MySociety.Application.Groups.Dtos;
using MySociety.Application.Groups.Validators;
using MySociety.Application.Members;
using MySociety.Application.Members.Validators;
using MySociety.Domain.Entities;
using MySociety.Domain.Enums;
using MySociety.Infrastructure.Persistence;
using MySociety.Infrastructure.Repositories;
using MySociety.Infrastructure.Security;
using MySociety.Infrastructure.Services;

namespace MySociety.Application.Tests;

internal static class TestData
{
    public static async Task<User> AddRegisteredUserAsync(
        AppDbContext context,
        string username,
        string email,
        string name,
        string? phone = null,
        string passwordHash = "test-hash",
        CancellationToken cancellationToken = default)
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = username,
            Email = email,
            Name = name,
            Phone = phone,
            PasswordHash = passwordHash,
            CreatedAt = DateTime.UtcNow,
        };
        await context.Users.AddAsync(user, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        return user;
    }

    public static async Task<User> AddUserAsync(
        AppDbContext context,
        string phone,
        string name,
        CancellationToken cancellationToken = default)
    {
        return await AddRegisteredUserAsync(
            context,
            $"user_{phone}",
            $"{phone}@invite.local",
            name,
            phone,
            cancellationToken: cancellationToken);
    }

    public static CreateGroupRequest DefaultCreateGroupRequest(
        string name = "Test Group",
        decimal openingMaintenanceBalance = 0m,
        decimal openingCorpusBalance = 0m,
        decimal creatorOpeningBalance = 0m) =>
        new(
            name,
            GroupType.Friends,
            ContributionModel.Fixed,
            1000m,
            ContributionFrequency.Quarterly,
            openingMaintenanceBalance,
            openingCorpusBalance,
            creatorOpeningBalance);

    public static GroupService CreateGroupService(AppDbContext context) =>
        new(
            new GroupRepository(context),
            new MemberRepository(context),
            new UserRepository(context),
            CreateMemberService(context),
            new CreateGroupRequestValidator(),
            new UpdateGroupRequestValidator());

    public static MemberService CreateMemberService(AppDbContext context) =>
        new(
            new GroupRepository(context),
            new MemberRepository(context),
            new UserRepository(context),
            new LedgerService(context),
            new UnitOfWork(context),
            new MemberInviteRepository(context),
            new PasswordResetRepository(context),
            new InviteCodeService(),
            new CreateMemberRequestValidator(),
            new UpdateMemberRequestValidator());
}
