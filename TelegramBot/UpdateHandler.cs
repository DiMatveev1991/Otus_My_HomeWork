using System;
using System.Linq;
using System.Text;
using Core.Entities;
using Core.Services;
using Otus.ToDoList.ConsoleBot;
using Otus.ToDoList.ConsoleBot.Types;

namespace TelegramBot
{
	/// <summary>
	/// Обработчик команд бота магазина автозапчастей AutoParts Hub
	/// </summary>
	public class UpdateHandler : IUpdateHandler
	{
		private readonly IUserService _userService;
		private readonly IToDoService _toDoService;
		private readonly IToDoReportService _toDoReportService;

		public UpdateHandler(
			IUserService userService,
			IToDoService toDoService,
			IToDoReportService toDoReportService)
		{
			_userService = userService;
			_toDoService = toDoService;
			_toDoReportService = toDoReportService;
		}

		public void HandleUpdateAsync(ITelegramBotClient botClient, Update update)
		{
			try
			{
				var message = update.Message;
				var chat = message.Chat;
				var from = message.From;
				var text = message.Text?.Trim() ?? string.Empty;

				var parts = text.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
				if (parts.Length == 0)
				{
					botClient.SendMessage(chat, "Введите команду. Используйте /help для справки.");
					return;
				}

				var command = parts[0].ToLower();
				var argument = parts.Length > 1 ? parts[1].Trim() : string.Empty;

				var currentUser = _userService.GetUser(from.Id);

				// Команды доступные без регистрации
				switch (command)
				{
					case "/help":
						HandleHelp(botClient, chat, currentUser);
						return;
					case "/info":
						HandleInfo(botClient, chat, currentUser);
						return;
					case "/start":
						HandleStart(botClient, chat, from, currentUser);
						return;
				}

				// Остальные команды только для зарегистрированных
				if (currentUser == null)
				{
					botClient.SendMessage(chat,
						"Добро пожаловать в AutoParts Hub!\n" +
						"Для начала работы выполните команду /start.\n" +
						"Доступны команды: /help, /info");
					return;
				}

				switch (command)
				{
					case "/showtasks":
						HandleShowOrders(botClient, chat, currentUser);
						break;
					case "/showalltasks":
						HandleShowAllOrders(botClient, chat, currentUser);
						break;
					case "/addtask":
						HandleAddOrder(botClient, chat, currentUser, argument);
						break;
					case "/completetask":
						HandleCompleteOrder(botClient, chat, currentUser, argument);
						break;
					case "/removetask":
						HandleRemoveOrder(botClient, chat, currentUser, argument);
						break;
					case "/report":
						HandleReport(botClient, chat, currentUser);
						break;
					case "/find":
						HandleFind(botClient, chat, currentUser, argument);
						break;
					case "/exit":
						HandleExit(botClient, chat, currentUser);
						break;
					default:
						botClient.SendMessage(chat,
							$"Неизвестная команда \"{text}\".\n" +
							"Введите /help для просмотра доступных команд.");
						break;
				}
			}
			catch (Exception ex)
			{
				botClient.SendMessage(update.Message.Chat, $"Ошибка: {ex.Message}");
			}
		}

		// ── Обработчики команд ───────────────────────────────────────────────

		private void HandleStart(ITelegramBotClient botClient, Chat chat,
			User from, ToDoUser? currentUser)
		{
			if (currentUser != null)
			{
				botClient.SendMessage(chat,
					$"Вы уже зарегистрированы, {currentUser.TelegramUserName}!\n" +
					"Введите /help для просмотра доступных команд.");
				return;
			}

			var userName = from.Username ?? $"Client_{from.Id}";
			var newUser = _userService.RegisterUser(from.Id, userName);

			botClient.SendMessage(chat,
				$"Добро пожаловать в AutoParts Hub, {newUser.TelegramUserName}!\n" +
				"Вы успешно зарегистрированы. Теперь вы можете создавать заказы на запчасти.\n" +
				$"UserId: {newUser.UserId}\n" +
				"Введите /help для просмотра команд.");
		}

