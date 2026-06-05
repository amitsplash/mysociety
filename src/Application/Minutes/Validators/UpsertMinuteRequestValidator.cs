using FluentValidation;
using MySociety.Application.Minutes.Dtos;

namespace MySociety.Application.Minutes.Validators;

public class UpsertMinuteRequestValidator : AbstractValidator<UpsertMinuteRequest>
{
    public UpsertMinuteRequestValidator()
    {
        RuleFor(x => x.DiscussionSummary).MaximumLength(4000);
        RuleFor(x => x.DecisionTaken).MaximumLength(2000);
        RuleFor(x => x.BudgetApproved).GreaterThanOrEqualTo(0).When(x => x.BudgetApproved.HasValue);
    }
}
