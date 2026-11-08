using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using SmsService.Api.Data;

namespace SmsService.Tests;

/// <summary>
/// Base class for tests that need a real DB: a fresh in-memory SQLite DB (with the demo employees) per test.
/// </summary>
public abstract class TestDb : IDisposable
{
    private readonly SqliteConnection _connection;
    protected readonly SmsDbContext Db;

    protected TestDb()
    {
        // In-memory SQLite lives as long as the connection is open.
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();
        Db = new SmsDbContext(new DbContextOptionsBuilder<SmsDbContext>().UseSqlite(_connection).Options);
        Db.Database.EnsureCreated();
    }

    public void Dispose()
    {
        Db.Dispose();
        _connection.Dispose();
    }
}
