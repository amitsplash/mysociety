using FluentValidation;
using MySociety.Application.Common.Authorization;
using MySociety.Application.Common.Exceptions;
using MySociety.Application.Common.Interfaces;
using MySociety.Application.Financial;
using MySociety.Application.Members.Dtos;
using MySociety.Domain.Entities;
using MySociety.Domain.Enums;
using ValidationException = MySociety.Application.Common.Exceptions.ValidationException;

namespace MySociety.Application.Members;

public interface IMemberService
{
    Task<MemberResponse> AddGroupCreatorAsAdminAsync(
        Guid groupId,
        Guid userId,
        decimal openingBalance,
        decimal? squareFeet,
        decimal corpusAmount,
        bool corpusPaid,
        CancellationToken cancellationToken);

    Task<CreateMemberResponse> CreateAsync(CreateMemberRequest request, Guid actingMemberId, CancellationToken cancellationToken);
    Task<IReadOnlyList<MemberResponse>> GetByGroupIdAsync(Guid groupId, Guid actingMemberId, CancellationToken cancellationToken);
    Task<MemberResponse> UpdateAsync(Guid id, UpdateMemberRequest request, Guid actingMemberId, CancellationToken cancellationToken);
    Task DeleteAsync(Guid id, Guid actingMemberId, CancellationToken cancellationToken);
    Task<MarkCorpusReceivedResponse> MarkCorpusReceivedAsync(
        Guid memberId,
        Guid actingMemberId,
        CancellationToken cancellationToken);
    Task<IssuePasswordResetResponse> IssuePasswordResetAsync(
        Guid memberId,
        Guid actingMemberId,
        CancellationToken cancellationToken);
}

public class MemberService : IMemberService
{
    private readonly IGroupRepository _groupRepository;
    private readonly IMemberRepository _memberRepository;
    private readonly IUserRepository _userRepository;
    private readonly ILedgerService _ledgerService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMemberInviteRepository _memberInviteRepository;
    private readonly IPasswordResetRepository _passwordResetRepository;
    private readonly IInviteCodeService _inviteCodeService;
    private readonly IValidator<CreateMemberRequest> _createValidator;
    private readonly IValidator<UpdateMemberRequest> _updateValidator;

    private const int PasswordResetExpiryDays = 7;

