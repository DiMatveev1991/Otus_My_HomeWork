using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Core.Entities;
using Core.Services;
using Otus.ToDoList.ConsoleBot;
using Otus.ToDoList.ConsoleBot.Types;

namespace TelegramBot
{
	/// <summary>
	/// Обработчик команд бота AutoParts Hub.
	/// Адаптирован под асинхронный IUpdateHandler с CancellationToken
	/// и обязательной реализацией HandleErrorAsync.
	/// </summary>
	public class UpdateHandler : IUpdateHandler
	{
		private readonly IUserService _userService;
		private readonly IToDoService _toDoService;
		private readonly IToDoReportService _toDoReportService;
		private readonly CancellationTokenSource _appCts;

		public UpdateHandler(
			IUserService userService,
			IToDoService toDoService,
			IToDoReportService toDoReportService,
			CancellationTokenSource appCts)
		{
			_userService = userService;
			_toDoService = toDoService;
			_toDoReportService = toDoReportService;
			_appCts = appCts;
		}

		public async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken ct)
		{
			var message = update.Message;
			var chat = message.Chat;
			var from = message.From;
			var text = message.Text?.Trim() ?? string.Empty;

			var parts = text.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
			if (parts.Length == 0)
			{
				await botClient.SendMessage(chat, "Введите команду. Используйте /help для справки.", ct);
				return;
			}

			var command = parts[0].ToLower();
			var argument = parts.Length > 1 ? parts[1].Trim() : string.Empty;

			var currentUser = _userService.GetUser(from.Id);

			// Команды доступные без регистрации
			switch (command)
			{
				case "/help":
					await HandleHelp(botClient, chat, currentUser, ct);
					return;
				case "/info":
					await HandleInfo(botClient, chat, currentUser, ct);
					return;
				case "/start":
					await HandleStart(botClient, chat, from, currentUser, ct);
					return;
			}

			// Остальные команды — только для зарегистрированных
			if (currentUser == null)
			{
				await botClient.SendMessage(chat,
					"Добро пожаловать в AutoParts Hub!\n" +
					"Для начала работы выполните команду /start.\n" +
					"Доступны команды: /help, /info", ct);
				return;
			}

			switch (command)
			{
				case "/showtasks":
					await HandleShowOrders(botClient, chat, currentUser, ct);
					break;
				case "/showalltasks":
					await HandleShowAllOrders(botClient, chat, currentUser, ct);
					break;
				case "/addtask":
					await HandleAddOrder(botClient, chat, currentUser, argument, ct);
					break;
				case "/completetask":
					await HandleCompleteOrder(botClient, chat, currentUser, argument, ct);
					break;
				case "/removetask":
					await HandleRemoveOrder(botClient, chat, currentUser, argument, ct);
					break;
				case "/report":
					await HandleReport(botClient, chat, currentUser, ct);
					break;
				case "/find":
					await HandleFind(botClient, chat, currentUser, argument, ct);
					break;
				case "/exit":
					await HandleExit(botClient, chat, currentUser, ct);
					break;
				default:
					await botClient.SendMessage(chat,
						$"Неизвестная команда \"{text}\".\n" +
						"Введите /help для просмотра доступных команд.", ct);
					break;
			}
		}