		private void HandleHelp(ITelegramBotClient botClient, Chat chat, ToDoUser? currentUser)
		{
			var sb = new StringBuilder();

			if (currentUser != null)
				sb.AppendLine($"{currentUser.TelegramUserName}, доступные команды AutoParts Hub:");
			else
				sb.AppendLine("AutoParts Hub — бот для заказа автозапчастей.\nДоступные команды:");

			sb.AppendLine();
			sb.AppendLine("/start                    - Регистрация в системе");
			sb.AppendLine("/help                     - Справка по командам");
			sb.AppendLine("/info                     - Информация о программе и вашем аккаунте");
			sb.AppendLine("/addtask <запчасть>       - Добавить заказ. Пример:");
			sb.AppendLine("                            /addtask Масляный фильтр Toyota Camry 2.5");
			sb.AppendLine("/showtasks                - Показать активные заказы");
			sb.AppendLine("/showalltasks             - Показать все заказы (включая выполненные)");
			sb.AppendLine("/completetask <id>        - Отметить заказ выполненным по GUID.");
			sb.AppendLine("                            Пример: /completetask 3fa85f64-5717-...");
			sb.AppendLine("/removetask <номер>       - Удалить заказ по номеру.");
			sb.AppendLine("                            Пример: /removetask 2");
			sb.AppendLine("/report                   - Статистика по вашим заказам");
			sb.AppendLine("                            (всего / выполненных / активных)");
			sb.AppendLine("/find <префикс>           - Поиск заказов по началу названия.");
			sb.AppendLine("                            Пример: /find Масляный");
			sb.AppendLine("/exit                     - Выйти из программы");

			botClient.SendMessage(chat, sb.ToString());
		}

		private void HandleInfo(ITelegramBotClient botClient, Chat chat, ToDoUser? currentUser)
		{
			var sb = new StringBuilder();
			sb.AppendLine("==================================================");
			sb.AppendLine("  AutoParts Hub Bot v6.0 (репозитории + лямбды)");
			sb.AppendLine("  Система управления заказами автозапчастей");
			sb.AppendLine("==================================================");

			if (currentUser != null)
			{
				var all = _toDoService.GetAllByUserId(currentUser.UserId);
				var active = _toDoService.GetActiveByUserId(currentUser.UserId);
				sb.AppendLine($"  Клиент:        {currentUser.TelegramUserName}");
				sb.AppendLine($"  UserId:        {currentUser.UserId}");
				sb.AppendLine($"  Зарегистрирован: {currentUser.RegisteredAt:dd.MM.yyyy HH:mm}");
				sb.AppendLine($"  Заказов всего: {all.Count}");
				sb.AppendLine($"  Активных:      {active.Count}");
			}
			else
			{
				sb.AppendLine("  Вы не зарегистрированы. Введите /start.");
			}

			sb.AppendLine("  Разработчик: Команда AutoParts Hub");

			botClient.SendMessage(chat, sb.ToString());
		}

		private void HandleShowOrders(ITelegramBotClient botClient, Chat chat, ToDoUser user)
		{
			var orders = _toDoService.GetActiveByUserId(user.UserId);

			if (orders.Count == 0)
			{
				botClient.SendMessage(chat,
					$"{user.TelegramUserName}, у вас нет активных заказов.\n" +
					"Добавьте заказ командой /addtask <название запчасти>");
				return;
			}

			var sb = new StringBuilder();
			sb.AppendLine($"{user.TelegramUserName}, ваши активные заказы:");
			sb.AppendLine("======================================================================");
			for (int i = 0; i < orders.Count; i++)
				sb.AppendLine($"{i + 1}. {orders[i]}");
			sb.AppendLine("======================================================================");
			sb.AppendLine($"Активных заказов: {orders.Count}");

			botClient.SendMessage(chat, sb.ToString());
		}

		private void HandleShowAllOrders(ITelegramBotClient botClient, Chat chat, ToDoUser user)
		{
			var orders = _toDoService.GetAllByUserId(user.UserId);

			if (orders.Count == 0)
			{
				botClient.SendMessage(chat,
					$"{user.TelegramUserName}, список заказов пуст.\n" +
					"Добавьте заказ командой /addtask <название запчасти>");
				return;
			}

			var active = _toDoService.GetActiveByUserId(user.UserId);
			var sb = new StringBuilder();
			sb.AppendLine($"{user.TelegramUserName}, все ваши заказы:");
			sb.AppendLine("======================================================================");
			for (int i = 0; i < orders.Count; i++)
				sb.AppendLine($"{i + 1}. {orders[i].ToStringWithState()}");
			sb.AppendLine("======================================================================");
			sb.AppendLine($"Всего заказов: {orders.Count} (активных: {active.Count}, выполненных: {orders.Count - active.Count})");

			botClient.SendMessage(chat, sb.ToString());
		}

		private void HandleAddOrder(ITelegramBotClient botClient, Chat chat,
			ToDoUser user, string argument)
		{
			if (string.IsNullOrWhiteSpace(argument))
			{
				botClient.SendMessage(chat,
					"Укажите название запчасти или описание заказа.\n" +
					"Пример: /addtask Масляный фильтр Toyota Camry 2.5\n" +
					"Пример: /addtask Тормозные колодки передние Honda Accord");
				return;
			}

			var order = _toDoService.Add(user, argument);
			botClient.SendMessage(chat,
				$"Заказ добавлен!\n" +
				$"Запчасть: {order.Name}\n" +
				$"ID заказа: {order.Id}\n" +
				$"Дата создания: {order.CreatedAt:dd.MM.yyyy HH:mm:ss}");
		}

