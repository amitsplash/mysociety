using FluentValidation;
using MySociety.Application.Common;
using MySociety.Application.Expenses.Dtos;

namespace MySociety.Application.Expenses.Validators;

public class CreateExpenseRequestValidator : AbstractValidator<CreateExpenseRequest>
{
    public CreateExpenseRequestValidator()
    {
        RuleFor(x => x.GroupId).NotEmpty();
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.Description).NotEmpty().MaximumLength(500);
        RuleFor(x => x.ExpenseDate)
            .Must(ExpenseDateRules.IsNotInFuture)
            .WithMessage("Expense date cannot be in the future.");
    }
}
