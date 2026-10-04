using System.Diagnostics.CodeAnalysis;
using EventManagement.Api.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.EntityFrameworkCore;

namespace EventManagement.Tests;

public class FakeSession : ISession
{
    private readonly Dictionary<string, byte[]> _store = new();

    public bool IsAvailable => true;
    public string Id { get; } = Guid.NewGuid().ToString();
    public IEnumerable<string> Keys => _store.Keys;

    public void Clear() => _store.Clear();
    public Task CommitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task LoadAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    public void Remove(string key) => _store.Remove(key);
    public void Set(string key, byte[] value) => _store[key] = value;
    public bool TryGetValue(string key, [NotNullWhen(true)] out byte[]? value) =>
        _store.TryGetValue(key, out value);
}

public class FakeSessionFeature : ISessionFeature
{
    public ISession Session { get; set; } = new FakeSession();
}

public static class TestHelpers
{
    public static AppDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    public static DefaultHttpContext CreateHttpContext(int? userId = null, string? role = null)
    {
        var http = new DefaultHttpContext();
        var session = new FakeSession();
        if (userId != null) session.SetInt32("UserId", userId.Value);
        if (role != null) session.SetString("Role", role);
        http.Features.Set<ISessionFeature>(new FakeSessionFeature { Session = session });
        return http;
    }
}