    public MemberService(
        IGroupRepository groupRepository,
        IMemberRepository memberRepository,
        IUserRepository userRepository,
        ILedgerService ledgerService,
        IUnitOfWork unitOfWork,
        IMemberInviteRepository memberInviteRepository,
        IPasswordResetRepository passwordResetRepository,
        IInviteCodeService inviteCodeService,
        IValidator<CreateMemberRequest> createValidator,
        IValidator<UpdateMemberRequest> updateValidator)
    {
        _groupRepository = groupRepository;
        _memberRepository = memberRepository;
        _userRepository = userRepository;
        _ledgerService = ledgerService;
        _unitOfWork = unitOfWork;
        _memberInviteRepository = memberInviteRepository;
        _passwordResetRepository = passwordResetRepository;
        _inviteCodeService = inviteCodeService;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    public async Task<MemberResponse> AddGroupCreatorAsAdminAsync(
        Guid groupId,
        Guid userId,
        decimal openingBalance,
        decimal? squareFeet,
        decimal corpusAmount,
        bool corpusPaid,
        CancellationToken cancellationToken)
    {
        var group = await _groupRepository.GetByIdAsync(groupId, cancellationToken)
            ?? throw new NotFoundException("Group not found.");

        var user = await _userRepository.GetByIdAsync(userId, cancellationToken)
            ?? throw new NotFoundException("User not found.");

        if (await _memberRepository.ExistsInGroupAsync(groupId, userId, cancellationToken))
        {
            throw new ConflictException("User is already a member of this group.");
        }

        var member = new Member
        {
            Id = Guid.NewGuid(),
            GroupId = groupId,
            UserId = userId,
            Role = MemberRole.Admin,
            SquareFeet = squareFeet,
            CorpusAmount = corpusAmount,
            CorpusPaidAt = corpusAmount > 0 && corpusPaid ? DateTime.UtcNow : null,
            CreatedAt = DateTime.UtcNow
        };

        await _memberRepository.AddAsync(member, cancellationToken);
        await _memberRepository.SaveChangesAsync(cancellationToken);

        if (openingBalance != 0)
        {
            await _ledgerService.RecordOpeningBalanceAsync(
                member.Id,
                member.GroupId,
                openingBalance,
                cancellationToken);
        }

        // Pre-paid corpus at onboarding is covered by group OpeningCorpusBalance — no ledger inflow.

        return MapMember(member, user);
    }

    public async Task<CreateMemberResponse> CreateAsync(
        CreateMemberRequest request,
        Guid actingMemberId,
        CancellationToken cancellationToken)
    {
        await ValidateAsync(_createValidator, request, cancellationToken);

        await MemberAuthorization.EnsureGroupAdminAsync(
            _memberRepository, actingMemberId, request.GroupId, cancellationToken);

        var group = await _groupRepository.GetByIdAsync(request.GroupId, cancellationToken)
            ?? throw new NotFoundException("Group not found.");

        if (group.ContributionModel == ContributionModel.PerSquareFeet && !request.SquareFeet.HasValue)
        {
            throw new ValidationException("Square feet is required for per-square-feet contribution groups.");
        }

        return await CreateMemberInternalAsync(
            request.GroupId,
            request.Name,
            request.Phone,
            request.Role,
            request.OpeningBalance,
            request.SquareFeet,
            request.CorpusAmount,
            request.CorpusPaid,
            actingMemberId,
            cancellationToken);
    }

    private async Task<CreateMemberResponse> CreateMemberInternalAsync(
        Guid groupId,
        string name,
        string phone,
        MemberRole role,
        decimal openingBalance,
        decimal? squareFeet,
        decimal corpusAmount,
        bool corpusPaid,
        Guid? actingMemberId,
        CancellationToken cancellationToken)
    {
        MemberResponse? memberResponse = null;
        var requiresActivation = false;
        string? inviteCode = null;
        DateTime? inviteExpiresAt = null;

        await _unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            var trimmedPhone = phone.Trim();
            var user = await _userRepository.GetByPhoneAsync(trimmedPhone, ct);
            if (user is null)
            {
                user = new User
                {
                    Id = Guid.NewGuid(),
                    Username = InviteUserCredentials.UsernameForPhone(trimmedPhone),
                    Email = InviteUserCredentials.EmailForPhone(trimmedPhone),
                    Name = name.Trim(),
                    Phone = trimmedPhone,
                    PasswordHash = string.Empty,
                    CreatedAt = DateTime.UtcNow
                };
                await _userRepository.AddAsync(user, ct);
                await _userRepository.SaveChangesAsync(ct);
            }
            else if (!string.Equals(user.Name, name.Trim(), StringComparison.Ordinal))
            {
                user.Name = name.Trim();
            }

            if (await _memberRepository.ExistsInGroupAsync(groupId, user.Id, ct))
            {
                throw new ConflictException("Member already exists in this group.");
            }

            var member = new Member
            {
                Id = Guid.NewGuid(),
                GroupId = groupId,
                UserId = user.Id,
                Role = role,
                SquareFeet = squareFeet,
                CorpusAmount = corpusAmount,
                CorpusPaidAt = corpusAmount > 0 && corpusPaid ? DateTime.UtcNow : null,
                CreatedAt = DateTime.UtcNow
            };

            await _memberRepository.AddAsync(member, ct);
            await _memberRepository.SaveChangesAsync(ct);

            if (openingBalance != 0)
            {
                await _ledgerService.RecordOpeningBalanceAsync(
                    member.Id,
                    member.GroupId,
                    openingBalance,
                    ct);
            }

            // Pre-paid corpus at onboarding is covered by group OpeningCorpusBalance — no ledger inflow.

            memberResponse = MapMember(member, user);

            if (string.IsNullOrWhiteSpace(user.PasswordHash))
            {
                requiresActivation = true;
                inviteCode = _inviteCodeService.GenerateCode();
                inviteExpiresAt = DateTime.UtcNow.AddDays(7);
                var invite = new MemberInvite
                {
                    Id = Guid.NewGuid(),
                    MemberId = member.Id,
                    CodeHash = _inviteCodeService.HashCode(inviteCode),
                    ExpiresAt = inviteExpiresAt.Value,
                    CreatedByMemberId = actingMemberId ?? member.Id,
                    CreatedAt = DateTime.UtcNow
                };
                await _memberInviteRepository.AddAsync(invite, ct);
                await _memberInviteRepository.SaveChangesAsync(ct);
            }
        }, cancellationToken);

