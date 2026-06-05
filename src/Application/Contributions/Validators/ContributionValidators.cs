using FluentValidation;
using MySociety.Application.Contributions.Dtos;

namespace MySociety.Application.Contributions.Validators;

public class GenerateContributionsRequestValidator : AbstractValidator<GenerateContributionsRequest>
{
    public GenerateContributionsRequestValidator()
    {
        RuleFor(x => x.GroupId).NotEmpty();
        RuleFor(x => x.FromMonth).NotEmpty().MaximumLength(7);
        RuleFor(x => x.ToMonth).NotEmpty().MaximumLength(7);
    }
}

public class RecordPaymentRequestValidator : AbstractValidator<RecordPaymentRequest>
{
    public RecordPaymentRequestValidator()
    {
        RuleFor(x => x.MemberId).NotEmpty();
        RuleFor(x => x.Amount).GreaterThan(0);
    }
}