		// Метод обязательный по новому контракту IUpdateHandler.
		// Сюда библиотека ConsoleBotClient прокидывает все исключения
		// из HandleUpdateAsync — единая точка обработки ошибок.
		public Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken ct)
		{
			var prevColor = Console.ForegroundColor;
			Console.ForegroundColor = ConsoleColor.Red;
			Console.WriteLine($"HandleError: {exception.GetType().Name}: {exception.Message}");
			Console.ForegroundColor = prevColor;
			return Task.CompletedTask;
		}

		// ── Обработчики команд ───────────────────────────────────────────────

		private async Task HandleStart(ITelegramBotClient botClient, Chat chat,
			User from, ToDoUser? currentUser, CancellationToken ct)
		{
			if (currentUser != null)
			{
				await botClient.SendMessage(chat,
					$"Вы уже зарегистрированы, {currentUser.TelegramUserName}!\n" +
					"Введите /help для просмотра доступных команд.", ct);
				return;
			}

			var userName = from.Username ?? $"Client_{from.Id}";
			var newUser = _userService.RegisterUser(from.Id, userName);

			await botClient.SendMessage(chat,
				$"Добро пожаловать в AutoParts Hub, {newUser.TelegramUserName}!\n" +
				"Вы успешно зарегистрированы. Теперь вы можете создавать заказы на запчасти.\n" +
				$"UserId: {newUser.UserId}\n" +
				"Введите /help для просмотра команд.", ct);
		}

		private async Task HandleHelp(ITelegramBotClient botClient, Chat chat,
			ToDoUser? currentUser, CancellationToken ct)
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

			await botClient.SendMessage(chat, sb.ToString(), ct);
		}

		private async Task HandleInfo(ITelegramBotClient botClient, Chat chat,
			ToDoUser? currentUser, CancellationToken ct)
		{
			var sb = new StringBuilder();
			sb.AppendLine("==================================================");
			sb.AppendLine("  AutoParts Hub Bot v7.0 (async + CancellationToken)");
			sb.AppendLine("  Система управления заказами автозапчастей");
			sb.AppendLine("==================================================");

			if (currentUser != null)
			{
				var all = _toDoService.GetAllByUserId(currentUser.UserId);
				var active = _toDoService.GetActiveByUserId(currentUser.UserId);
				sb.AppendLine($"  Клиент:          {currentUser.TelegramUserName}");
				sb.AppendLine($"  UserId:          {currentUser.UserId}");
				sb.AppendLine($"  Зарегистрирован: {currentUser.RegisteredAt:dd.MM.yyyy HH:mm}");
				sb.AppendLine($"  Заказов всего:   {all.Count}");
				sb.AppendLine($"  Активных:        {active.Count}");
			}
			else
			{
				sb.AppendLine("  Вы не зарегистрированы. Введите /start.");
			}

			sb.AppendLine("  Разработчик: Команда AutoParts Hub");

			await botClient.SendMessage(chat, sb.ToString(), ct);
		}

		private async Task HandleShowOrders(ITelegramBotClient botClient, Chat chat,
			ToDoUser user, CancellationToken ct)
		{
			var orders = _toDoService.GetActiveByUserId(user.UserId);

			if (orders.Count == 0)
			{
				await botClient.SendMessage(chat,
					$"{user.TelegramUserName}, у вас нет активных заказов.\n" +
					"Добавьте заказ командой /addtask <название запчасти>", ct);
				return;
			}

			var sb = new StringBuilder();
			sb.AppendLine($"{user.TelegramUserName}, ваши активные заказы:");
			sb.AppendLine("======================================================================");
			for (int i = 0; i < orders.Count; i++)
				sb.AppendLine($"{i + 1}. {orders[i]}");
			sb.AppendLine("======================================================================");
			sb.AppendLine($"Активных заказов: {orders.Count}");

			await botClient.SendMessage(chat, sb.ToString(), ct);
		}

		private async Task HandleShowAllOrders(ITelegramBotClient botClient, Chat chat,
			ToDoUser user, CancellationToken ct)
		{
			var orders = _toDoService.GetAllByUserId(user.UserId);

			if (orders.Count == 0)
			{
				await botClient.SendMessage(chat,
					$"{user.TelegramUserName}, список заказов пуст.\n" +
					"Добавьте заказ командой /addtask <название запчасти>", ct);
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

			await botClient.SendMessage(chat, sb.ToString(), ct);
		}

		private async Task HandleAddOrder(ITelegramBotClient botClient, Chat chat,
			ToDoUser user, string argument, CancellationToken ct)
		{
			if (string.IsNullOrWhiteSpace(argument))
			{
				await botClient.SendMessage(chat,
					"Укажите название запчасти или описание заказа.\n" +
					"Пример: /addtask Масляный фильтр Toyota Camry 2.5\n" +
					"Пример: /addtask Тормозные колодки передние Honda Accord", ct);
				return;
			}

			var order = _toDoService.Add(user, argument);
			await botClient.SendMessage(chat,
				$"Заказ добавлен!\n" +
				$"Запчасть: {order.Name}\n" +
				$"ID заказа: {order.Id}\n" +
				$"Дата создания: {order.CreatedAt:dd.MM.yyyy HH:mm:ss}", ct);
		}

		private async Task HandleCompleteOrder(ITelegramBotClient botClient, Chat chat,
			ToDoUser user, string argument, CancellationToken ct)
		{
			if (!Guid.TryParse(argument, out var orderId))
			{
				await botClient.SendMessage(chat,
					"Укажите корректный ID заказа в формате GUID.\n" +
					"ID заказа можно найти в списке /showtasks или /showalltasks.\n" +
					"Пример: /completetask 3fa85f64-5717-4562-b3fc-2c963f66afa6", ct);
				return;
			}

			var orders = _toDoService.GetAllByUserId(user.UserId);
			var order = orders.FirstOrDefault(t => t.Id == orderId);

			if (order == null)
			{
				await botClient.SendMessage(chat,
					$"Заказ с ID {orderId} не найден в вашем списке.\n" +
					"Проверьте ID командой /showalltasks", ct);
				return;
			}

			_toDoService.MarkCompleted(orderId);
			await botClient.SendMessage(chat,
				$"Заказ выполнен!\n" +
				$"Запчасть: {order.Name}\n" +
				$"Время выполнения: {DateTime.UtcNow:dd.MM.yyyy HH:mm:ss}", ct);
		}

		private async Task HandleRemoveOrder(ITelegramBotClient botClient, Chat chat,
			ToDoUser user, string argument, CancellationToken ct)
		{
			var allOrders = _toDoService.GetAllByUserId(user.UserId).ToList();

			if (allOrders.Count == 0)
			{
				await botClient.SendMessage(chat,
					$"{user.TelegramUserName}, список заказов пуст. Нечего удалять.", ct);
				return;
			}

			if (!int.TryParse(argument, out var number) ||
				number < 1 || number > allOrders.Count)
			{
				await botClient.SendMessage(chat,
					$"Укажите номер заказа от 1 до {allOrders.Count}.\n" +
					"Пример: /removetask 2\n" +
					"Список заказов: /showalltasks", ct);
				return;
			}

			var order = allOrders[number - 1];
			_toDoService.Delete(order.Id);
			await botClient.SendMessage(chat,
				$"Заказ удалён!\n" +
				$"Запчасть: {order.Name}\n" +
				$"Осталось заказов: {allOrders.Count - 1}", ct);
		}

		// /report — кортеж из IToDoReportService
		private async Task HandleReport(ITelegramBotClient botClient, Chat chat,
			ToDoUser user, CancellationToken ct)
		{
			var stats = _toDoReportService.GetUserStats(user.UserId);
			var (total, completed, active, generatedAt) = stats;

			await botClient.SendMessage(chat,
				$"Статистика по задачам на {generatedAt:dd.MM.yyyy HH:mm:ss}. " +
				$"Всего: {total}; Завершенных: {completed}; Активных: {active};", ct);
		}

		// /find — лямбда через IToDoService.Find
		private async Task HandleFind(ITelegramBotClient botClient, Chat chat,
			ToDoUser user, string argument, CancellationToken ct)
		{
			if (string.IsNullOrWhiteSpace(argument))
			{
				await botClient.SendMessage(chat,
					"Укажите префикс имени для поиска.\n" +
					"Пример: /find Масляный", ct);
				return;
			}

			var found = _toDoService.Find(user, argument);

			if (found.Count == 0)
			{
				await botClient.SendMessage(chat,
					$"{user.TelegramUserName}, заказов, начинающихся на \"{argument}\", не найдено.", ct);
				return;
			}

			var sb = new StringBuilder();
			sb.AppendLine($"{user.TelegramUserName}, найдено заказов: {found.Count}");
			sb.AppendLine("======================================================================");
			for (int i = 0; i < found.Count; i++)
				sb.AppendLine($"{i + 1}. {found[i]}");
			sb.AppendLine("======================================================================");

			await botClient.SendMessage(chat, sb.ToString(), ct);
		}

		// /exit — корректное завершение через CancellationTokenSource
		// вместо Environment.Exit, которое жёстко рубит процесс.
		private async Task HandleExit(ITelegramBotClient botClient, Chat chat,
			ToDoUser user, CancellationToken ct)
		{
			var active = _toDoService.GetActiveByUserId(user.UserId);
			var all = _toDoService.GetAllByUserId(user.UserId);
			await botClient.SendMessage(chat,
				$"До свидания, {user.TelegramUserName}!\n" +
				$"Ваши заказы сохранены. Всего: {all.Count} (активных: {active.Count})\n" +
				"Ждём вас в AutoParts Hub! (нажмите Enter для выхода)", ct);

			_appCts.Cancel();
		}
	}
}