        return new CreateMemberResponse(
            memberResponse!,
            requiresActivation,
            inviteCode,
            inviteExpiresAt);
    }

    public async Task<IReadOnlyList<MemberResponse>> GetByGroupIdAsync(
        Guid groupId,
        Guid actingMemberId,
        CancellationToken cancellationToken)
    {
        _ = await _groupRepository.GetByIdAsync(groupId, cancellationToken)
            ?? throw new NotFoundException("Group not found.");

        await MemberAuthorization.EnsureGroupAdminAsync(
            _memberRepository, actingMemberId, groupId, cancellationToken);

        var members = await _memberRepository.GetByGroupIdAsync(groupId, cancellationToken);
        return members.Select(x => MapMember(x, x.User)).ToList();
    }

    public async Task<MemberResponse> UpdateAsync(
        Guid id,
        UpdateMemberRequest request,
        Guid actingMemberId,
        CancellationToken cancellationToken)
    {
        await ValidateAsync(_updateValidator, request, cancellationToken);

        var member = await _memberRepository.GetByIdWithUserAsync(id, cancellationToken)
            ?? throw new NotFoundException("Member not found.");

        await MemberAuthorization.EnsureGroupAdminAsync(
            _memberRepository, actingMemberId, member.GroupId, cancellationToken);

        var group = await _groupRepository.GetByIdAsync(member.GroupId, cancellationToken)
            ?? throw new NotFoundException("Group not found.");

        if (group.ContributionModel == ContributionModel.PerSquareFeet && !request.SquareFeet.HasValue)
        {
            throw new ValidationException("Square feet is required for per-square-feet contribution groups.");
        }

        var nextName = request.Name.Trim();
        var nextPhone = request.Phone.Trim();
        if (!string.Equals(member.User.Phone, nextPhone, StringComparison.Ordinal))
        {
            var existingUser = await _userRepository.GetByPhoneAsync(nextPhone, cancellationToken);
            if (existingUser is not null && existingUser.Id != member.UserId)
            {
                throw new ConflictException("Phone already linked to another user.");
            }
        }

        member.User.Name = nextName;
        member.User.Phone = nextPhone;
        member.Role = request.Role;
        member.SquareFeet = request.SquareFeet;

        await _memberRepository.SaveChangesAsync(cancellationToken);
        return MapMember(member, member.User);
    }

    public async Task DeleteAsync(Guid id, Guid actingMemberId, CancellationToken cancellationToken)
    {
        var member = await _memberRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException("Member not found.");

        await MemberAuthorization.EnsureGroupAdminAsync(
            _memberRepository, actingMemberId, member.GroupId, cancellationToken);

        if (member.Id == actingMemberId)
        {
            throw new ValidationException("You cannot remove yourself.");
        }

        await _memberRepository.RemoveAsync(member, cancellationToken);
        await _memberRepository.SaveChangesAsync(cancellationToken);
    }

    public async Task<IssuePasswordResetResponse> IssuePasswordResetAsync(
        Guid memberId,
        Guid actingMemberId,
        CancellationToken cancellationToken)
    {
        var member = await _memberRepository.GetByIdWithUserAsync(memberId, cancellationToken)
            ?? throw new NotFoundException("Member not found.");

        await MemberAuthorization.EnsureGroupAdminAsync(
            _memberRepository, actingMemberId, member.GroupId, cancellationToken);

        if (string.IsNullOrWhiteSpace(member.User.PasswordHash))
        {
            throw new ValidationException(
                "This member has not activated their account yet. Share their invite code instead.");
        }

        await _passwordResetRepository.InvalidateActiveByUserIdAsync(member.UserId, cancellationToken);

        var resetCode = _inviteCodeService.GenerateCode();
        var expiresAt = DateTime.UtcNow.AddDays(PasswordResetExpiryDays);
        var token = new PasswordResetToken
        {
            Id = Guid.NewGuid(),
            UserId = member.UserId,
            CodeHash = _inviteCodeService.HashCode(resetCode),
            ExpiresAt = expiresAt,
            CreatedByMemberId = actingMemberId,
            CreatedAt = DateTime.UtcNow
        };

        await _passwordResetRepository.AddAsync(token, cancellationToken);
        await _passwordResetRepository.SaveChangesAsync(cancellationToken);

        return new IssuePasswordResetResponse(
            member.User.Phone,
            member.User.Username,
            member.User.Name,
            resetCode,
            expiresAt);
    }

    public async Task<MarkCorpusReceivedResponse> MarkCorpusReceivedAsync(
        Guid memberId,
        Guid actingMemberId,
        CancellationToken cancellationToken)
    {
        var member = await _memberRepository.GetByIdWithUserAsync(memberId, cancellationToken)
            ?? throw new NotFoundException("Member not found.");

        await MemberAuthorization.EnsureGroupAdminAsync(
            _memberRepository, actingMemberId, member.GroupId, cancellationToken);

        if (member.CorpusAmount <= 0)
        {
            throw new ValidationException("This member has no corpus amount defined.");
        }

        if (member.CorpusPaidAt.HasValue)
        {
            throw new ValidationException("Corpus has already been marked as received for this member.");
        }

        member.CorpusPaidAt = DateTime.UtcNow;
        await _memberRepository.SaveChangesAsync(cancellationToken);

        await _ledgerService.RecordCorpusPaymentAsync(
            member.Id,
            member.GroupId,
            member.CorpusAmount,
            cancellationToken);

        var corpusBalance = await _ledgerService.GetCorpusFundBalanceAsync(member.GroupId, cancellationToken);

        return new MarkCorpusReceivedResponse(
            MapMember(member, member.User),
            member.CorpusAmount,
            corpusBalance.Balance);
    }

    private static MemberResponse MapMember(Member member, User user)
    {
        return new MemberResponse(
            member.Id,
            member.GroupId,
            user.Name,
            user.Phone,
            member.Role,
            member.SquareFeet,
            member.CorpusAmount,
            member.CorpusPaidAt,
            member.CreatedAt);
    }

    private static async Task ValidateAsync<T>(IValidator<T> validator, T instance, CancellationToken cancellationToken)
    {
        var result = await validator.ValidateAsync(instance, cancellationToken);
        if (!result.IsValid)
        {
            throw new ValidationException(result.Errors.Select(x => x.ErrorMessage));
        }
    }
}

internal static class InviteUserCredentials
{
    public static string UsernameForPhone(string phone) => $"user_{phone}";

    public static string EmailForPhone(string phone) => $"{phone}@invite.local";
}
