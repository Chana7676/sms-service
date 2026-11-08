using Microsoft.EntityFrameworkCore;
using SmsService.Api.Services;

namespace SmsService.Tests.Services;

/// <summary>Uses the demo employees (EmployeeSeedData), which contain every duplicate case on purpose.</summary>
public class CurrentEmployeesTests : TestDb
{
    [Theory]
    [InlineData("ישראל", "ישראלי", "הרצל 10, רחובות", "0501111111")]     // one row
    [InlineData("שרה", "כהן", "הנשיא 5, ירושלים", "0522222223")]          // three rows -> latest
    [InlineData("דוד", "לוי", "ויצמן 3, בית דגן", "0533333333")]           // same phone twice
    [InlineData("אבי", "גולן", "הזית 4, ראשון לציון", "0548888882")]       // same date -> higher Id
    [InlineData("דן", "כץ", "הגפן 1, לוד", "051234")]                      // invalid latest phone is returned as is
    public async Task Returns_the_latest_row(string first, string last, string address, string expectedPhone)
    {
        var employee = await CurrentEmployees.FindAsync(Db.Employees, first, last, address);

        Assert.Equal(expectedPhone, employee!.Phone);
    }

    [Fact]
    public async Task Same_name_at_different_addresses_are_different_employees()
    {
        var holon = await CurrentEmployees.FindAsync(Db.Employees, "יוסף", "מזרחי", "הגפן 2, חולון");
        var ashdod = await CurrentEmployees.FindAsync(Db.Employees, "יוסף", "מזרחי", "התמר 9, אשדוד");

        Assert.Equal("0555555555", holon!.Phone);
        Assert.Equal("0556666666", ashdod!.Phone);
    }

    [Fact]
    public async Task Unknown_employee_returns_null() =>
        Assert.Null(await CurrentEmployees.FindAsync(Db.Employees, "אין", "כזה", "שום מקום"));

    [Fact]
    public async Task List_has_one_row_per_employee()
    {
        var all = await CurrentEmployees.Of(Db.Employees).ToListAsync();

        // 14 rows: Sarah has 3, David 2, Avi 2 -> 10 employees.
        Assert.Equal(10, all.Count);
        Assert.Equal(all.Count, all.Select(e => (e.FirstName, e.LastName, e.Address)).Distinct().Count());
    }
}
