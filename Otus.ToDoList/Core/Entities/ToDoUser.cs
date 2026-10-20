namespace Otus.ToDoList.Core.Entities;

public class ToDoUser
{
    public Guid UserId { get; set; }
    public long TelegramUserId { get; set; }
    public string TelegramUserName { get; set; } = "";
    public DateTime RegisteredAt { get; set; }
}
