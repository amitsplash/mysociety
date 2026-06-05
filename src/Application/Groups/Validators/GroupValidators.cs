using FluentValidation;



using MySociety.Application.Groups.Dtos;



using MySociety.Domain.Enums;







namespace MySociety.Application.Groups.Validators;







public class CreateGroupRequestValidator : AbstractValidator<CreateGroupRequest>



{



    public CreateGroupRequestValidator()



    {



        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);



        RuleFor(x => x.Type).IsInEnum();



        RuleFor(x => x.ContributionModel).IsInEnum();



        RuleFor(x => x.ContributionAmount).GreaterThanOrEqualTo(0);



        RuleFor(x => x.ContributionFrequency).IsInEnum();



        RuleFor(x => x.OpeningMaintenanceBalance).GreaterThanOrEqualTo(0);

        RuleFor(x => x.OpeningCorpusBalance).GreaterThanOrEqualTo(0);

        RuleFor(x => x.CreatorCorpusAmount).GreaterThanOrEqualTo(0);



    }



}







public class UpdateGroupRequestValidator : AbstractValidator<UpdateGroupRequest>



{



    public UpdateGroupRequestValidator()



    {



        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);



        RuleFor(x => x.Type).IsInEnum();



        RuleFor(x => x.ContributionModel).IsInEnum();



        RuleFor(x => x.ContributionAmount).GreaterThanOrEqualTo(0);



        RuleFor(x => x.ContributionFrequency).IsInEnum();



    }



}




