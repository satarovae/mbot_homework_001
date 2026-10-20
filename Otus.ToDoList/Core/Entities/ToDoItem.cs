namespace Otus.ToDoList.Core.Entities;

public class ToDoItem
{
    public Guid Id { get; set; }
    public ToDoUser User { get; set; } = null!;
    public string Name { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public ToDoItemState State { get; set; }
    public DateTime? StateChangedAt { get; set; }
}
