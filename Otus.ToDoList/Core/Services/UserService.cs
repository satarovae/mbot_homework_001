using Otus.ToDoList.Core.DataAccess;
using Otus.ToDoList.Core.Entities;

namespace Otus.ToDoList.Core.Services;

public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;

    public UserService(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<ToDoUser> RegisterUser(long telegramUserId, string telegramUserName, CancellationToken ct)
    {
        var user = await _userRepository.GetUserByTelegramUserId(telegramUserId, ct);
        if (user != null)
            return user;

        user = new ToDoUser
        {
            UserId = Guid.NewGuid(),
            TelegramUserId = telegramUserId,
            TelegramUserName = telegramUserName,
            RegisteredAt = DateTime.UtcNow
        };
        await _userRepository.Add(user, ct);
        return user;
    }

    public Task<ToDoUser?> GetUser(long telegramUserId, CancellationToken ct)
    {
        return _userRepository.GetUserByTelegramUserId(telegramUserId, ct);
    }
}
