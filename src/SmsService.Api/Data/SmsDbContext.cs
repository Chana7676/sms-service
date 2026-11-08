using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using SmsService.Api.Data.Entities;

namespace SmsService.Api.Data;

public class SmsDbContext(DbContextOptions<SmsDbContext> options) : DbContext(options)
{
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<Message> Messages => Set<Message>();
    public DbSet<MessageStatusInfo> MessageStatuses => Set<MessageStatusInfo>();

    // All times are stored in UTC. SQLite does not keep DateTime.Kind, so mark values as UTC when reading
    // (otherwise the API would return "2026-11-05T14:02:00" without the "Z").
    protected override void ConfigureConventions(ModelConfigurationBuilder configuration) =>
        configuration.Properties<DateTime>().HaveConversion<UtcDateTimeConverter>();

    private class UtcDateTimeConverter() : ValueConverter<DateTime, DateTime>(
        v => v, v => DateTime.SpecifyKind(v, DateTimeKind.Utc));

    protected override void OnModelCreating(ModelBuilder model)
    {
        model.Entity<Employee>(e =>
        {
            e.Property(x => x.FirstName).HasMaxLength(100).IsRequired();
            e.Property(x => x.LastName).HasMaxLength(100).IsRequired();
            e.Property(x => x.Address).HasMaxLength(100).IsRequired();
            e.Property(x => x.Phone).HasMaxLength(20).IsRequired();

            // Finds all rows of one employee (to pick the latest). Not unique: an employee has several rows.
            e.HasIndex(x => new { x.FirstName, x.LastName, x.Address });

            e.HasData(EmployeeSeedData.Employees);
        });

        model.Entity<MessageStatusInfo>(e =>
        {
            e.ToTable("MessageStatuses");
            e.Property(x => x.Name).HasMaxLength(20).IsRequired();

            // The lookup rows come from the enum, so code and DB never drift apart.
            e.HasData(Enum.GetValues<MessageStatus>()
                .Select(s => new MessageStatusInfo { Id = s, Name = s.ToString() }));
        });

        model.Entity<Message>(e =>
        {
            e.Property(x => x.Phone).HasMaxLength(20).IsRequired();
            e.Property(x => x.Body).HasMaxLength(335).IsRequired();

            e.Property(x => x.Status).HasColumnName("StatusId");
            e.HasOne<MessageStatusInfo>().WithMany().HasForeignKey(x => x.Status);

            e.HasOne(x => x.Employee).WithMany().HasForeignKey(x => x.EmployeeId);

            // Supports GET /messages?status=... ordered by newest first.
            e.HasIndex(x => new { x.Status, x.CreatedAt });
        });
    }
}
