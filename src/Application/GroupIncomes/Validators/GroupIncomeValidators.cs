using FluentValidation;
using MySociety.Application.Common;
using MySociety.Application.GroupIncomes.Dtos;

namespace MySociety.Application.GroupIncomes.Validators;

public class CreateGroupIncomeRequestValidator : AbstractValidator<CreateGroupIncomeRequest>
{
    public CreateGroupIncomeRequestValidator()
    {
        RuleFor(x => x.GroupId).NotEmpty();
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.Description).NotEmpty().MaximumLength(500);
        RuleFor(x => x.IncomeDate)
            .Must(ExpenseDateRules.IsNotInFuture)
            .WithMessage("Income date cannot be in the future.");
    }
}
