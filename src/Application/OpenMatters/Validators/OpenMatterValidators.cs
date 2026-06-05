using FluentValidation;
using MySociety.Application.OpenMatters.Dtos;

namespace MySociety.Application.OpenMatters.Validators;

public class CreateOpenMatterRequestValidator : AbstractValidator<CreateOpenMatterRequest>
{
    public CreateOpenMatterRequestValidator()
    {
        RuleFor(x => x.GroupId).NotEmpty();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(2000);
    }
}

public class UpdateOpenMatterRequestValidator : AbstractValidator<UpdateOpenMatterRequest>
{
    public UpdateOpenMatterRequestValidator()
    {
        RuleFor(x => x.Title).MaximumLength(200).When(x => x.Title is not null);
        RuleFor(x => x.Description).MaximumLength(2000);
        RuleFor(x => x.Status).IsInEnum().When(x => x.Status.HasValue);
    }
}
