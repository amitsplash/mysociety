using System.Security.Claims;
using MySociety.Application.Common.Exceptions;
using MySociety.Application.Common.Interfaces;

namespace MySociety.Api.Extensions;

public static class HttpContextExtensions
{
    public const string ActingMemberIdHeader = "X-Member-Id";

    public static Guid GetRequiredUserId(this HttpContext context)
    {
        var userIdClaim = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            throw new UnauthorizedException("Authentication required.");
        }

        return userId;
    }

    public static async Task<Guid> GetRequiredActingMemberIdAsync(
        this HttpContext context,
        IMemberRepository memberRepository,
        CancellationToken cancellationToken)
    {
        var userIdClaim = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            throw new UnauthorizedException("Authentication required.");
        }

        if (!context.Request.Headers.TryGetValue(ActingMemberIdHeader, out var headerValue) ||
            !Guid.TryParse(headerValue, out var memberId))
        {
            throw new UnauthorizedException("X-Member-Id header is required.");
        }

        var member = await memberRepository.GetByIdAsync(memberId, cancellationToken)
            ?? throw new UnauthorizedException("Invalid acting member.");

        if (member.UserId != userId)
        {
            throw new ForbiddenException("Member does not belong to the authenticated user.");
        }

        return memberId;
    }

    public static async Task<Guid?> TryGetActingMemberIdAsync(
        this HttpContext context,
        IMemberRepository memberRepository,
        CancellationToken cancellationToken)
    {
        var userId = context.GetRequiredUserId();

        if (!context.Request.Headers.TryGetValue(ActingMemberIdHeader, out var headerValue) ||
            !Guid.TryParse(headerValue, out var memberId))
        {
            return null;
        }

        var member = await memberRepository.GetByIdAsync(memberId, cancellationToken)
            ?? throw new UnauthorizedException("Invalid acting member.");

        if (member.UserId != userId)
        {
            throw new ForbiddenException("Member does not belong to the authenticated user.");
        }

        return memberId;
    }
}
