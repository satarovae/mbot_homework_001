using Otus.ToDoList.Core.Entities;
using Otus.ToDoList.Core.Exceptions;
using Otus.ToDoList.Core.Services;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace Otus.ToDoList.TelegramBot;

public class UpdateHandler : IUpdateHandler
{
    private readonly IUserService _userService;
    private readonly IToDoService _toDoService;
    private readonly IToDoReportService _reportService;

    public UpdateHandler(IUserService userService, IToDoService toDoService, IToDoReportService reportService)
    {
        _userService = userService;
        _toDoService = toDoService;
        _reportService = reportService;
    }

    public async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken ct)
    {
        if (update.Message is not { Text: { } text } message || message.From == null)
            return;

        var chatId = message.Chat.Id;
        var user = await _userService.GetUser(message.From.Id, ct);

        try
        {
            var parts = text.Trim().Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
            var command = parts.Length > 0 ? parts[0].ToLower() : "";
            var argument = parts.Length > 1 ? parts[1].Trim() : "";

            var at = command.IndexOf('@');
            if (at > 0)
                command = command.Substring(0, at);

            if (command == "/start")
            {
                user = await _userService.RegisterUser(message.From.Id, message.From.Username ?? message.From.FirstName, ct);
                await Send(botClient, chatId, $"Привет, {Escape(user.TelegramUserName)}! Вы зарегистрированы.\nВведите /help, чтобы посмотреть команды.", user, ct);
                return;
            }

            if (command == "/help")
            {
                await Send(botClient, chatId, GetHelpText(user), user, ct);
                return;
            }

            if (command == "/info")
            {
                await Send(botClient, chatId, "Версия программы 1.0, дата создания 08.10.2026", user, ct);
                return;
            }

            if (user == null)
            {
                await Send(botClient, chatId, "Сначала зарегистрируйтесь командой /start", null, ct);
                return;
            }

            switch (command)
            {
                case "/addtask":
                    await AddTask(botClient, chatId, user, argument, ct);
                    break;
                case "/showtasks":
                    var activeTasks = await _toDoService.GetActiveByUserId(user.UserId, ct);
                    await ShowTasks(botClient, chatId, user, activeTasks, "Активных задач нет", false, ct);
                    break;
                case "/showalltasks":
                    var allTasks = await _toDoService.GetAllByUserId(user.UserId, ct);
                    await ShowTasks(botClient, chatId, user, allTasks, "Задач нет", true, ct);
                    break;
                case "/removetask":
                    await _toDoService.Delete(ParseId(argument), ct);
                    await Send(botClient, chatId, "Задача удалена", user, ct);
                    break;
                case "/completetask":
                    await _toDoService.MarkCompleted(ParseId(argument), ct);
                    await Send(botClient, chatId, "Задача завершена", user, ct);
                    break;
                case "/report":
                    await Report(botClient, chatId, user, ct);
                    break;
                case "/find":
                    if (string.IsNullOrWhiteSpace(argument))
                        throw new ArgumentException("Укажите начало названия задачи: /find текст");
                    var found = await _toDoService.Find(user, argument, ct);
                    await ShowTasks(botClient, chatId, user, found, "Ничего не найдено", false, ct);
                    break;
                default:
                    await Send(botClient, chatId, "Неизвестная команда. Введите /help", user, ct);
                    break;
            }
        }
        catch (Exception ex) when (ex is ArgumentException
            or TaskCountLimitException
            or TaskLengthLimitException
            or DuplicateTaskException)
        {
            await Send(botClient, chatId, Escape(ex.Message), user, ct);
        }
    }

    public Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, HandleErrorSource source, CancellationToken ct)
    {
        Console.WriteLine($"Ошибка: {exception.Message}");
        return Task.CompletedTask;
    }

    private async Task AddTask(ITelegramBotClient botClient, long chatId, ToDoUser user, string name, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Укажите название задачи: /addtask Название");

        var item = await _toDoService.Add(user, name, ct);
        await Send(botClient, chatId, $"Задача \"{Escape(item.Name)}\" добавлена, Id: `{item.Id}`", user, ct);
    }

    private async Task ShowTasks(ITelegramBotClient botClient, long chatId, ToDoUser user,
        IReadOnlyList<ToDoItem> tasks, string emptyText, bool showState, CancellationToken ct)
    {
        if (tasks.Count == 0)
        {
            await Send(botClient, chatId, emptyText, user, ct);
            return;
        }

        var lines = new List<string>();
        foreach (var task in tasks)
        {
            var line = $"{Escape(task.Name)} - {task.CreatedAt.ToLocalTime():dd.MM.yyyy HH:mm:ss} - `{task.Id}`";
            if (showState)
                line = $"({task.State}) " + line;
            lines.Add(line);
        }

        await Send(botClient, chatId, string.Join("\n", lines), user, ct);
    }

    private async Task Report(ITelegramBotClient botClient, long chatId, ToDoUser user, CancellationToken ct)
    {
        var (total, completed, active, generatedAt) = await _reportService.GetUserStats(user.UserId, ct);
        var text = $"Статистика по задачам на {generatedAt:dd.MM.yyyy HH:mm:ss}. " +
                   $"Всего: {total}; Завершенных: {completed}; Активных: {active};";
        await Send(botClient, chatId, text, user, ct);
    }

    private static Guid ParseId(string argument)
    {
        if (!Guid.TryParse(argument.Trim('`'), out var id))
            throw new ArgumentException("Укажите корректный Id задачи");
        return id;
    }

    private static string GetHelpText(ToDoUser? user)
    {
        if (user == null)
            return "Доступные команды:\n/start - регистрация\n/help - справка\n/info - информация о программе";

        return "Доступные команды:\n" +
               "/addtask Название - добавить задачу\n" +
               "/showtasks - активные задачи\n" +
               "/showalltasks - все задачи\n" +
               "/completetask Id - завершить задачу\n" +
               "/removetask Id - удалить задачу\n" +
               "/find текст - найти задачи по началу названия\n" +
               "/report - статистика\n" +
               "/info - информация о программе";
    }

    private static ReplyKeyboardMarkup GetKeyboard(ToDoUser? user)
    {
        if (user == null)
        {
            return new ReplyKeyboardMarkup(new KeyboardButton("/start"))
            {
                ResizeKeyboard = true
            };
        }

        return new ReplyKeyboardMarkup(new[]
        {
            new KeyboardButton("/showalltasks"),
            new KeyboardButton("/showtasks"),
            new KeyboardButton("/report")
        })
        {
            ResizeKeyboard = true
        };
    }

    private static Task Send(ITelegramBotClient botClient, long chatId, string text, ToDoUser? user, CancellationToken ct)
    {
        return botClient.SendMessage(chatId, text, parseMode: ParseMode.Markdown,
            replyMarkup: GetKeyboard(user), cancellationToken: ct);
    }

    private static string Escape(string text)
    {
        return text.Replace("\\", "\\\\")
            .Replace("_", "\\_")
            .Replace("*", "\\*")
            .Replace("`", "\\`")
            .Replace("[", "\\[");
    }
}
