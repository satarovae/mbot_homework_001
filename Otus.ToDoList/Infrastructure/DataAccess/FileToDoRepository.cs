using System.Text.Json;
using Otus.ToDoList.Core.DataAccess;
using Otus.ToDoList.Core.Entities;

namespace Otus.ToDoList.Infrastructure.DataAccess;

public class FileToDoRepository : IToDoRepository
{
    private const string IndexFileName = "index.json";

    private readonly string _baseDirectory;
    private readonly string _indexPath;

    public FileToDoRepository(string baseDirectory)
    {
        _baseDirectory = baseDirectory;
        _indexPath = Path.Combine(_baseDirectory, IndexFileName);
        Directory.CreateDirectory(_baseDirectory);
    }

    public async Task<IReadOnlyList<ToDoItem>> GetAllByUserId(Guid userId, CancellationToken ct)
    {
        return await ReadUserItems(userId, ct);
    }

    public async Task<IReadOnlyList<ToDoItem>> GetActiveByUserId(Guid userId, CancellationToken ct)
    {
        var items = await ReadUserItems(userId, ct);
        return items.Where(i => i.State == ToDoItemState.Active).ToList();
    }

    public async Task<IReadOnlyList<ToDoItem>> Find(Guid userId, Func<ToDoItem, bool> predicate, CancellationToken ct)
    {
        var items = await ReadUserItems(userId, ct);
        return items.Where(predicate).ToList();
    }

    public async Task<ToDoItem?> Get(Guid id, CancellationToken ct)
    {
        var index = await LoadIndex(ct);
        if (!index.TryGetValue(id, out var userId))
            return null;

        var path = GetItemPath(userId, id);
        if (!File.Exists(path))
            return null;

        return await ReadItem(path, ct);
    }

    public async Task Add(ToDoItem item, CancellationToken ct)
    {
        var index = await LoadIndex(ct);
        await WriteItem(item, ct);
        index[item.Id] = item.User.UserId;
        await SaveIndex(index, ct);
    }

    public async Task Update(ToDoItem item, CancellationToken ct)
    {
        var path = GetItemPath(item.User.UserId, item.Id);
        if (!File.Exists(path))
            return;

        await WriteItem(item, ct);
    }

    public async Task Delete(Guid id, CancellationToken ct)
    {
        var index = await LoadIndex(ct);
        if (!index.Remove(id, out var userId))
            return;

        var path = GetItemPath(userId, id);
        if (File.Exists(path))
            File.Delete(path);

        await SaveIndex(index, ct);
    }

    public async Task<bool> ExistsByName(Guid userId, string name, CancellationToken ct)
    {
        var items = await ReadUserItems(userId, ct);
        return items.Any(i => string.Equals(i.Name, name, StringComparison.OrdinalIgnoreCase));
    }

    public async Task<int> CountActive(Guid userId, CancellationToken ct)
    {
        var items = await ReadUserItems(userId, ct);
        return items.Count(i => i.State == ToDoItemState.Active);
    }

    private string GetUserDirectory(Guid userId)
    {
        return Path.Combine(_baseDirectory, userId.ToString());
    }

    private string GetItemPath(Guid userId, Guid itemId)
    {
        return Path.Combine(GetUserDirectory(userId), $"{itemId}.json");
    }

    private async Task<List<ToDoItem>> ReadUserItems(Guid userId, CancellationToken ct)
    {
        var items = new List<ToDoItem>();
        var directory = GetUserDirectory(userId);
        if (!Directory.Exists(directory))
            return items;

        foreach (var path in Directory.EnumerateFiles(directory, "*.json"))
        {
            var item = await ReadItem(path, ct);
            if (item != null)
                items.Add(item);
        }

        return items.OrderBy(i => i.CreatedAt).ToList();
    }

    private static async Task<ToDoItem?> ReadItem(string path, CancellationToken ct)
    {
        await using var stream = File.OpenRead(path);
        return await JsonSerializer.DeserializeAsync<ToDoItem>(stream, cancellationToken: ct);
    }

    private async Task WriteItem(ToDoItem item, CancellationToken ct)
    {
        Directory.CreateDirectory(GetUserDirectory(item.User.UserId));
        await using var stream = File.Create(GetItemPath(item.User.UserId, item.Id));
        await JsonSerializer.SerializeAsync(stream, item, cancellationToken: ct);
    }

    private async Task<Dictionary<Guid, Guid>> LoadIndex(CancellationToken ct)
    {
        if (!File.Exists(_indexPath))
        {
            var rebuilt = await BuildIndex(ct);
            await SaveIndex(rebuilt, ct);
            return rebuilt;
        }

        await using var stream = File.OpenRead(_indexPath);
        var index = await JsonSerializer.DeserializeAsync<Dictionary<Guid, Guid>>(stream, cancellationToken: ct);
        return index ?? new Dictionary<Guid, Guid>();
    }

    private async Task SaveIndex(Dictionary<Guid, Guid> index, CancellationToken ct)
    {
        Directory.CreateDirectory(_baseDirectory);
        await using var stream = File.Create(_indexPath);
        await JsonSerializer.SerializeAsync(stream, index, cancellationToken: ct);
    }

    private async Task<Dictionary<Guid, Guid>> BuildIndex(CancellationToken ct)
    {
        var index = new Dictionary<Guid, Guid>();
        foreach (var directory in Directory.EnumerateDirectories(_baseDirectory))
        {
            if (!Guid.TryParse(Path.GetFileName(directory), out var userId))
                continue;

            foreach (var path in Directory.EnumerateFiles(directory, "*.json"))
            {
                var item = await ReadItem(path, ct);
                if (item != null)
                    index[item.Id] = userId;
            }
        }

        return index;
    }
}
