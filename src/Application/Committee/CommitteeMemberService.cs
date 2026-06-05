using FluentValidation;
using MySociety.Application.Common.Authorization;
using MySociety.Application.Common.Exceptions;
using MySociety.Application.Common.Interfaces;
using MySociety.Application.Committee.Dtos;
using MySociety.Domain.Entities;
using MySociety.Domain.Enums;
using ValidationException = MySociety.Application.Common.Exceptions.ValidationException;

namespace MySociety.Application.Committee;

public interface ICommitteeMemberService
{
    Task<IReadOnlyList<CommitteeMemberResponse>> GetByGroupIdAsync(
        Guid groupId,
        Guid actingMemberId,
        CancellationToken cancellationToken);

    Task<CommitteeMemberResponse> CreateAsync(
        CreateCommitteeMemberRequest request,
        Guid actingMemberId,
        CancellationToken cancellationToken);

    Task<CommitteeMemberResponse> UpdateAsync(
        Guid id,
        UpdateCommitteeMemberRequest request,
        Guid actingMemberId,
        CancellationToken cancellationToken);

    Task DeleteAsync(
        Guid id,
        Guid actingMemberId,
        CancellationToken cancellationToken);
}

public class CommitteeMemberService : ICommitteeMemberService
{
    private readonly IGroupRepository _groupRepository;
    private readonly IMemberRepository _memberRepository;
    private readonly ICommitteeMemberRepository _committeeMemberRepository;
    private readonly IValidator<CreateCommitteeMemberRequest> _createValidator;
    private readonly IValidator<UpdateCommitteeMemberRequest> _updateValidator;

    public CommitteeMemberService(
        IGroupRepository groupRepository,
        IMemberRepository memberRepository,
        ICommitteeMemberRepository committeeMemberRepository,
        IValidator<CreateCommitteeMemberRequest> createValidator,
        IValidator<UpdateCommitteeMemberRequest> updateValidator)
    {
        _groupRepository = groupRepository;
        _memberRepository = memberRepository;
        _committeeMemberRepository = committeeMemberRepository;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    public async Task<IReadOnlyList<CommitteeMemberResponse>> GetByGroupIdAsync(
        Guid groupId,
        Guid actingMemberId,
        CancellationToken cancellationToken)
    {
        _ = await _groupRepository.GetByIdAsync(groupId, cancellationToken)
            ?? throw new NotFoundException("Group not found.");

        await MemberAuthorization.EnsureGroupMemberAsync(
            _memberRepository, actingMemberId, groupId, cancellationToken);

        var committeeMembers = await _committeeMemberRepository.GetByGroupIdAsync(groupId, cancellationToken);
        return committeeMembers.Select(MapToResponse).ToList();
    }

    public async Task<CommitteeMemberResponse> CreateAsync(
        CreateCommitteeMemberRequest request,
        Guid actingMemberId,
        CancellationToken cancellationToken)
    {
        await ValidateAsync(_createValidator, request, cancellationToken);

        _ = await _groupRepository.GetByIdAsync(request.GroupId, cancellationToken)
            ?? throw new NotFoundException("Group not found.");

        await MemberAuthorization.EnsureGroupAdminAsync(
            _memberRepository, actingMemberId, request.GroupId, cancellationToken);

        var member = await _memberRepository.GetByIdWithUserAsync(request.MemberId, cancellationToken)
            ?? throw new NotFoundException("Member not found.");

        if (member.GroupId != request.GroupId)
        {
            throw new ValidationException("Member does not belong to this group.");
        }

        if (await _committeeMemberRepository.ExistsForMemberAsync(
                request.GroupId, request.MemberId, cancellationToken))
        {
            throw new ConflictException("Member is already on the committee.");
        }

        await EnsureOfficerRoleAvailableAsync(request.GroupId, request.Role, excludeId: null, cancellationToken);

        var committeeMember = new CommitteeMember
        {
            Id = Guid.NewGuid(),
            GroupId = request.GroupId,
            MemberId = request.MemberId,
            Role = request.Role,
            CreatedAt = DateTime.UtcNow,
        };

        await _committeeMemberRepository.AddAsync(committeeMember, cancellationToken);
        await _committeeMemberRepository.SaveChangesAsync(cancellationToken);

        committeeMember.Member = member;
        return MapToResponse(committeeMember);
    }

    public async Task<CommitteeMemberResponse> UpdateAsync(
        Guid id,
        UpdateCommitteeMemberRequest request,
        Guid actingMemberId,
        CancellationToken cancellationToken)
    {
        await ValidateAsync(_updateValidator, request, cancellationToken);

        var committeeMember = await _committeeMemberRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException("Committee member not found.");

        await MemberAuthorization.EnsureGroupAdminAsync(
            _memberRepository, actingMemberId, committeeMember.GroupId, cancellationToken);

        await EnsureOfficerRoleAvailableAsync(
            committeeMember.GroupId, request.Role, committeeMember.Id, cancellationToken);

        committeeMember.Role = request.Role;
        await _committeeMemberRepository.SaveChangesAsync(cancellationToken);

        return MapToResponse(committeeMember);
    }

    public async Task DeleteAsync(
        Guid id,
        Guid actingMemberId,
        CancellationToken cancellationToken)
    {
        var committeeMember = await _committeeMemberRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException("Committee member not found.");

        await MemberAuthorization.EnsureGroupAdminAsync(
            _memberRepository, actingMemberId, committeeMember.GroupId, cancellationToken);

        await _committeeMemberRepository.RemoveAsync(committeeMember, cancellationToken);
        await _committeeMemberRepository.SaveChangesAsync(cancellationToken);
    }

    private async Task EnsureOfficerRoleAvailableAsync(
        Guid groupId,
        CommitteeRole role,
        Guid? excludeId,
        CancellationToken cancellationToken)
    {
        if (!IsOfficerRole(role))
        {
            return;
        }

        var exists = excludeId.HasValue
            ? await _committeeMemberRepository.ExistsForOfficerRoleExceptAsync(
                groupId, role, excludeId.Value, cancellationToken)
            : await _committeeMemberRepository.ExistsForOfficerRoleAsync(
                groupId, role, cancellationToken);

        if (exists)
        {
            throw new ConflictException($"The {role} role is already assigned.");
        }
    }

    private static bool IsOfficerRole(CommitteeRole role) => role != CommitteeRole.CommitteeMember;

    private static CommitteeMemberResponse MapToResponse(CommitteeMember committeeMember) =>
        new(
            committeeMember.Id,
            committeeMember.GroupId,
            committeeMember.MemberId,
            committeeMember.Member.User.Name,
            committeeMember.Role,
            committeeMember.CreatedAt);

    private static async Task ValidateAsync<T>(
        IValidator<T> validator,
        T instance,
        CancellationToken cancellationToken)
    {
        var result = await validator.ValidateAsync(instance, cancellationToken);
        if (!result.IsValid)
        {
            throw new ValidationException(result.Errors.Select(x => x.ErrorMessage).ToArray());
        }
    }
}
