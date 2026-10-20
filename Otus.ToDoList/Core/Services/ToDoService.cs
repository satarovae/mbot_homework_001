using Otus.ToDoList.Core.DataAccess;
using Otus.ToDoList.Core.Entities;
using Otus.ToDoList.Core.Exceptions;

namespace Otus.ToDoList.Core.Services;

public class ToDoService : IToDoService
{
    private readonly IToDoRepository _toDoRepository;
    private readonly int _maxTaskCount;
    private readonly int _maxTaskLength;

    public ToDoService(IToDoRepository toDoRepository, int maxTaskCount, int maxTaskLength)
    {
        _toDoRepository = toDoRepository;
        _maxTaskCount = maxTaskCount;
        _maxTaskLength = maxTaskLength;
    }

    public Task<IReadOnlyList<ToDoItem>> GetAllByUserId(Guid userId, CancellationToken ct)
    {
        return _toDoRepository.GetAllByUserId(userId, ct);
    }

    public Task<IReadOnlyList<ToDoItem>> GetActiveByUserId(Guid userId, CancellationToken ct)
    {
        return _toDoRepository.GetActiveByUserId(userId, ct);
    }

    public Task<IReadOnlyList<ToDoItem>> Find(ToDoUser user, string namePrefix, CancellationToken ct)
    {
        return _toDoRepository.Find(user.UserId,
            i => i.Name.StartsWith(namePrefix, StringComparison.OrdinalIgnoreCase), ct);
    }

    public async Task<ToDoItem> Add(ToDoUser user, string name, CancellationToken ct)
    {
        if (await _toDoRepository.CountActive(user.UserId, ct) >= _maxTaskCount)
            throw new TaskCountLimitException(_maxTaskCount);

        if (name.Length > _maxTaskLength)
            throw new TaskLengthLimitException(name.Length, _maxTaskLength);

        if (await _toDoRepository.ExistsByName(user.UserId, name, ct))
            throw new DuplicateTaskException(name);

        var item = new ToDoItem
        {
            Id = Guid.NewGuid(),
            User = user,
            Name = name,
            CreatedAt = DateTime.UtcNow,
            State = ToDoItemState.Active
        };
        await _toDoRepository.Add(item, ct);
        return item;
    }

    public async Task MarkCompleted(Guid id, CancellationToken ct)
    {
        var item = await _toDoRepository.Get(id, ct);
        if (item == null)
            throw new ArgumentException($"Задача с Id {id} не найдена");

        item.State = ToDoItemState.Completed;
        item.StateChangedAt = DateTime.UtcNow;
        await _toDoRepository.Update(item, ct);
    }

    public async Task Delete(Guid id, CancellationToken ct)
    {
        var item = await _toDoRepository.Get(id, ct);
        if (item == null)
            throw new ArgumentException($"Задача с Id {id} не найдена");

        await _toDoRepository.Delete(id, ct);
    }
}
