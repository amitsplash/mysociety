using FluentValidation;

using Microsoft.Extensions.DependencyInjection;

using MySociety.Application.Auth;

using MySociety.Application.Auth.Validators;

using MySociety.Application.Contributions;

using MySociety.Application.Expenses;

using MySociety.Application.Groups;

using MySociety.Application.Groups.Validators;

using MySociety.Application.Ledgers;

using MySociety.Application.Members;

using MySociety.Application.Members.Validators;

using MySociety.Application.GroupExpenses;

using MySociety.Application.Committee;

using MySociety.Application.Agenda;
using MySociety.Application.Meetings;
using MySociety.Application.OpenMatters;
using MySociety.Application.Minutes;
using MySociety.Application.Resolutions;
using MySociety.Application.GroupDecisions;



namespace MySociety.Application;



public static class DependencyInjection

{

    public static IServiceCollection AddApplication(this IServiceCollection services)

    {

        services.AddValidatorsFromAssemblyContaining<CreateGroupRequestValidator>();

        services.AddValidatorsFromAssemblyContaining<LoginRequestValidator>();



        services.AddScoped<IGroupService, GroupService>();

        services.AddScoped<IMemberService, MemberService>();

        services.AddScoped<IContributionService, ContributionService>();

        services.AddScoped<IExpenseService, ExpenseService>();

        services.AddScoped<IGroupExpenseService, GroupExpenseService>();

        services.AddScoped<ICommitteeMemberService, CommitteeMemberService>();

        services.AddScoped<IMeetingService, MeetingService>();

        services.AddScoped<IOpenMatterService, OpenMatterService>();

        services.AddScoped<IAgendaService, AgendaService>();

        services.AddScoped<IMinuteService, MinuteService>();

        services.AddScoped<IResolutionService, ResolutionService>();

        services.AddScoped<IGroupDecisionService, GroupDecisionService>();

        services.AddScoped<ILedgerQueryService, LedgerQueryService>();

        services.AddScoped<IAuthService, AuthService>();



        return services;

    }

}

