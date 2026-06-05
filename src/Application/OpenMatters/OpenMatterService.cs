using FluentValidation;
using MySociety.Application.Common.Authorization;
using MySociety.Application.Common.Exceptions;
using MySociety.Application.Common.Interfaces;
using MySociety.Application.OpenMatters.Dtos;
using MySociety.Domain.Entities;
using MySociety.Domain.Enums;
using ValidationException = MySociety.Application.Common.Exceptions.ValidationException;

namespace MySociety.Application.OpenMatters;

public interface IOpenMatterService
{
    Task<OpenMatterSummaryResponse> GetSummaryAsync(Guid groupId, Guid actingMemberId, CancellationToken cancellationToken);
    Task<IReadOnlyList<OpenMatterResponse>> GetByGroupIdAsync(
        Guid groupId,
        Guid actingMemberId,
        OpenMatterStatus? statusFilter,
        CancellationToken cancellationToken);
    Task<OpenMatterResponse> GetByIdAsync(Guid groupId, Guid id, Guid actingMemberId, CancellationToken cancellationToken);
    Task<OpenMatterResponse> CreateAsync(CreateOpenMatterRequest request, Guid actingMemberId, CancellationToken cancellationToken);
    Task<OpenMatterResponse> UpdateAsync(Guid id, UpdateOpenMatterRequest request, Guid actingMemberId, CancellationToken cancellationToken);
    Task<OpenMatterResponse> PromoteFromAgendaAsync(Guid groupId, Guid agendaItemId, Guid actingMemberId, CancellationToken cancellationToken);
}

public class OpenMatterService : IOpenMatterService
{
    private readonly IGroupRepository _groupRepository;
    private readonly IMemberRepository _memberRepository;
    private readonly ICommitteeMemberRepository _committeeMemberRepository;
    private readonly IOpenMatterRepository _openMatterRepository;
    private readonly IAgendaItemRepository _agendaItemRepository;
    private readonly IValidator<CreateOpenMatterRequest> _createValidator;
    private readonly IValidator<UpdateOpenMatterRequest> _updateValidator;

