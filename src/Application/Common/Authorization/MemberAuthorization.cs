using MySociety.Application.Common.Exceptions;
using MySociety.Application.Common.Interfaces;
using MySociety.Domain.Enums;

namespace MySociety.Application.Common.Authorization;

public static class MemberAuthorization
{
    public static async Task EnsureGroupMemberAsync(
        IMemberRepository memberRepository,
        Guid actingMemberId,
        Guid groupId,
        CancellationToken cancellationToken)
    {
        var actingMember = await memberRepository.GetByIdAsync(actingMemberId, cancellationToken)
            ?? throw new UnauthorizedException("Invalid acting member.");

        if (actingMember.GroupId != groupId)
        {
            throw new ForbiddenException("You are not a member of this group.");
        }
    }

    public static async Task EnsureGroupAdminAsync(
        IMemberRepository memberRepository,
        Guid actingMemberId,
        Guid groupId,
        CancellationToken cancellationToken)
    {
        var actingMember = await memberRepository.GetByIdAsync(actingMemberId, cancellationToken)
            ?? throw new UnauthorizedException("Invalid acting member.");

        if (actingMember.GroupId != groupId)
        {
            throw new ForbiddenException("You are not a member of this group.");
        }

        if (actingMember.Role != MemberRole.Admin)
        {
            throw new ForbiddenException("Admin privileges are required.");
        }
    }

    public static async Task EnsureCanManageMeetingsAsync(
        IMemberRepository memberRepository,
        ICommitteeMemberRepository committeeMemberRepository,
        Guid actingMemberId,
        Guid groupId,
        CancellationToken cancellationToken)
    {
        var actingMember = await memberRepository.GetByIdAsync(actingMemberId, cancellationToken)
            ?? throw new UnauthorizedException("Invalid acting member.");

        if (actingMember.GroupId != groupId)
        {
            throw new ForbiddenException("You are not a member of this group.");
        }

        if (actingMember.Role == MemberRole.Admin)
        {
            return;
        }

        var isCommitteeMember = await committeeMemberRepository.IsCommitteeMemberAsync(
            groupId,
            actingMemberId,
            cancellationToken);

        if (!isCommitteeMember)
        {
            throw new ForbiddenException("Committee member or admin privileges are required.");
        }
    }
}