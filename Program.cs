using System;
using AutoPartsBot;
using Otus.ToDoList.ConsoleBot;
using Otus_My_HomeWork.Services;

try
{
	var userService = new UserService();
	var toDoService = new AutoPartsToDoService(maxTaskCount: 10, maxTaskLength: 100);
	var handler = new UpdateHandler(userService, toDoService);

	var botClient = new ConsoleBotClient();
	botClient.StartReceiving(handler);
}
catch (Exception ex)
{
	Console.WriteLine($"Критическая ошибка: {ex.GetType().Name}");
	Console.WriteLine($"Сообщение: {ex.Message}");
	Console.WriteLine($"StackTrace: {ex.StackTrace}");
}
