using MySociety.Domain.Enums;

namespace MySociety.Application.Contributions.Dtos;

public record GenerateContributionsRequest(Guid GroupId, string FromMonth, string ToMonth);

public record RecordPaymentRequest(
    Guid MemberId,
    decimal Amount,
    Guid? ContributionId);

public record ContributionResponse(
    Guid Id,
    Guid MemberId,
    Guid GroupId,
    string Period,
    decimal Amount,
    ContributionStatus Status,
    DateTime CreatedAt,
    string? MemberName = null,
    decimal PaidAmount = 0,
    decimal RemainingAmount = 0,
    string? InternalRemark = null);

public record PendingContributionItemResponse(
    Guid Id,
    string Period,
    decimal Amount,
    decimal PaidAmount,
    decimal RemainingAmount,
    string? InternalRemark = null);

public record PaymentAllocationDetail(
    Guid PaymentId,
    Guid? ContributionId,
    string Period,
    decimal AmountApplied,
    decimal RemainingAfter,
    string? InternalRemark,
    PaymentStatus Status = PaymentStatus.Approved);

public record RecordPaymentResponse(
    Guid SubmissionId,
    Guid MemberId,
    Guid GroupId,
    decimal TotalAmount,
    decimal AdvanceAmount,
    PaymentStatus Status,
    DateTime CreatedAt,
    IReadOnlyList<PaymentAllocationDetail> Allocations);

public record PendingPaymentSubmissionResponse(
    Guid SubmissionId,
    Guid MemberId,
    string MemberName,
    decimal TotalAmount,
    decimal AdvanceAmount,
    DateTime SubmittedAt,
    IReadOnlyList<PaymentAllocationDetail> Allocations);

public record PaymentSubmissionActionResponse(
    Guid SubmissionId,
    PaymentStatus Status,
    decimal TotalAmount,
    IReadOnlyList<PaymentAllocationDetail> Allocations);

public record MemberPendingContributionsResponse(
    Guid MemberId,
    string MemberName,
    decimal TotalOutstanding,
    IReadOnlyList<PendingContributionItemResponse> Items);

public record GroupPendingContributionsResponse(
    Guid GroupId,
    decimal TotalOutstanding,
    int MemberCount,
    IReadOnlyList<MemberPendingContributionsResponse> Members);

public record GenerateContributionsResponse(
    Guid GroupId,
    string Period,
    string FromMonth,
    string ToMonth,
    int MonthCount,
    int CreatedCount,
    int SkippedCount,
    IReadOnlyList<ContributionResponse> Contributions);

