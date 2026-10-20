using Otus.ToDoList.Core.DataAccess;
using Otus.ToDoList.Core.Entities;

namespace Otus.ToDoList.Infrastructure.DataAccess;

public class InMemoryUserRepository : IUserRepository
{
    private readonly List<ToDoUser> _users = new();

    public Task<ToDoUser?> GetUser(Guid userId, CancellationToken ct)
    {
        return Task.FromResult(_users.FirstOrDefault(u => u.UserId == userId));
    }

    public Task<ToDoUser?> GetUserByTelegramUserId(long telegramUserId, CancellationToken ct)
    {
        return Task.FromResult(_users.FirstOrDefault(u => u.TelegramUserId == telegramUserId));
    }

    public Task Add(ToDoUser user, CancellationToken ct)
    {
        _users.Add(user);
        return Task.CompletedTask;
    }
}
