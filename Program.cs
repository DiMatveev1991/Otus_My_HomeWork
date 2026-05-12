using System;
using Core.DataAccess;
using Core.Services;
using Infrastructure.DataAccess;
using Otus.ToDoList.ConsoleBot;
using TelegramBot;

try
{
	// 1. Создаём репозитории (Infrastructure)
	IUserRepository userRepository = new InMemoryUserRepository();
	IToDoRepository toDoRepository = new InMemoryToDoRepository();

	// 2. Создаём сервисы и внедряем репозитории через конструктор (DI)
	IUserService userService = new UserService(userRepository);
	IToDoService toDoService = new AutoPartsToDoService(toDoRepository,
		maxTaskCount: 10, maxTaskLength: 100);
	IToDoReportService toDoReportService = new ToDoReportService(toDoRepository);

	// 3. Создаём обработчик и передаём сервисы
	var handler = new UpdateHandler(userService, toDoService, toDoReportService);

	// 4. Запускаем консольного бота
	var botClient = new ConsoleBotClient();
	botClient.StartReceiving(handler);
}
catch (Exception ex)
{
	Console.WriteLine($"Критическая ошибка: {ex.GetType().Name}");
	Console.WriteLine($"Сообщение: {ex.Message}");
	Console.WriteLine($"StackTrace: {ex.StackTrace}");
}
