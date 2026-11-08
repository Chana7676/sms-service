namespace SmsService.Api.Data.Entities;

/// <summary>
/// One employee "instance": an employee (first name + last name + address) with one phone.
/// The same employee can appear in several rows with different phones.
/// Id is an internal DB key only and is never exposed by the API.
/// </summary>
public class Employee
{
    public int Id { get; set; }
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string Address { get; set; } = "";
    public string Phone { get; set; } = "";
    public DateTime CreatedAt { get; set; }
}
