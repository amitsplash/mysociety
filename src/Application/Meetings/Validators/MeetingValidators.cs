using FluentValidation;
using MySociety.Application.Meetings.Dtos;

namespace MySociety.Application.Meetings.Validators;

public class CreateMeetingRequestValidator : AbstractValidator<CreateMeetingRequest>
{
    public CreateMeetingRequestValidator()
    {
        RuleFor(x => x.GroupId).NotEmpty();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.MeetingDate).NotEmpty();
        RuleFor(x => x.Location).MaximumLength(200);
        RuleFor(x => x.Summary).MaximumLength(2000);
        RuleFor(x => x.MeetingType).IsInEnum();
        RuleFor(x => x.Status).IsInEnum();
    }
}

public class UpdateMeetingRequestValidator : AbstractValidator<UpdateMeetingRequest>
{
    public UpdateMeetingRequestValidator()
    {
        RuleFor(x => x.Title).MaximumLength(200).When(x => x.Title is not null);
        RuleFor(x => x.Location).MaximumLength(200);
        RuleFor(x => x.Summary).MaximumLength(2000);
        RuleFor(x => x.MeetingType).IsInEnum().When(x => x.MeetingType.HasValue);
    }
}

public class UpdateMeetingStatusRequestValidator : AbstractValidator<UpdateMeetingStatusRequest>
{
    public UpdateMeetingStatusRequestValidator()
    {
        RuleFor(x => x.Status).IsInEnum();
    }
}
