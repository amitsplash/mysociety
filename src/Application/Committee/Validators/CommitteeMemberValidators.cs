using FluentValidation;
using MySociety.Application.Committee.Dtos;

namespace MySociety.Application.Committee.Validators;

public class CreateCommitteeMemberRequestValidator : AbstractValidator<CreateCommitteeMemberRequest>
{
    public CreateCommitteeMemberRequestValidator()
    {
        RuleFor(x => x.GroupId).NotEmpty();
        RuleFor(x => x.MemberId).NotEmpty();
        RuleFor(x => x.Role).IsInEnum();
    }
}

public class UpdateCommitteeMemberRequestValidator : AbstractValidator<UpdateCommitteeMemberRequest>
{
    public UpdateCommitteeMemberRequestValidator()
    {
        RuleFor(x => x.Role).IsInEnum();
    }
}
