using MySociety.Domain.Enums;

namespace MySociety.Application.Committee.Dtos;

public record CreateCommitteeMemberRequest(
    Guid GroupId,
    Guid MemberId,
    CommitteeRole Role);

public record UpdateCommitteeMemberRequest(
    CommitteeRole Role);

public record CommitteeMemberResponse(
    Guid Id,
    Guid GroupId,
    Guid MemberId,
    string MemberName,
    CommitteeRole Role,
    DateTime CreatedAt);
