using Otus.ToDoList.Core.DataAccess;
using Otus.ToDoList.Core.Entities;

namespace Otus.ToDoList.Infrastructure.DataAccess;

public class InMemoryToDoRepository : IToDoRepository
{
    private readonly List<ToDoItem> _items = new();

    public Task<IReadOnlyList<ToDoItem>> GetAllByUserId(Guid userId, CancellationToken ct)
    {
        IReadOnlyList<ToDoItem> result = _items.Where(i => i.User.UserId == userId).ToList();
        return Task.FromResult(result);
    }

    public Task<IReadOnlyList<ToDoItem>> GetActiveByUserId(Guid userId, CancellationToken ct)
    {
        IReadOnlyList<ToDoItem> result = _items
            .Where(i => i.User.UserId == userId && i.State == ToDoItemState.Active)
            .ToList();
        return Task.FromResult(result);
    }

    public Task<IReadOnlyList<ToDoItem>> Find(Guid userId, Func<ToDoItem, bool> predicate, CancellationToken ct)
    {
        IReadOnlyList<ToDoItem> result = _items.Where(i => i.User.UserId == userId).Where(predicate).ToList();
        return Task.FromResult(result);
    }

    public Task<ToDoItem?> Get(Guid id, CancellationToken ct)
    {
        return Task.FromResult(_items.FirstOrDefault(i => i.Id == id));
    }

    public Task Add(ToDoItem item, CancellationToken ct)
    {
        _items.Add(item);
        return Task.CompletedTask;
    }

    public Task Update(ToDoItem item, CancellationToken ct)
    {
        var index = _items.FindIndex(i => i.Id == item.Id);
        if (index >= 0)
            _items[index] = item;
        return Task.CompletedTask;
    }

    public Task Delete(Guid id, CancellationToken ct)
    {
        _items.RemoveAll(i => i.Id == id);
        return Task.CompletedTask;
    }

    public Task<bool> ExistsByName(Guid userId, string name, CancellationToken ct)
    {
        var exists = _items.Any(i => i.User.UserId == userId
            && string.Equals(i.Name, name, StringComparison.OrdinalIgnoreCase));
        return Task.FromResult(exists);
    }

    public Task<int> CountActive(Guid userId, CancellationToken ct)
    {
        var count = _items.Count(i => i.User.UserId == userId && i.State == ToDoItemState.Active);
        return Task.FromResult(count);
    }
}
