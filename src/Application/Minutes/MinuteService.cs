using FluentValidation;
using MySociety.Application.Agenda;
using MySociety.Application.Agenda.Dtos;
using MySociety.Application.Common.Authorization;
using MySociety.Application.Common.Exceptions;
using MySociety.Application.Common.Interfaces;
using MySociety.Application.Minutes.Dtos;
using MySociety.Application.Resolutions;
using MySociety.Domain.Entities;
using MySociety.Domain.Enums;
using ValidationException = MySociety.Application.Common.Exceptions.ValidationException;

namespace MySociety.Application.Minutes;

public interface IMinuteService
{
    Task<AgendaItemResponse> UpsertAsync(
        Guid groupId,
        Guid agendaItemId,
        UpsertMinuteRequest request,
        Guid actingMemberId,
        CancellationToken cancellationToken);
}

public class MinuteService : IMinuteService
{
    private readonly IGroupRepository _groupRepository;
    private readonly IMemberRepository _memberRepository;
    private readonly ICommitteeMemberRepository _committeeMemberRepository;
    private readonly IAgendaItemRepository _agendaItemRepository;
    private readonly IMeetingRepository _meetingRepository;
    private readonly IMinuteRepository _minuteRepository;
    private readonly IResolutionRepository _resolutionRepository;
    private readonly IValidator<UpsertMinuteRequest> _validator;

    public MinuteService(
        IGroupRepository groupRepository,
        IMemberRepository memberRepository,
        ICommitteeMemberRepository committeeMemberRepository,
        IAgendaItemRepository agendaItemRepository,
        IMeetingRepository meetingRepository,
        IMinuteRepository minuteRepository,
        IResolutionRepository resolutionRepository,
        IValidator<UpsertMinuteRequest> validator)
    {
        _groupRepository = groupRepository;
        _memberRepository = memberRepository;
        _committeeMemberRepository = committeeMemberRepository;
        _agendaItemRepository = agendaItemRepository;
        _meetingRepository = meetingRepository;
        _minuteRepository = minuteRepository;
        _resolutionRepository = resolutionRepository;
        _validator = validator;
    }

    public async Task<AgendaItemResponse> UpsertAsync(
        Guid groupId,
        Guid agendaItemId,
        UpsertMinuteRequest request,
        Guid actingMemberId,
        CancellationToken cancellationToken)
    {
        await ValidateAsync(_validator, request, cancellationToken);

        _ = await _groupRepository.GetByIdAsync(groupId, cancellationToken)
            ?? throw new NotFoundException("Group not found.");

        await MemberAuthorization.EnsureCanManageMeetingsAsync(
            _memberRepository,
            _committeeMemberRepository,
            actingMemberId,
            groupId,
            cancellationToken);

        var agendaItem = await _agendaItemRepository.GetByIdAsync(agendaItemId, cancellationToken)
            ?? throw new NotFoundException("Agenda item not found.");

        var meeting = await _meetingRepository.GetByIdAsync(agendaItem.MeetingId, cancellationToken)
            ?? throw new NotFoundException("Meeting not found.");

        if (meeting.GroupId != groupId)
        {
            throw new NotFoundException("Agenda item not found.");
        }

        if (meeting.Status is MeetingStatus.Published or MeetingStatus.Archived)
        {
            throw new ConflictException("Cannot edit minutes on a published or archived meeting.");
        }

        var minute = await _minuteRepository.GetByAgendaItemIdAsync(agendaItemId, cancellationToken);
        if (minute is null)
        {
            minute = new Minute
            {
                Id = Guid.NewGuid(),
                AgendaItemId = agendaItemId,
                CreatedAt = DateTime.UtcNow,
            };
            await _minuteRepository.AddAsync(minute, cancellationToken);
        }

        if (request.DiscussionSummary is not null)
        {
            minute.DiscussionSummary = string.IsNullOrWhiteSpace(request.DiscussionSummary)
                ? null
                : request.DiscussionSummary.Trim();
            agendaItem.DiscussionSummary = minute.DiscussionSummary;
        }

        if (request.DecisionTaken is not null)
        {
            minute.DecisionTaken = string.IsNullOrWhiteSpace(request.DecisionTaken)
                ? null
                : request.DecisionTaken.Trim();
        }

        if (request.BudgetApproved.HasValue)
        {
            minute.BudgetApproved = request.BudgetApproved;
        }

        await ResolutionProvisioning.EnsureForAgendaMinuteAsync(
            _resolutionRepository,
            agendaItem,
            meeting,
            minute,
            actingMemberId,
            cancellationToken);

        await _minuteRepository.SaveChangesAsync(cancellationToken);

        var refreshed = await _agendaItemRepository.GetByIdAsync(agendaItemId, cancellationToken)
            ?? throw new NotFoundException("Agenda item not found.");

        return AgendaMapping.MapToResponse(refreshed);
    }

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
