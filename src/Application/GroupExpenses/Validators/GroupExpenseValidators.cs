using FluentValidation;
using MySociety.Application.Common;
using MySociety.Application.GroupExpenses.Dtos;

namespace MySociety.Application.GroupExpenses.Validators;

public class CreateGroupExpenseRequestValidator : AbstractValidator<CreateGroupExpenseRequest>
{
    public CreateGroupExpenseRequestValidator()
    {
        RuleFor(x => x.GroupId).NotEmpty();
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.Description).NotEmpty().MaximumLength(500);
        RuleFor(x => x.FundType).IsInEnum();
        RuleFor(x => x.ExpenseDate)
            .Must(ExpenseDateRules.IsNotInFuture)
            .WithMessage("Expense date cannot be in the future.");
    }
}
