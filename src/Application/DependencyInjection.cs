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

using MySociety.Application.GroupIncomes;

using MySociety.Application.Committee;

using MySociety.Application.Agenda;
using MySociety.Application.Meetings;
using MySociety.Application.OpenMatters;
using MySociety.Application.Minutes;
using MySociety.Application.Resolutions;
using MySociety.Application.GroupDecisions;

using MySociety.Application.Notifications;
using MySociety.Application.Assets;
using MySociety.Application.MaintenanceRecords;
using MySociety.Application.MaintenanceAlerts;



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

        services.AddScoped<IGroupIncomeService, GroupIncomeService>();

        services.AddScoped<ICommitteeMemberService, CommitteeMemberService>();

        services.AddScoped<IMeetingService, MeetingService>();

        services.AddScoped<IOpenMatterService, OpenMatterService>();

        services.AddScoped<IAgendaService, AgendaService>();

        services.AddScoped<IMinuteService, MinuteService>();

        services.AddScoped<IResolutionService, ResolutionService>();

        services.AddScoped<IGroupDecisionService, GroupDecisionService>();

        services.AddScoped<ILedgerQueryService, LedgerQueryService>();

        services.AddScoped<IAuthService, AuthService>();

        services.AddScoped<INotificationService, NotificationService>();

        services.AddScoped<IAssetService, AssetService>();

        services.AddScoped<IMaintenanceRecordService, MaintenanceRecordService>();

        services.AddScoped<IMaintenanceAlertService, MaintenanceAlertService>();



        return services;

    }

}

