using Microsoft.EntityFrameworkCore;
using SmsService.Api.Data.Entities;

namespace SmsService.Api.Services;

/// <summary>
/// Several rows for the same employee (first name + last name + address) are duplicates;
/// the latest row (CreatedAt, then Id on a tie) holds the current phone.
/// Both the employee list and message validation use this, so they always agree on the current phone.
/// </summary>
public static class CurrentEmployees
{
    /// <summary>One row per employee: the rows that have no newer row for the same employee.</summary>
    public static IQueryable<Employee> Of(IQueryable<Employee> employees) =>
        employees.Where(e => !employees.Any(newer =>
            newer.FirstName == e.FirstName &&
            newer.LastName == e.LastName &&
            newer.Address == e.Address &&
            (newer.CreatedAt > e.CreatedAt || (newer.CreatedAt == e.CreatedAt && newer.Id > e.Id))));

    public static Task<Employee?> FindAsync(IQueryable<Employee> employees, string firstName, string lastName, string address) =>
        Of(employees).FirstOrDefaultAsync(e =>
            e.FirstName == firstName && e.LastName == lastName && e.Address == address);
}
