using System.Text.Encodings.Web;
using System.Text.Unicode;
using Microsoft.EntityFrameworkCore;
using SmsService.Api.Data;
using SmsService.Api.Providers;
using SmsService.Api.Services;
using SmsService.Api.Workers;

var builder = WebApplication.CreateBuilder(args);

// The DB file lives in the project folder, so it works the same from Visual Studio and `dotnet run`.
var dbPath = Path.Combine(builder.Environment.ContentRootPath, builder.Configuration["Database:FileName"] ?? "sms.db");
builder.Services.AddDbContext<SmsDbContext>(options => options.UseSqlite($"Data Source={dbPath}"));
builder.Services.AddScoped<MessageService>();

// The SMS provider. To use a real provider: implement ISmsProvider and change this registration.
builder.Services.Configure<FakeSmsProviderOptions>(builder.Configuration.GetSection("FakeSmsProvider"));
builder.Services.AddSingleton<ISmsProvider, FakeSmsProvider>();

// Background sending: POST puts the message in SendQueue; SmsWorker sends it and retries failures.
builder.Services.Configure<SmsWorkerOptions>(builder.Configuration.GetSection("SmsWorker"));
builder.Services.AddSingleton<SendQueue>();
builder.Services.AddHostedService<SmsWorker>();

builder.Services.AddControllers()
    // Return Hebrew as readable text instead of ש... escapes.
    .AddJsonOptions(o => o.JsonSerializerOptions.Encoder = JavaScriptEncoder.Create(UnicodeRanges.All));
builder.Services.AddProblemDetails();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Create the DB file, tables and demo employees if the DB does not exist yet.
using (var scope = app.Services.CreateScope())
{
    scope.ServiceProvider.GetRequiredService<SmsDbContext>().Database.EnsureCreated();
}

// Unexpected errors are returned as ProblemDetails (500) like all other errors.
app.UseExceptionHandler();

// Swagger is always on: it is the way to try the API locally (no auth, no deploy).
app.UseSwagger();
app.UseSwaggerUI();

app.MapControllers();

app.Run();

// Lets the integration tests start the app (WebApplicationFactory<Program>).
public partial class Program;
