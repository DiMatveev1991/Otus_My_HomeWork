using System;
using System.Threading;
using Core.DataAccess;
using Core.Services;
using Infrastructure.DataAccess;
using Otus.ToDoList.ConsoleBot;
using TelegramBot;

try
{
	// 0. Источник отмены для всего приложения.
	//    Используется для:
	//    - корректного завершения по Ctrl+C
	//    - корректного завершения по команде /exit
	using var cts = new CancellationTokenSource();

	Console.CancelKeyPress += (_, e) =>
	{
		cts.Cancel();
		e.Cancel = true;
	};

	// 1. Репозитории (Infrastructure)
	IUserRepository userRepository = new InMemoryUserRepository();
	IToDoRepository toDoRepository = new InMemoryToDoRepository();

	// 2. Сервисы с DI через конструктор
	IUserService userService = new UserService(userRepository);
	IToDoService toDoService = new AutoPartsToDoService(toDoRepository,
		maxTaskCount: 10, maxTaskLength: 100);
	IToDoReportService toDoReportService = new ToDoReportService(toDoRepository);

	// 3. Обработчик с возможностью инициировать отмену из /exit
	var handler = new UpdateHandler(userService, toDoService, toDoReportService, cts);

	// 4. Запускаем консольного бота с токеном отмены
	var botClient = new ConsoleBotClient();
	botClient.StartReceiving(handler, cts.Token);
}
catch (Exception ex)
{
	Console.WriteLine($"Критическая ошибка: {ex.GetType().Name}");
	Console.WriteLine($"Сообщение: {ex.Message}");
	Console.WriteLine($"StackTrace: {ex.StackTrace}");
}