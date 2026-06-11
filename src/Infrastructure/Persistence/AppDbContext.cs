using Microsoft.EntityFrameworkCore;
using MySociety.Domain.Entities;

namespace MySociety.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Group> Groups => Set<Group>();
    public DbSet<Member> Members => Set<Member>();
    public DbSet<Contribution> Contributions => Set<Contribution>();
    public DbSet<Expense> Expenses => Set<Expense>();
    public DbSet<GroupExpense> GroupExpenses => Set<GroupExpense>();
    public DbSet<GroupIncome> GroupIncomes => Set<GroupIncome>();
    public DbSet<LedgerEntry> LedgerEntries => Set<LedgerEntry>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<MemberInvite> MemberInvites => Set<MemberInvite>();
    public DbSet<PhoneOtpVerification> PhoneOtpVerifications => Set<PhoneOtpVerification>();
    public DbSet<PasswordResetToken> PasswordResetTokens => Set<PasswordResetToken>();
    public DbSet<CommitteeMember> CommitteeMembers => Set<CommitteeMember>();
    public DbSet<Meeting> Meetings => Set<Meeting>();
    public DbSet<OpenMatter> OpenMatters => Set<OpenMatter>();
    public DbSet<AgendaItem> AgendaItems => Set<AgendaItem>();
    public DbSet<MeetingAttendee> MeetingAttendees => Set<MeetingAttendee>();
    public DbSet<Minute> Minutes => Set<Minute>();
    public DbSet<Resolution> Resolutions => Set<Resolution>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<Asset> Assets => Set<Asset>();
    public DbSet<MaintenanceRecord> MaintenanceRecords => Set<MaintenanceRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
