using FluentValidation;
using MySociety.Application.Assets.Dtos;
using MySociety.Domain.Enums;

namespace MySociety.Application.Assets.Validators;

public class CreateAssetRequestValidator : AbstractValidator<CreateAssetRequest>
{
    public CreateAssetRequestValidator()
    {
        RuleFor(x => x.GroupId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Location).MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(1000);
        RuleFor(x => x.SerialNumber).MaximumLength(100);
        RuleFor(x => x.VendorName).MaximumLength(200);
        RuleFor(x => x.MaintenanceIntervalDays).GreaterThan(0);
        RuleFor(x => x.AlertLeadDays).GreaterThanOrEqualTo(0).LessThanOrEqualTo(90);
        RuleFor(x => x.Status).IsInEnum().NotEqual(AssetStatus.Decommissioned);
    }
}

public class UpdateAssetRequestValidator : AbstractValidator<UpdateAssetRequest>
{
    public UpdateAssetRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Location).MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(1000);
        RuleFor(x => x.SerialNumber).MaximumLength(100);
        RuleFor(x => x.VendorName).MaximumLength(200);
        RuleFor(x => x.MaintenanceIntervalDays).GreaterThan(0);
        RuleFor(x => x.AlertLeadDays).GreaterThanOrEqualTo(0).LessThanOrEqualTo(90);
        RuleFor(x => x.Status).IsInEnum();
    }
}
