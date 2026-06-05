using FluentValidation;
using MySociety.Application.Resolutions.Dtos;

namespace MySociety.Application.Resolutions.Validators;

public class CreateResolutionRequestValidator : AbstractValidator<CreateResolutionRequest>
{
    public CreateResolutionRequestValidator()
    {
        RuleFor(x => x.GroupId).NotEmpty();
        RuleFor(x => x.MeetingId).NotEmpty();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(4000);
        RuleFor(x => x.ApprovedBudget).GreaterThanOrEqualTo(0).When(x => x.ApprovedBudget.HasValue);
    }
}

public class UpdateResolutionRequestValidator : AbstractValidator<UpdateResolutionRequest>
{
    public UpdateResolutionRequestValidator()
    {
        RuleFor(x => x.Title).MaximumLength(200).When(x => x.Title is not null);
        RuleFor(x => x.Description).MaximumLength(4000).When(x => x.Description is not null);
        RuleFor(x => x.ApprovedBudget).GreaterThanOrEqualTo(0).When(x => x.ApprovedBudget.HasValue);
    }
}