    public OpenMatterService(
        IGroupRepository groupRepository,
        IMemberRepository memberRepository,
        ICommitteeMemberRepository committeeMemberRepository,
        IOpenMatterRepository openMatterRepository,
        IAgendaItemRepository agendaItemRepository,
        IValidator<CreateOpenMatterRequest> createValidator,
        IValidator<UpdateOpenMatterRequest> updateValidator)
    {
        _groupRepository = groupRepository;
        _memberRepository = memberRepository;
        _committeeMemberRepository = committeeMemberRepository;
        _openMatterRepository = openMatterRepository;
        _agendaItemRepository = agendaItemRepository;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    public async Task<OpenMatterSummaryResponse> GetSummaryAsync(
        Guid groupId,
        Guid actingMemberId,
        CancellationToken cancellationToken)
    {
        await EnsureGroupMemberAsync(groupId, actingMemberId, cancellationToken);
        var all = await _openMatterRepository.GetByGroupIdAsync(groupId, null, cancellationToken);
        return new OpenMatterSummaryResponse(
            all.Count(x => x.Status == OpenMatterStatus.Open),
            all.Count(x => x.Status == OpenMatterStatus.Finalized),
            all.Count(x => x.Status == OpenMatterStatus.Cancelled));
    }

    public async Task<IReadOnlyList<OpenMatterResponse>> GetByGroupIdAsync(
        Guid groupId,
        Guid actingMemberId,
        OpenMatterStatus? statusFilter,
        CancellationToken cancellationToken)
    {
        await EnsureGroupMemberAsync(groupId, actingMemberId, cancellationToken);
        var matters = await _openMatterRepository.GetByGroupIdAsync(groupId, statusFilter, cancellationToken);
        return matters.Select(MapToResponse).ToList();
    }

    public async Task<OpenMatterResponse> GetByIdAsync(
        Guid groupId,
        Guid id,
        Guid actingMemberId,
        CancellationToken cancellationToken)
    {
        await EnsureGroupMemberAsync(groupId, actingMemberId, cancellationToken);
        var matter = await GetMatterInGroupAsync(groupId, id, cancellationToken);
        return MapToResponse(matter);
    }

    public async Task<OpenMatterResponse> CreateAsync(
        CreateOpenMatterRequest request,
        Guid actingMemberId,
        CancellationToken cancellationToken)
    {
        await ValidateAsync(_createValidator, request, cancellationToken);
        await EnsureCanManageAsync(request.GroupId, actingMemberId, cancellationToken);

        var matter = new OpenMatter
        {
            Id = Guid.NewGuid(),
            GroupId = request.GroupId,
            Title = request.Title.Trim(),
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
            Status = OpenMatterStatus.Open,
            RaisedAt = DateTime.UtcNow,
            CreatedByMemberId = actingMemberId,
            CreatedAt = DateTime.UtcNow,
        };

        await _openMatterRepository.AddAsync(matter, cancellationToken);
        await _openMatterRepository.SaveChangesAsync(cancellationToken);

        matter = await _openMatterRepository.GetByIdAsync(matter.Id, cancellationToken) ?? matter;
        return MapToResponse(matter);
    }

    public async Task<OpenMatterResponse> UpdateAsync(
        Guid id,
        UpdateOpenMatterRequest request,
        Guid actingMemberId,
        CancellationToken cancellationToken)
    {
        await ValidateAsync(_updateValidator, request, cancellationToken);
        var matter = await _openMatterRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException("Open matter not found.");

        await EnsureCanManageAsync(matter.GroupId, actingMemberId, cancellationToken);

        if (request.Title is not null)
        {
            matter.Title = request.Title.Trim();
        }

        if (request.Description is not null)
        {
            matter.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
        }

        if (request.Status.HasValue)
        {
            matter.Status = request.Status.Value;
        }

        await _openMatterRepository.SaveChangesAsync(cancellationToken);
        return MapToResponse(matter);
    }

    public async Task<OpenMatterResponse> PromoteFromAgendaAsync(
        Guid groupId,
        Guid agendaItemId,
        Guid actingMemberId,
        CancellationToken cancellationToken)
    {
        await EnsureCanManageAsync(groupId, actingMemberId, cancellationToken);

        var agendaItem = await _agendaItemRepository.GetByIdAsync(agendaItemId, cancellationToken)
            ?? throw new NotFoundException("Agenda item not found.");

        if (agendaItem.Meeting.GroupId != groupId)
        {
            throw new NotFoundException("Agenda item not found.");
        }

        if (agendaItem.OpenMatterId.HasValue)
        {
            throw new ConflictException("Agenda item is already linked to an open matter.");
        }

        var matter = new OpenMatter
        {
            Id = Guid.NewGuid(),
            GroupId = groupId,
            Title = agendaItem.Title,
            Description = agendaItem.Description,
            Status = OpenMatterStatus.Open,
            RaisedAt = DateTime.UtcNow,
            LastDiscussedInMeetingId = agendaItem.MeetingId,
            CreatedByMemberId = actingMemberId,
            CreatedAt = DateTime.UtcNow,
        };

        await _openMatterRepository.AddAsync(matter, cancellationToken);
        agendaItem.OpenMatterId = matter.Id;
        agendaItem.Source = AgendaItemSource.FromBacklog;
        await _agendaItemRepository.SaveChangesAsync(cancellationToken);

        matter = await _openMatterRepository.GetByIdAsync(matter.Id, cancellationToken) ?? matter;
        return MapToResponse(matter);
    }

    private async Task<OpenMatter> GetMatterInGroupAsync(Guid groupId, Guid id, CancellationToken cancellationToken)
    {
        var matter = await _openMatterRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException("Open matter not found.");

        if (matter.GroupId != groupId)
        {
            throw new NotFoundException("Open matter not found.");
        }

        return matter;
    }

    private async Task EnsureGroupMemberAsync(Guid groupId, Guid actingMemberId, CancellationToken cancellationToken)
    {
        _ = await _groupRepository.GetByIdAsync(groupId, cancellationToken)
            ?? throw new NotFoundException("Group not found.");

        await MemberAuthorization.EnsureGroupMemberAsync(_memberRepository, actingMemberId, groupId, cancellationToken);
    }

    private async Task EnsureCanManageAsync(Guid groupId, Guid actingMemberId, CancellationToken cancellationToken)
    {
        _ = await _groupRepository.GetByIdAsync(groupId, cancellationToken)
            ?? throw new NotFoundException("Group not found.");

        await MemberAuthorization.EnsureCanManageMeetingsAsync(
            _memberRepository, _committeeMemberRepository, actingMemberId, groupId, cancellationToken);
    }

    private static OpenMatterResponse MapToResponse(OpenMatter matter) =>
        new(
            matter.Id,
            matter.GroupId,
            matter.Title,
            matter.Description,
            matter.Status,
            matter.RaisedAt,
            matter.LastDiscussedInMeetingId,
            matter.CreatedByMember?.User?.Name ?? string.Empty,
            matter.CreatedAt);

    private static async Task ValidateAsync<T>(IValidator<T> validator, T instance, CancellationToken cancellationToken)
    {
        var result = await validator.ValidateAsync(instance, cancellationToken);
        if (!result.IsValid)
        {
            throw new ValidationException(result.Errors.Select(x => x.ErrorMessage).ToArray());
        }
    }
}
