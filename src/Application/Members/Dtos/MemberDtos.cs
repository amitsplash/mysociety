using MySociety.Domain.Enums;



namespace MySociety.Application.Members.Dtos;



public record CreateMemberRequest(

    Guid GroupId,

    string Name,

    string Phone,

    MemberRole Role,

    decimal OpeningBalance,

    decimal? SquareFeet,

    decimal CorpusAmount = 0,

    bool CorpusPaid = false);



public record UpdateMemberRequest(

    string Name,

    string Phone,

    MemberRole Role,

    decimal? SquareFeet);



public record MemberResponse(

    Guid Id,

    Guid GroupId,

    string Name,

    string? Phone,

    MemberRole Role,

    decimal? SquareFeet,

    decimal CorpusAmount,

    DateTime? CorpusPaidAt,

    DateTime CreatedAt);



public record MarkCorpusReceivedResponse(

    MemberResponse Member,

    decimal CorpusAmountAdded,

    decimal CorpusFundBalance);



public record CreateMemberResponse(

    MemberResponse Member,

    bool RequiresActivation,

    string? InviteCode,

    DateTime? InviteExpiresAt);



public record IssuePasswordResetResponse(

    string? Phone,

    string Username,

    string Name,

    string ResetCode,

    DateTime ExpiresAt);

