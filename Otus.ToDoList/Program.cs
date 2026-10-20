using Otus.ToDoList.Core.Services;
using Otus.ToDoList.Infrastructure.DataAccess;
using Otus.ToDoList.TelegramBot;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace Otus.ToDoList;

internal class Program
{
    private const int MaxTaskCount = 20;
    private const int MaxTaskLength = 100;
    private const string DataDirectory = "Data";

    private static async Task Main()
    {
        var token = "8852045176:AAGvvjcrhPDHFnBnxddxNYV3wUjl0VIXAK4";
        if (string.IsNullOrWhiteSpace(token))
        {
            Console.WriteLine("Не задана переменная окружения TELEGRAM_BOT_TOKEN");
            return;
        }

        var userRepository = new FileUserRepository(Path.Combine(DataDirectory, "Users"));
        var toDoRepository = new FileToDoRepository(Path.Combine(DataDirectory, "ToDoItems"));
        var userService = new UserService(userRepository);
        var toDoService = new ToDoService(toDoRepository, MaxTaskCount, MaxTaskLength);
        var reportService = new ToDoReportService(toDoRepository);

        using var cts = new CancellationTokenSource();

        var botClient = new TelegramBotClient(token, cancellationToken: cts.Token);
        var receiverOptions = new ReceiverOptions
        {
            AllowedUpdates = [UpdateType.Message],
            DropPendingUpdates = true
        };
        var handler = new UpdateHandler(userService, toDoService, reportService);

        try
        {
            await botClient.SetMyCommands(GetCommands(), cancellationToken: cts.Token);
            botClient.StartReceiving(handler, receiverOptions, cts.Token);

            var me = await botClient.GetMe(cts.Token);
            Console.WriteLine($"{me.FirstName} запущен!");

            while (true)
            {
                Console.WriteLine("Нажмите клавишу A для выхода");
                var key = Console.ReadKey(true);
                if (key.Key == ConsoleKey.A)
                {
                    cts.Cancel();
                    Console.WriteLine("Бот остановлен");
                    break;
                }

                me = await botClient.GetMe(cts.Token);
                Console.WriteLine($"Id: {me.Id}, Имя: {me.FirstName}, Username: @{me.Username}");
            }
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("Операция отменена");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка: {ex.Message}");
        }
    }

    private static BotCommand[] GetCommands()
    {
        return
        [
            new BotCommand { Command = "start", Description = "Регистрация" },
            new BotCommand { Command = "help", Description = "Справка по командам" },
            new BotCommand { Command = "info", Description = "Информация о программе" },
            new BotCommand { Command = "addtask", Description = "Добавить задачу" },
            new BotCommand { Command = "showtasks", Description = "Активные задачи" },
            new BotCommand { Command = "showalltasks", Description = "Все задачи" },
            new BotCommand { Command = "completetask", Description = "Завершить задачу по Id" },
            new BotCommand { Command = "removetask", Description = "Удалить задачу по Id" },
            new BotCommand { Command = "find", Description = "Найти задачи по началу названия" },
            new BotCommand { Command = "report", Description = "Статистика по задачам" }
        ];
    }
}
