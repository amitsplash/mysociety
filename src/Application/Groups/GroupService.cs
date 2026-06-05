using FluentValidation;

using MySociety.Application.Common.Authorization;

using MySociety.Application.Common.Exceptions;

using MySociety.Application.Common.Interfaces;

using MySociety.Application.Groups.Dtos;

using MySociety.Application.Members;

using MySociety.Application.Members.Dtos;

using MySociety.Domain.Entities;

using MySociety.Domain.Enums;

using ValidationException = MySociety.Application.Common.Exceptions.ValidationException;



namespace MySociety.Application.Groups;



public interface IGroupService

{

    Task<CreateGroupResponse> CreateAsync(Guid userId, CreateGroupRequest request, CancellationToken cancellationToken);

    Task<IReadOnlyList<GroupResponse>> ListMineAsync(Guid userId, CancellationToken cancellationToken);

    Task<GroupResponse> GetByIdAsync(Guid id, Guid userId, Guid actingMemberId, CancellationToken cancellationToken);

    Task<GroupResponse> UpdateAsync(Guid id, UpdateGroupRequest request, Guid userId, Guid actingMemberId, CancellationToken cancellationToken);

    Task DeleteAsync(Guid id, Guid userId, Guid actingMemberId, CancellationToken cancellationToken);

}



public class GroupService : IGroupService

{

    private readonly IGroupRepository _groupRepository;

    private readonly IMemberRepository _memberRepository;

    private readonly IUserRepository _userRepository;

    private readonly IMemberService _memberService;

    private readonly IValidator<CreateGroupRequest> _createValidator;

    private readonly IValidator<UpdateGroupRequest> _updateValidator;



    public GroupService(

        IGroupRepository groupRepository,

        IMemberRepository memberRepository,

        IUserRepository userRepository,

        IMemberService memberService,

        IValidator<CreateGroupRequest> createValidator,

        IValidator<UpdateGroupRequest> updateValidator)

    {

        _groupRepository = groupRepository;

        _memberRepository = memberRepository;

        _userRepository = userRepository;

        _memberService = memberService;

        _createValidator = createValidator;

        _updateValidator = updateValidator;

    }



    public async Task<CreateGroupResponse> CreateAsync(

        Guid userId,

        CreateGroupRequest request,

        CancellationToken cancellationToken)

    {

        await ValidateAsync(_createValidator, request, cancellationToken);



        var user = await _userRepository.GetByIdAsync(userId, cancellationToken)

            ?? throw new UnauthorizedException("User not found.");



        if (request.ContributionModel == ContributionModel.PerSquareFeet && !request.CreatorSquareFeet.HasValue)

        {

            throw new ValidationException("Square feet is required for per-square-feet contribution groups.");

        }



        var group = new Group

        {

            Id = Guid.NewGuid(),

            Name = request.Name.Trim(),

            Type = request.Type,

            ContributionModel = request.ContributionModel,

            ContributionAmount = request.ContributionAmount,

            ContributionFrequency = request.ContributionFrequency,

            OpeningMaintenanceBalance = request.OpeningMaintenanceBalance,

            OpeningCorpusBalance = request.OpeningCorpusBalance,

            CreatedByUserId = userId,

            CreatedAt = DateTime.UtcNow

        };



        await _groupRepository.AddAsync(group, cancellationToken);

        await _groupRepository.SaveChangesAsync(cancellationToken);



        var creatorMember = await _memberService.AddGroupCreatorAsAdminAsync(

            group.Id,

            user.Id,

            request.CreatorOpeningBalance,

            request.CreatorSquareFeet,

            request.CreatorCorpusAmount,

            request.CreatorCorpusPaid,

            cancellationToken);



        return new CreateGroupResponse(MapGroup(group), creatorMember);

    }



    public async Task<IReadOnlyList<GroupResponse>> ListMineAsync(

        Guid userId,

        CancellationToken cancellationToken)

    {

        var groups = await _groupRepository.GetByUserIdAsync(userId, cancellationToken);

        return groups.Select(MapGroup).ToList();

    }



    public async Task<GroupResponse> GetByIdAsync(

        Guid id,

        Guid userId,

        Guid actingMemberId,

        CancellationToken cancellationToken)

    {

        var group = await _groupRepository.GetByIdAsync(id, cancellationToken)

            ?? throw new NotFoundException("Group not found.");



        await MemberAuthorization.EnsureGroupMemberAsync(

            _memberRepository, actingMemberId, id, cancellationToken);



        return MapGroup(group);

    }



    public async Task<GroupResponse> UpdateAsync(

        Guid id,

        UpdateGroupRequest request,

        Guid userId,

        Guid actingMemberId,

        CancellationToken cancellationToken)

    {

        await ValidateAsync(_updateValidator, request, cancellationToken);



        var group = await _groupRepository.GetByIdAsync(id, cancellationToken)

            ?? throw new NotFoundException("Group not found.");



        await MemberAuthorization.EnsureGroupAdminAsync(

            _memberRepository, actingMemberId, id, cancellationToken);



        group.Name = request.Name.Trim();

        group.Type = request.Type;

        group.ContributionModel = request.ContributionModel;

        group.ContributionAmount = request.ContributionAmount;

        group.ContributionFrequency = request.ContributionFrequency;



        await _groupRepository.SaveChangesAsync(cancellationToken);

        return MapGroup(group);

    }



    public async Task DeleteAsync(

        Guid id,

        Guid userId,

        Guid actingMemberId,

        CancellationToken cancellationToken)

    {

        _ = await _groupRepository.GetByIdAsync(id, cancellationToken)

            ?? throw new NotFoundException("Group not found.");



        await MemberAuthorization.EnsureGroupAdminAsync(

            _memberRepository, actingMemberId, id, cancellationToken);



        await _groupRepository.DeleteByIdAsync(id, cancellationToken);

        await _groupRepository.SaveChangesAsync(cancellationToken);

    }



    private static GroupResponse MapGroup(Group group)

    {

        return new GroupResponse(

            group.Id,

            group.Name,

            group.Type,

            group.ContributionModel,

            group.ContributionAmount,

            group.ContributionFrequency,

            group.OpeningMaintenanceBalance,

            group.OpeningCorpusBalance,

            group.CreatedAt);

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

