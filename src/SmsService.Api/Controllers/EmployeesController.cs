using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmsService.Api.Contracts;
using SmsService.Api.Data;
using SmsService.Api.Services;

namespace SmsService.Api.Controllers;

/// <summary>For the UI: one row per employee with the current phone. A plain query, so no service layer.</summary>
[ApiController]
[Route("employees")]
public class EmployeesController(SmsDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<List<EmployeeResponse>> List()
    {
        var employees = await CurrentEmployees.Of(db.Employees)
            .OrderBy(e => e.LastName).ThenBy(e => e.FirstName)
            .ToListAsync();

        return employees
            .Select(e => new EmployeeResponse(e.FirstName, e.LastName, e.Address, e.Phone, PhoneNumber.IsValid(e.Phone)))
            .ToList();
    }
}
