using FluentValidation;
using MySociety.Application.Agenda.Dtos;
using MySociety.Application.Common.Authorization;
using MySociety.Application.Common.Exceptions;
using MySociety.Application.Common.Interfaces;
using MySociety.Application.Meetings;
using MySociety.Domain.Entities;
using MySociety.Domain.Enums;
using ValidationException = MySociety.Application.Common.Exceptions.ValidationException;

namespace MySociety.Application.Agenda;

public interface IAgendaService
{
    Task<AgendaItemResponse> AddAsync(
        Guid groupId,
        Guid meetingId,
        CreateAgendaItemRequest request,
        Guid actingMemberId,
        CancellationToken cancellationToken);

    Task<AgendaItemResponse> AddFromOpenMatterAsync(
        Guid groupId,
        Guid meetingId,
        Guid openMatterId,
        Guid actingMemberId,
        CancellationToken cancellationToken);

    Task<AgendaItemResponse> UpdateAsync(
        Guid groupId,
        Guid agendaItemId,
        UpdateAgendaItemRequest request,
        Guid actingMemberId,
        CancellationToken cancellationToken);

    Task<AgendaItemResponse> UpdateOutcomeAsync(
        Guid groupId,
        Guid agendaItemId,
        UpdateAgendaOutcomeRequest request,
        Guid actingMemberId,
        CancellationToken cancellationToken);

    Task DeleteAsync(Guid groupId, Guid agendaItemId, Guid actingMemberId, CancellationToken cancellationToken);
}

public class AgendaService : IAgendaService
{
    private readonly IGroupRepository _groupRepository;
    private readonly IMemberRepository _memberRepository;
    private readonly ICommitteeMemberRepository _committeeMemberRepository;
    private readonly IMeetingRepository _meetingRepository;
    private readonly IOpenMatterRepository _openMatterRepository;
    private readonly IAgendaItemRepository _agendaItemRepository;
    private readonly IValidator<CreateAgendaItemRequest> _createValidator;
    private readonly IValidator<UpdateAgendaItemRequest> _updateValidator;
    private readonly IValidator<UpdateAgendaOutcomeRequest> _outcomeValidator;

    public AgendaService(
        IGroupRepository groupRepository,
        IMemberRepository memberRepository,
        ICommitteeMemberRepository committeeMemberRepository,
        IMeetingRepository meetingRepository,
        IOpenMatterRepository openMatterRepository,
        IAgendaItemRepository agendaItemRepository,
        IValidator<CreateAgendaItemRequest> createValidator,
        IValidator<UpdateAgendaItemRequest> updateValidator,
        IValidator<UpdateAgendaOutcomeRequest> outcomeValidator)
    {
        _groupRepository = groupRepository;
        _memberRepository = memberRepository;
        _committeeMemberRepository = committeeMemberRepository;
        _meetingRepository = meetingRepository;
        _openMatterRepository = openMatterRepository;
        _agendaItemRepository = agendaItemRepository;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _outcomeValidator = outcomeValidator;
    }

    public async Task<AgendaItemResponse> AddAsync(
        Guid groupId,
        Guid meetingId,
        CreateAgendaItemRequest request,
        Guid actingMemberId,
        CancellationToken cancellationToken)
    {
        await ValidateAsync(_createValidator, request, cancellationToken);
        var meeting = await GetEditableMeetingAsync(groupId, meetingId, actingMemberId, cancellationToken);

        if (request.OpenMatterId.HasValue)
        {
            await ValidateOpenMatterAsync(groupId, request.OpenMatterId.Value, cancellationToken);
        }

        var items = await _agendaItemRepository.GetByMeetingIdAsync(meetingId, cancellationToken);
        var nextOrder = items.Count > 0 ? items.Max(x => x.DisplayOrder) + 1 : 0;
        var nextNumber = items.Count > 0 ? items.Max(x => x.AgendaNumber) + 1 : 1;

        var item = new AgendaItem
        {
            Id = Guid.NewGuid(),
            MeetingId = meetingId,
            OpenMatterId = request.OpenMatterId,
            AgendaNumber = nextNumber,
            Title = request.Title.Trim(),
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
            DisplayOrder = nextOrder,
            Source = request.OpenMatterId.HasValue ? AgendaItemSource.FromBacklog : request.Source,
            Outcome = MeetingItemOutcome.NotDiscussed,
            CreatedAt = DateTime.UtcNow,
        };

        await _agendaItemRepository.AddAsync(item, cancellationToken);

        if (item.Source == AgendaItemSource.AdHoc && !item.OpenMatterId.HasValue)
        {
            await LinkAgendaToOpenMatterAsync(item, groupId, meetingId, actingMemberId, cancellationToken);
        }

        await _agendaItemRepository.SaveChangesAsync(cancellationToken);

        return await MapItemAsync(item.Id, cancellationToken);
    }

