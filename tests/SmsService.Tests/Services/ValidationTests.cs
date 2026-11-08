using System.ComponentModel.DataAnnotations;
using SmsService.Api.Contracts;

namespace SmsService.Tests.Services;

/// <summary>
/// Runs the same DataAnnotations rules ASP.NET runs before the controller (the API returns 400 when they fail).
/// Missing fields in the JSON (and explicit nulls) are covered in MessagesApiTests.
/// </summary>
public class ValidationTests
{
    [Fact]
    public void Valid_request_passes() => Assert.Empty(Errors(Request(body: "שלום")));

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Empty_body_fails(string body) => Assert.Contains("Body", Errors(Request(body)));

    [Fact]
    public void Body_of_335_chars_passes_and_336_fails()
    {
        Assert.Empty(Errors(Request(new string('א', 335))));
        Assert.Contains("Body", Errors(Request(new string('א', 336))));
    }

    [Fact]
    public void Recipient_with_missing_field_or_bad_phone_fails()
    {
        var recipient = new RecipientDto { FirstName = "שרה", LastName = "", Address = "הנשיא 5, ירושלים", Phone = "123" };

        var errors = Errors(recipient);

        Assert.Contains("LastName", errors);
        Assert.Contains("Phone", errors);
    }

    [Fact]
    public void Empty_phone_gets_one_error_from_required_only()
    {
        var recipient = new RecipientDto { FirstName = "שרה", LastName = "כהן", Address = "הנשיא 5, ירושלים", Phone = "" };

        Assert.Single(Errors(recipient), "Phone");
    }

    private static SendMessageRequest Request(string body) => new()
    {
        To = new RecipientDto { FirstName = "שרה", LastName = "כהן", Address = "הנשיא 5, ירושלים", Phone = "0522222223" },
        Body = body
    };

    private static List<string> Errors(object model)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(model, new ValidationContext(model), results, validateAllProperties: true);
        return results.SelectMany(r => r.MemberNames).ToList();
    }
}
