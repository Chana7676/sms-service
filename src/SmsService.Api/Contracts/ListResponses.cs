namespace SmsService.Api.Contracts;

public record CreateMessageResponse(int Id);

/// <summary>An employee with the current phone (the internal Id is not exposed).</summary>
public record EmployeeResponse(string FirstName, string LastName, string Address, string Phone, bool HasValidPhone);

public record PagedResponse<T>(List<T> Items, int Page, int PageSize, int TotalCount);
