using FluentValidation;
using MySociety.Application.Common;
using MySociety.Application.MaintenanceRecords.Dtos;

namespace MySociety.Application.MaintenanceRecords.Validators;

public class CreateMaintenanceRecordRequestValidator : AbstractValidator<CreateMaintenanceRecordRequest>
{
    public CreateMaintenanceRecordRequestValidator()
    {
        RuleFor(x => x.AssetId).NotEmpty();
        RuleFor(x => x.GroupId).NotEmpty();
        RuleFor(x => x.Description).NotEmpty().MaximumLength(500);
        RuleFor(x => x.VendorName).MaximumLength(200);
        RuleFor(x => x.Notes).MaximumLength(1000);
        RuleFor(x => x.Cost).GreaterThanOrEqualTo(0).When(x => x.Cost.HasValue);
        RuleFor(x => x.PerformedDate)
            .Must(ExpenseDateRules.IsNotInFuture)
            .WithMessage("Performed date cannot be in the future.");
    }
}
