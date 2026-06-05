using FluentValidation;
using MySociety.Application.Members.Dtos;
using MySociety.Domain.Enums;

namespace MySociety.Application.Members.Validators;

public class CreateMemberRequestValidator : AbstractValidator<CreateMemberRequest>
{
    public CreateMemberRequestValidator()
    {
        RuleFor(x => x.GroupId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Phone).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Role).IsInEnum();
        RuleFor(x => x.SquareFeet)
            .GreaterThan(0)
            .When(x => x.SquareFeet.HasValue);
        RuleFor(x => x.CorpusAmount).GreaterThanOrEqualTo(0);
    }
}

public class UpdateMemberRequestValidator : AbstractValidator<UpdateMemberRequest>
{
    public UpdateMemberRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Phone).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Role).IsInEnum();
        RuleFor(x => x.SquareFeet)
            .GreaterThan(0)
            .When(x => x.SquareFeet.HasValue);
    }
}
