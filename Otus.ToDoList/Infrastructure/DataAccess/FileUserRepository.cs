using System.Text.Json;
using Otus.ToDoList.Core.DataAccess;
using Otus.ToDoList.Core.Entities;

namespace Otus.ToDoList.Infrastructure.DataAccess;

public class FileUserRepository : IUserRepository
{
    private readonly string _baseDirectory;

    public FileUserRepository(string baseDirectory)
    {
        _baseDirectory = baseDirectory;
        Directory.CreateDirectory(_baseDirectory);
    }

    public async Task<ToDoUser?> GetUser(Guid userId, CancellationToken ct)
    {
        var path = GetPath(userId);
        if (!File.Exists(path))
            return null;

        return await ReadUser(path, ct);
    }

    public async Task<ToDoUser?> GetUserByTelegramUserId(long telegramUserId, CancellationToken ct)
    {
        foreach (var path in Directory.EnumerateFiles(_baseDirectory, "*.json"))
        {
            var user = await ReadUser(path, ct);
            if (user != null && user.TelegramUserId == telegramUserId)
                return user;
        }

        return null;
    }

    public async Task Add(ToDoUser user, CancellationToken ct)
    {
        Directory.CreateDirectory(_baseDirectory);
        await using var stream = File.Create(GetPath(user.UserId));
        await JsonSerializer.SerializeAsync(stream, user, cancellationToken: ct);
    }

    private string GetPath(Guid userId)
    {
        return Path.Combine(_baseDirectory, $"{userId}.json");
    }

    private static async Task<ToDoUser?> ReadUser(string path, CancellationToken ct)
    {
        await using var stream = File.OpenRead(path);
        return await JsonSerializer.DeserializeAsync<ToDoUser>(stream, cancellationToken: ct);
    }
}
