using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using SmsService.Api.Contracts;

namespace SmsService.Tests.Api;

/// <summary>
/// Runs the real app in memory and calls it over HTTP, to check the status codes of each endpoint.
/// Each test class instance gets its own temporary DB file.
/// </summary>
public class MessagesApiTests : IDisposable
{
    private readonly string _dbFile = Path.Combine(Path.GetTempPath(), $"sms-test-{Guid.NewGuid()}.db");
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public MessagesApiTests()
    {
        // Read by Program at startup: a temp DB, and a provider that never fails (predictable results).
        Environment.SetEnvironmentVariable("Database__FileName", _dbFile);
        Environment.SetEnvironmentVariable("FakeSmsProvider__ErrorRate", "0");
        _factory = new WebApplicationFactory<Program>();
        _client = _factory.CreateClient();
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
        Environment.SetEnvironmentVariable("Database__FileName", null);
        Environment.SetEnvironmentVariable("FakeSmsProvider__ErrorRate", null);
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
        File.Delete(_dbFile);
    }

    [Fact]
    public async Task Post_returns_202_and_the_worker_sends_it_in_the_background()
    {
        var post = await _client.PostAsJsonAsync("/messages", Request("0522222223"));

        Assert.Equal(HttpStatusCode.Accepted, post.StatusCode);
        var created = await post.Content.ReadFromJsonAsync<CreateMessageResponse>();
        Assert.EndsWith($"/messages/{created!.Id}", post.Headers.Location!.ToString());

        var message = await WaitForStatus(created.Id, "Sent");
        Assert.Equal("שרה", message.To.FirstName);
        Assert.Equal(1, message.Attempts);
        Assert.Equal(DateTimeKind.Utc, message.CreatedAt.Kind);
    }

    [Fact]
    public async Task Post_with_invalid_input_returns_400() =>
        Assert.Equal(HttpStatusCode.BadRequest, (await _client.PostAsJsonAsync("/messages", Request("123"))).StatusCode);

    [Theory]
    [InlineData("""{ "body": "שלום" }""")]                                              // no "to"
    [InlineData("""{ "to": null, "body": "שלום" }""")]                                  // explicit null
    [InlineData("""{ "to": { "firstName": "שרה", "lastName": "כהן", "address": "הנשיא 5, ירושלים", "phone": "0522222223" } }""")] // no body
    [InlineData("""{ "to": { "firstName": "שרה", "lastName": "כהן", "address": "הנשיא 5, ירושלים", "phone": "0522222223" }, "body": null }""")]
    public async Task Post_with_missing_or_null_fields_returns_400(string json)
    {
        var response = await _client.PostAsync("/messages", new StringContent(json, System.Text.Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Post_with_old_phone_returns_422() =>
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await _client.PostAsJsonAsync("/messages", Request("0522222221"))).StatusCode);

    [Theory]
    [InlineData("/messages/999")]
    [InlineData("/messages/abc")]
    public async Task Get_unknown_message_returns_404(string url) =>
        Assert.Equal(HttpStatusCode.NotFound, (await _client.GetAsync(url)).StatusCode);

    [Fact]
    public async Task List_filters_by_status_case_insensitive()
    {
        var post = await _client.PostAsJsonAsync("/messages", Request("0522222223"));
        await WaitForStatus((await post.Content.ReadFromJsonAsync<CreateMessageResponse>())!.Id, "Sent");

        var sent = await _client.GetFromJsonAsync<PagedResponse<MessageResponse>>("/messages?status=sent");
        var failed = await _client.GetFromJsonAsync<PagedResponse<MessageResponse>>("/messages?status=Failed");

        Assert.Equal(1, sent!.TotalCount);
        Assert.Equal(0, failed!.TotalCount);
        Assert.Empty(failed.Items);
    }

    [Theory]
    [InlineData("/messages?status=abc")]
    [InlineData("/messages?status=2")]
    [InlineData("/messages?page=0")]
    [InlineData("/messages?pageSize=500")]
    public async Task List_with_bad_query_returns_400(string url) =>
        Assert.Equal(HttpStatusCode.BadRequest, (await _client.GetAsync(url)).StatusCode);

    [Fact]
    public async Task Employees_lists_one_row_per_employee_with_the_current_phone()
    {
        var employees = await _client.GetFromJsonAsync<List<EmployeeResponse>>("/employees");

        Assert.Equal(10, employees!.Count);
        Assert.Equal("0522222223", employees.Single(e => e.FirstName == "שרה").Phone);
        Assert.False(employees.Single(e => e.LastName == "כץ").HasValidPhone);
    }

    // The worker sends in the background, so poll GET for up to 5 seconds.
    private async Task<MessageResponse> WaitForStatus(int id, string status)
    {
        for (var i = 0; i < 50; i++)
        {
            var message = await _client.GetFromJsonAsync<MessageResponse>($"/messages/{id}");
            if (message!.Status == status)
                return message;
            await Task.Delay(100);
        }
        throw new TimeoutException($"Message {id} did not become {status}.");
    }

    private static SendMessageRequest Request(string phone) => new()
    {
        To = new RecipientDto { FirstName = "שרה", LastName = "כהן", Address = "הנשיא 5, ירושלים", Phone = phone },
        Body = "תזכורת: ישיבת צוות מחר ב-9:00"
    };
}
