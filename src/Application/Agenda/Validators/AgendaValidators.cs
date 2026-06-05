using FluentValidation;
using MySociety.Application.Agenda.Dtos;

namespace MySociety.Application.Agenda.Validators;

public class CreateAgendaItemRequestValidator : AbstractValidator<CreateAgendaItemRequest>
{
    public CreateAgendaItemRequestValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(2000);
        RuleFor(x => x.Source).IsInEnum();
    }
}

public class UpdateAgendaItemRequestValidator : AbstractValidator<UpdateAgendaItemRequest>
{
    public UpdateAgendaItemRequestValidator()
    {
        RuleFor(x => x.Title).MaximumLength(200).When(x => x.Title is not null);
        RuleFor(x => x.Description).MaximumLength(2000);
    }
}

public class UpdateAgendaOutcomeRequestValidator : AbstractValidator<UpdateAgendaOutcomeRequest>
{
    public UpdateAgendaOutcomeRequestValidator()
    {
        RuleFor(x => x.Outcome).IsInEnum();
        RuleFor(x => x.DiscussionSummary).MaximumLength(4000);
    }
}
