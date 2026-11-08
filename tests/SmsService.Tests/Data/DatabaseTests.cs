using Microsoft.EntityFrameworkCore;
using SmsService.Api.Data;
using SmsService.Api.Data.Entities;

namespace SmsService.Tests.Data;

public class DatabaseTests : TestDb
{
    [Fact]
    public async Task Status_lookup_table_matches_the_enum()
    {
        var rows = await Db.MessageStatuses.OrderBy(s => s.Id).ToListAsync();

        Assert.Equal(Enum.GetValues<MessageStatus>(), rows.Select(r => r.Id));
        Assert.Equal("Pending", rows[0].Name);
    }

    [Fact]
    public async Task Demo_employees_are_created_with_the_db()
    {
        Assert.Equal(EmployeeSeedData.Employees.Length, await Db.Employees.CountAsync());
        Assert.True(await Db.Employees.AnyAsync(e => e.LastName == "כץ" && e.Phone == "051234"));
    }
}