    public async Task<AgendaItemResponse> AddFromOpenMatterAsync(
        Guid groupId,
        Guid meetingId,
        Guid openMatterId,
        Guid actingMemberId,
        CancellationToken cancellationToken)
    {
        await GetEditableMeetingAsync(groupId, meetingId, actingMemberId, cancellationToken);
        var matter = await _openMatterRepository.GetByIdAsync(openMatterId, cancellationToken)
            ?? throw new NotFoundException("Open matter not found.");

        if (matter.GroupId != groupId || matter.Status != OpenMatterStatus.Open)
        {
            throw new ValidationException("Open matter must belong to the group and be open.");
        }

        return await AddAsync(
            groupId,
            meetingId,
            new CreateAgendaItemRequest(matter.Title, matter.Description, openMatterId, AgendaItemSource.FromBacklog),
            actingMemberId,
            cancellationToken);
    }

    public async Task<AgendaItemResponse> UpdateAsync(
        Guid groupId,
        Guid agendaItemId,
        UpdateAgendaItemRequest request,
        Guid actingMemberId,
        CancellationToken cancellationToken)
    {
        await ValidateAsync(_updateValidator, request, cancellationToken);
        var item = await GetEditableAgendaItemAsync(groupId, agendaItemId, actingMemberId, cancellationToken);

        if (request.Title is not null)
        {
            item.Title = request.Title.Trim();
        }

        if (request.Description is not null)
        {
            item.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
        }

        if (request.DisplayOrder.HasValue)
        {
            item.DisplayOrder = request.DisplayOrder.Value;
        }

        await _agendaItemRepository.SaveChangesAsync(cancellationToken);
        return await MapItemAsync(item.Id, cancellationToken);
    }

    public async Task<AgendaItemResponse> UpdateOutcomeAsync(
        Guid groupId,
        Guid agendaItemId,
        UpdateAgendaOutcomeRequest request,
        Guid actingMemberId,
        CancellationToken cancellationToken)
    {
        await ValidateAsync(_outcomeValidator, request, cancellationToken);
        var item = await GetEditableAgendaItemAsync(groupId, agendaItemId, actingMemberId, cancellationToken);

        item.Outcome = request.Outcome;
        if (request.DiscussionSummary is not null)
        {
            item.DiscussionSummary = string.IsNullOrWhiteSpace(request.DiscussionSummary)
                ? null
                : request.DiscussionSummary.Trim();
        }

        if (!item.OpenMatterId.HasValue && item.Source == AgendaItemSource.AdHoc)
        {
            await LinkAgendaToOpenMatterAsync(item, groupId, item.MeetingId, actingMemberId, cancellationToken);
        }

        await ApplyOpenMatterOutcomeAsync(item, cancellationToken);
        await _agendaItemRepository.SaveChangesAsync(cancellationToken);
        return await MapItemAsync(item.Id, cancellationToken);
    }

    public async Task DeleteAsync(
        Guid groupId,
        Guid agendaItemId,
        Guid actingMemberId,
        CancellationToken cancellationToken)
    {
        var item = await GetEditableAgendaItemAsync(groupId, agendaItemId, actingMemberId, cancellationToken);
        await _agendaItemRepository.RemoveAsync(item, cancellationToken);
        await _agendaItemRepository.SaveChangesAsync(cancellationToken);
    }