		private void HandleCompleteOrder(ITelegramBotClient botClient, Chat chat,
			ToDoUser user, string argument)
		{
			if (!Guid.TryParse(argument, out var orderId))
			{
				botClient.SendMessage(chat,
					"Укажите корректный ID заказа в формате GUID.\n" +
					"ID заказа можно найти в списке /showtasks или /showalltasks.\n" +
					"Пример: /completetask 3fa85f64-5717-4562-b3fc-2c963f66afa6");
				return;
			}

			var orders = _toDoService.GetAllByUserId(user.UserId);
			var order = orders.FirstOrDefault(t => t.Id == orderId);

			if (order == null)
			{
				botClient.SendMessage(chat,
					$"Заказ с ID {orderId} не найден в вашем списке.\n" +
					"Проверьте ID командой /showalltasks");
				return;
			}

			_toDoService.MarkCompleted(orderId);
			botClient.SendMessage(chat,
				$"Заказ выполнен!\n" +
				$"Запчасть: {order.Name}\n" +
				$"Время выполнения: {DateTime.UtcNow:dd.MM.yyyy HH:mm:ss}");
		}

		private void HandleRemoveOrder(ITelegramBotClient botClient, Chat chat,
			ToDoUser user, string argument)
		{
			var allOrders = _toDoService.GetAllByUserId(user.UserId).ToList();

			if (allOrders.Count == 0)
			{
				botClient.SendMessage(chat,
					$"{user.TelegramUserName}, список заказов пуст. Нечего удалять.");
				return;
			}

			if (!int.TryParse(argument, out var number) ||
				number < 1 || number > allOrders.Count)
			{
				botClient.SendMessage(chat,
					$"Укажите номер заказа от 1 до {allOrders.Count}.\n" +
					"Пример: /removetask 2\n" +
					"Список заказов: /showalltasks");
				return;
			}

			var order = allOrders[number - 1];
			_toDoService.Delete(order.Id);
			botClient.SendMessage(chat,
				$"Заказ удалён!\n" +
				$"Запчасть: {order.Name}\n" +
				$"Осталось заказов: {allOrders.Count - 1}");
		}

		// Команда /report — использует кортеж из IToDoReportService
		private void HandleReport(ITelegramBotClient botClient, Chat chat, ToDoUser user)
		{
			var stats = _toDoReportService.GetUserStats(user.UserId);

			// Деконструкция кортежа
			var (total, completed, active, generatedAt) = stats;

			botClient.SendMessage(chat,
				$"Статистика по задачам на {generatedAt:dd.MM.yyyy HH:mm:ss}. " +
				$"Всего: {total}; Завершенных: {completed}; Активных: {active};");
		}

		// Команда /find — использует лямбду через IToDoService.Find
		private void HandleFind(ITelegramBotClient botClient, Chat chat,
			ToDoUser user, string argument)
		{
			if (string.IsNullOrWhiteSpace(argument))
			{
				botClient.SendMessage(chat,
					"Укажите префикс имени для поиска.\n" +
					"Пример: /find Масляный");
				return;
			}

			var found = _toDoService.Find(user, argument);

			if (found.Count == 0)
			{
				botClient.SendMessage(chat,
					$"{user.TelegramUserName}, заказов, начинающихся на \"{argument}\", не найдено.");
				return;
			}

			// Вывод как в /showtasks
			var sb = new StringBuilder();
			sb.AppendLine($"{user.TelegramUserName}, найдено заказов: {found.Count}");
			sb.AppendLine("======================================================================");
			for (int i = 0; i < found.Count; i++)
				sb.AppendLine($"{i + 1}. {found[i]}");
			sb.AppendLine("======================================================================");

			botClient.SendMessage(chat, sb.ToString());
		}

		private void HandleExit(ITelegramBotClient botClient, Chat chat, ToDoUser user)
		{
			var active = _toDoService.GetActiveByUserId(user.UserId);
			var all = _toDoService.GetAllByUserId(user.UserId);
			botClient.SendMessage(chat,
				$"До свидания, {user.TelegramUserName}!\n" +
				$"Ваши заказы сохранены. Всего: {all.Count} (активных: {active.Count})\n" +
				"Ждём вас в AutoParts Hub!");
			Environment.Exit(0);
		}
	}
}
