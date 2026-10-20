using Otus.ToDoList.Core.DataAccess;
using Otus.ToDoList.Core.Entities;

namespace Otus.ToDoList.Core.Services;

public class ToDoReportService : IToDoReportService
{
    private readonly IToDoRepository _toDoRepository;

    public ToDoReportService(IToDoRepository toDoRepository)
    {
        _toDoRepository = toDoRepository;
    }

    public async Task<(int total, int completed, int active, DateTime generatedAt)> GetUserStats(Guid userId, CancellationToken ct)
    {
        var items = await _toDoRepository.GetAllByUserId(userId, ct);
        var completed = items.Count(i => i.State == ToDoItemState.Completed);
        var active = items.Count(i => i.State == ToDoItemState.Active);
        return (items.Count, completed, active, DateTime.Now);
    }
}