    private async Task ApplyOpenMatterOutcomeAsync(AgendaItem item, CancellationToken cancellationToken)
    {
        if (!item.OpenMatterId.HasValue)
        {
            return;
        }

        var matter = await _openMatterRepository.GetByIdAsync(item.OpenMatterId.Value, cancellationToken);
        if (matter is null)
        {
            return;
        }

        matter.LastDiscussedInMeetingId = item.MeetingId;

        matter.Status = item.Outcome switch
        {
            MeetingItemOutcome.Finalized => OpenMatterStatus.Finalized,
            MeetingItemOutcome.Postponed or MeetingItemOutcome.NeedsMoreDiscussion or MeetingItemOutcome.Discussed
                => OpenMatterStatus.Open,
            _ => matter.Status
        };

        await _openMatterRepository.SaveChangesAsync(cancellationToken);
    }

    private async Task<Meeting> GetEditableMeetingAsync(
        Guid groupId,
        Guid meetingId,
        Guid actingMemberId,
        CancellationToken cancellationToken)
    {
        await EnsureCanManageAsync(groupId, actingMemberId, cancellationToken);

        var meeting = await _meetingRepository.GetByIdAsync(meetingId, cancellationToken)
            ?? throw new NotFoundException("Meeting not found.");

        if (meeting.GroupId != groupId)
        {
            throw new NotFoundException("Meeting not found.");
        }

        if (meeting.Status is MeetingStatus.Published or MeetingStatus.Archived)
        {
            throw new ConflictException("Cannot edit agenda on a published or archived meeting.");
        }

        return meeting;
    }

    private async Task<AgendaItem> GetEditableAgendaItemAsync(
        Guid groupId,
        Guid agendaItemId,
        Guid actingMemberId,
        CancellationToken cancellationToken)
    {
        var item = await _agendaItemRepository.GetByIdAsync(agendaItemId, cancellationToken)
            ?? throw new NotFoundException("Agenda item not found.");

        if (item.Meeting.GroupId != groupId)
        {
            throw new NotFoundException("Agenda item not found.");
        }

        await GetEditableMeetingAsync(groupId, item.MeetingId, actingMemberId, cancellationToken);
        return item;
    }

    private async Task LinkAgendaToOpenMatterAsync(
        AgendaItem item,
        Guid groupId,
        Guid meetingId,
        Guid actingMemberId,
        CancellationToken cancellationToken)
    {
        if (item.OpenMatterId.HasValue)
        {
            return;
        }

        var matter = new OpenMatter
        {
            Id = Guid.NewGuid(),
            GroupId = groupId,
            Title = item.Title,
            Description = item.Description,
            Status = OpenMatterStatus.Open,
            RaisedAt = DateTime.UtcNow,
            LastDiscussedInMeetingId = meetingId,
            CreatedByMemberId = actingMemberId,
            CreatedAt = DateTime.UtcNow,
        };

        await _openMatterRepository.AddAsync(matter, cancellationToken);
        item.OpenMatterId = matter.Id;
        item.Source = AgendaItemSource.FromBacklog;
    }

    private async Task ValidateOpenMatterAsync(Guid groupId, Guid openMatterId, CancellationToken cancellationToken)
    {
        var matter = await _openMatterRepository.GetByIdAsync(openMatterId, cancellationToken)
            ?? throw new NotFoundException("Open matter not found.");

        if (matter.GroupId != groupId || matter.Status != OpenMatterStatus.Open)
        {
            throw new ValidationException("Open matter must belong to the group and be open.");
        }
    }

    private async Task EnsureCanManageAsync(Guid groupId, Guid actingMemberId, CancellationToken cancellationToken)
    {
        _ = await _groupRepository.GetByIdAsync(groupId, cancellationToken)
            ?? throw new NotFoundException("Group not found.");

        await MemberAuthorization.EnsureCanManageMeetingsAsync(
            _memberRepository, _committeeMemberRepository, actingMemberId, groupId, cancellationToken);
    }

    private async Task<AgendaItemResponse> MapItemAsync(Guid id, CancellationToken cancellationToken)
    {
        var item = await _agendaItemRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException("Agenda item not found.");

        return AgendaMapping.MapToResponse(item);
    }

    private static async Task ValidateAsync<T>(IValidator<T> validator, T instance, CancellationToken cancellationToken)
    {
        var result = await validator.ValidateAsync(instance, cancellationToken);
        if (!result.IsValid)
        {
            throw new ValidationException(result.Errors.Select(x => x.ErrorMessage).ToArray());
        }
    }
}
