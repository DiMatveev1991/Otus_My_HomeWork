using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Core.Entities;
using Core.Services;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace TelegramBot
{
	/// <summary>
	/// Обработчик команд бота AutoParts Hub.
	/// Реализует IUpdateHandler из Telegram.Bot.
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

		// === IUpdateHandler ===================================================

		public async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken ct)
		{
			// Интересуют только текстовые сообщения
			if (update.Message is not { Text: { } } message)
				return;

			var chat = message.Chat;
			var from = message.From;
			if (from is null) return;

			var text = message.Text!.Trim();
			var parts = text.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
			if (parts.Length == 0)
			{
				await SendAsync(botClient, chat.Id, await KeyboardForAsync(from.Id, ct),
					"Введите команду. Используйте /help для справки.", ct);
				return;
			}

			var command = parts[0].ToLower();
			var argument = parts.Length > 1 ? parts[1].Trim() : string.Empty;

			var currentUser = await _userService.GetUserAsync(from.Id, ct);

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
				await SendAsync(botClient, chat.Id, KeyboardFactory.PreRegistration,
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
					await SendAsync(botClient, chat.Id, KeyboardFactory.PostRegistration,
						$"Неизвестная команда \"{text}\".\n" +
						"Введите /help для просмотра доступных команд.", ct);
					break;
			}
		}

		// В Telegram.Bot v22 у HandleErrorAsync 4 параметра.
		// HandleErrorSource показывает, откуда прилетела ошибка
		// (polling-цикл или наш собственный код в HandleUpdateAsync).
		public Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception,
			HandleErrorSource source, CancellationToken ct)
		{
			var prevColor = Console.ForegroundColor;
			Console.ForegroundColor = ConsoleColor.Red;
			Console.WriteLine($"HandleError [{source}]: {exception.GetType().Name}: {exception.Message}");
			Console.ForegroundColor = prevColor;
			return Task.CompletedTask;
		}

		// === Вспомогательные методы ==========================================

		// Выбирает клавиатуру в зависимости от статуса регистрации
		private async Task<ReplyKeyboardMarkup> KeyboardForAsync(long telegramUserId, CancellationToken ct)
		{
			var user = await _userService.GetUserAsync(telegramUserId, ct);
			return user is null ? KeyboardFactory.PreRegistration : KeyboardFactory.PostRegistration;
		}

		// Унифицированная отправка с клавиатурой (без Markdown)
		private static Task<Message> SendAsync(ITelegramBotClient bot, ChatId chatId,
			ReplyKeyboardMarkup keyboard, string text, CancellationToken ct)
		{
			return bot.SendMessage(
				chatId: chatId,
				text: text,
				replyMarkup: keyboard,
				cancellationToken: ct);
		}

		// Отправка с Markdown (для оборачивания Id в обратные кавычки)
		private static Task<Message> SendMarkdownAsync(ITelegramBotClient bot, ChatId chatId,
			ReplyKeyboardMarkup keyboard, string text, CancellationToken ct)
		{
			return bot.SendMessage(
				chatId: chatId,
				text: text,
				parseMode: ParseMode.Markdown,
				replyMarkup: keyboard,
				cancellationToken: ct);
		}

		// === Обработчики команд ==============================================

		private async Task HandleStart(ITelegramBotClient bot, Chat chat,
			User from, ToDoUser? currentUser, CancellationToken ct)
		{
			if (currentUser != null)
			{
				await SendAsync(bot, chat.Id, KeyboardFactory.PostRegistration,
					$"Вы уже зарегистрированы, {currentUser.TelegramUserName}!\n" +
					"Введите /help для просмотра доступных команд.", ct);
				return;
			}

			var userName = from.Username ?? $"Client_{from.Id}";
			var newUser = await _userService.RegisterUserAsync(from.Id, userName, ct);

			await SendAsync(bot, chat.Id, KeyboardFactory.PostRegistration,
				$"Добро пожаловать в AutoParts Hub, {newUser.TelegramUserName}!\n" +
				"Вы успешно зарегистрированы. Теперь вы можете создавать заказы на запчасти.\n" +
				$"UserId: {newUser.UserId}\n" +
				"Введите /help для просмотра команд.", ct);
		}

		private async Task HandleHelp(ITelegramBotClient bot, Chat chat,
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
			sb.AppendLine("/exit                     - Остановить бота");

			var keyboard = currentUser is null
				? KeyboardFactory.PreRegistration
				: KeyboardFactory.PostRegistration;

			await SendAsync(bot, chat.Id, keyboard, sb.ToString(), ct);
		}

		private async Task HandleInfo(ITelegramBotClient bot, Chat chat,
			ToDoUser? currentUser, CancellationToken ct)
		{
			var sb = new StringBuilder();
			sb.AppendLine("==================================================");
			sb.AppendLine("  AutoParts Hub Bot v8.0 (Telegram.Bot)");
			sb.AppendLine("  Система управления заказами автозапчастей");
			sb.AppendLine("==================================================");

			ReplyKeyboardMarkup keyboard;
			if (currentUser != null)
			{
				var all = await _toDoService.GetAllByUserIdAsync(currentUser.UserId, ct);
				var active = await _toDoService.GetActiveByUserIdAsync(currentUser.UserId, ct);
				sb.AppendLine($"  Клиент:          {currentUser.TelegramUserName}");
				sb.AppendLine($"  UserId:          {currentUser.UserId}");
				sb.AppendLine($"  Зарегистрирован: {currentUser.RegisteredAt:dd.MM.yyyy HH:mm}");
				sb.AppendLine($"  Заказов всего:   {all.Count}");
				sb.AppendLine($"  Активных:        {active.Count}");
				keyboard = KeyboardFactory.PostRegistration;
			}
			else
			{
				sb.AppendLine("  Вы не зарегистрированы. Введите /start.");
				keyboard = KeyboardFactory.PreRegistration;
			}

			sb.AppendLine("  Разработчик: Команда AutoParts Hub");

			await SendAsync(bot, chat.Id, keyboard, sb.ToString(), ct);
		}

		private async Task HandleShowOrders(ITelegramBotClient bot, Chat chat,
			ToDoUser user, CancellationToken ct)
		{
			var orders = await _toDoService.GetActiveByUserIdAsync(user.UserId, ct);

			if (orders.Count == 0)
			{
				await SendAsync(bot, chat.Id, KeyboardFactory.PostRegistration,
					$"{user.TelegramUserName}, у вас нет активных заказов.\n" +
					"Добавьте заказ командой /addtask <название запчасти>", ct);
				return;
			}

			// Markdown: Id оборачиваем в обратные кавычки `…`,
			// чтобы Telegram отобразил их как inline-code и одним тапом копировал
			var sb = new StringBuilder();
			sb.AppendLine($"{user.TelegramUserName}, ваши активные заказы:");
			sb.AppendLine("======================================================================");
			for (int i = 0; i < orders.Count; i++)
			{
				var o = orders[i];
				sb.AppendLine(
					$"{i + 1}. {o.Name} — {o.CreatedAt:dd.MM.yyyy HH:mm:ss} — `{o.Id}`");
			}
			sb.AppendLine("======================================================================");
			sb.AppendLine($"Активных заказов: {orders.Count}");

			await SendMarkdownAsync(bot, chat.Id, KeyboardFactory.PostRegistration, sb.ToString(), ct);
		}

		private async Task HandleShowAllOrders(ITelegramBotClient bot, Chat chat,
			ToDoUser user, CancellationToken ct)
		{
			var orders = await _toDoService.GetAllByUserIdAsync(user.UserId, ct);

			if (orders.Count == 0)
			{
				await SendAsync(bot, chat.Id, KeyboardFactory.PostRegistration,
					$"{user.TelegramUserName}, список заказов пуст.\n" +
					"Добавьте заказ командой /addtask <название запчасти>", ct);
				return;
			}

			var active = await _toDoService.GetActiveByUserIdAsync(user.UserId, ct);
			var sb = new StringBuilder();
			sb.AppendLine($"{user.TelegramUserName}, все ваши заказы:");
			sb.AppendLine("======================================================================");
			for (int i = 0; i < orders.Count; i++)
			{
				var o = orders[i];
				var stateText = o.State == Core.Enums.ToDoItemState.Active ? "(Active)" : "(Completed)";
				var stateChangedText = o.StateChangedAt.HasValue
					? $" | Изменено: {o.StateChangedAt.Value:dd.MM.yyyy HH:mm:ss}"
					: "";

				// Id в обратных кавычках, остальной текст в обычном виде
				sb.AppendLine(
					$"{i + 1}. {stateText} {o.Name} — {o.CreatedAt:dd.MM.yyyy HH:mm:ss} — `{o.Id}`{stateChangedText}");
			}
			sb.AppendLine("======================================================================");
			sb.AppendLine($"Всего заказов: {orders.Count} (активных: {active.Count}, выполненных: {orders.Count - active.Count})");

			await SendMarkdownAsync(bot, chat.Id, KeyboardFactory.PostRegistration, sb.ToString(), ct);
		}

		private async Task HandleAddOrder(ITelegramBotClient bot, Chat chat,
			ToDoUser user, string argument, CancellationToken ct)
		{
			if (string.IsNullOrWhiteSpace(argument))
			{
				await SendAsync(bot, chat.Id, KeyboardFactory.PostRegistration,
					"Укажите название запчасти или описание заказа.\n" +
					"Пример: /addtask Масляный фильтр Toyota Camry 2.5\n" +
					"Пример: /addtask Тормозные колодки передние Honda Accord", ct);
				return;
			}

			var order = await _toDoService.AddAsync(user, argument, ct);
			await SendAsync(bot, chat.Id, KeyboardFactory.PostRegistration,
				$"Заказ добавлен!\n" +
				$"Запчасть: {order.Name}\n" +
				$"ID заказа: {order.Id}\n" +
				$"Дата создания: {order.CreatedAt:dd.MM.yyyy HH:mm:ss}", ct);
		}

		private async Task HandleCompleteOrder(ITelegramBotClient bot, Chat chat,
			ToDoUser user, string argument, CancellationToken ct)
		{
			if (!Guid.TryParse(argument, out var orderId))
			{
				await SendAsync(bot, chat.Id, KeyboardFactory.PostRegistration,
					"Укажите корректный ID заказа в формате GUID.\n" +
					"ID заказа можно найти в списке /showtasks или /showalltasks.\n" +
					"Пример: /completetask 3fa85f64-5717-4562-b3fc-2c963f66afa6", ct);
				return;
			}

			var orders = await _toDoService.GetAllByUserIdAsync(user.UserId, ct);
			var order = orders.FirstOrDefault(t => t.Id == orderId);

			if (order == null)
			{
				await SendAsync(bot, chat.Id, KeyboardFactory.PostRegistration,
					$"Заказ с ID {orderId} не найден в вашем списке.\n" +
					"Проверьте ID командой /showalltasks", ct);
				return;
			}

			await _toDoService.MarkCompletedAsync(orderId, ct);
			await SendAsync(bot, chat.Id, KeyboardFactory.PostRegistration,
				$"Заказ выполнен!\n" +
				$"Запчасть: {order.Name}\n" +
				$"Время выполнения: {DateTime.UtcNow:dd.MM.yyyy HH:mm:ss}", ct);
		}

		private async Task HandleRemoveOrder(ITelegramBotClient bot, Chat chat,
			ToDoUser user, string argument, CancellationToken ct)
		{
			var allOrders = (await _toDoService.GetAllByUserIdAsync(user.UserId, ct)).ToList();

			if (allOrders.Count == 0)
			{
				await SendAsync(bot, chat.Id, KeyboardFactory.PostRegistration,
					$"{user.TelegramUserName}, список заказов пуст. Нечего удалять.", ct);
				return;
			}

			if (!int.TryParse(argument, out var number) ||
				number < 1 || number > allOrders.Count)
			{
				await SendAsync(bot, chat.Id, KeyboardFactory.PostRegistration,
					$"Укажите номер заказа от 1 до {allOrders.Count}.\n" +
					"Пример: /removetask 2\n" +
					"Список заказов: /showalltasks", ct);
				return;
			}

			var order = allOrders[number - 1];
			await _toDoService.DeleteAsync(order.Id, ct);
			await SendAsync(bot, chat.Id, KeyboardFactory.PostRegistration,
				$"Заказ удалён!\n" +
				$"Запчасть: {order.Name}\n" +
				$"Осталось заказов: {allOrders.Count - 1}", ct);
		}

		private async Task HandleReport(ITelegramBotClient bot, Chat chat,
			ToDoUser user, CancellationToken ct)
		{
			var stats = await _toDoReportService.GetUserStatsAsync(user.UserId, ct);
			var (total, completed, active, generatedAt) = stats;

			await SendAsync(bot, chat.Id, KeyboardFactory.PostRegistration,
				$"Статистика по задачам на {generatedAt:dd.MM.yyyy HH:mm:ss}. " +
				$"Всего: {total}; Завершенных: {completed}; Активных: {active};", ct);
		}

		private async Task HandleFind(ITelegramBotClient bot, Chat chat,
			ToDoUser user, string argument, CancellationToken ct)
		{
			if (string.IsNullOrWhiteSpace(argument))
			{
				await SendAsync(bot, chat.Id, KeyboardFactory.PostRegistration,
					"Укажите префикс имени для поиска.\n" +
					"Пример: /find Масляный", ct);
				return;
			}

			var found = await _toDoService.FindAsync(user, argument, ct);

			if (found.Count == 0)
			{
				await SendAsync(bot, chat.Id, KeyboardFactory.PostRegistration,
					$"{user.TelegramUserName}, заказов, начинающихся на \"{argument}\", не найдено.", ct);
				return;
			}

			var sb = new StringBuilder();
			sb.AppendLine($"{user.TelegramUserName}, найдено заказов: {found.Count}");
			sb.AppendLine("======================================================================");
			for (int i = 0; i < found.Count; i++)
			{
				var o = found[i];
				sb.AppendLine($"{i + 1}. {o.Name} — {o.CreatedAt:dd.MM.yyyy HH:mm:ss} — `{o.Id}`");
			}
			sb.AppendLine("======================================================================");

			await SendMarkdownAsync(bot, chat.Id, KeyboardFactory.PostRegistration, sb.ToString(), ct);
		}

		private async Task HandleExit(ITelegramBotClient bot, Chat chat,
			ToDoUser user, CancellationToken ct)
		{
			var active = await _toDoService.GetActiveByUserIdAsync(user.UserId, ct);
			var all = await _toDoService.GetAllByUserIdAsync(user.UserId, ct);
			await SendAsync(bot, chat.Id, KeyboardFactory.PostRegistration,
				$"До свидания, {user.TelegramUserName}!\n" +
				$"Ваши заказы сохранены. Всего: {all.Count} (активных: {active.Count})\n" +
				"Бот будет остановлен.", ct);

			_appCts.Cancel();
		}
	}
}
