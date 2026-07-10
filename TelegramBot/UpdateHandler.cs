using System;
using System.Collections.Generic;
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
using TelegramBot.Dto;
using TelegramBot.Scenarios;

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
		private readonly IToDoListService _toDoListService;
		private readonly IToDoReportService _toDoReportService;
		private readonly IEnumerable<IScenario> _scenarios;
		private readonly IScenarioContextRepository _contextRepository;
		private readonly CancellationTokenSource _appCts;

		public UpdateHandler(
			IUserService userService,
			IToDoService toDoService,
			IToDoListService toDoListService,
			IToDoReportService toDoReportService,
			IEnumerable<IScenario> scenarios,
			IScenarioContextRepository contextRepository,
			CancellationTokenSource appCts)
		{
			_userService = userService;
			_toDoService = toDoService;
			_toDoListService = toDoListService;
			_toDoReportService = toDoReportService;
			_scenarios = scenarios;
			_contextRepository = contextRepository;
			_appCts = appCts;
		}

		// === IUpdateHandler ===================================================

		public async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken ct)
		{
			await (update switch
			{
				{ Message: { } message } => OnMessage(botClient, update, message, ct),
				{ CallbackQuery: { } callbackQuery } => OnCallbackQuery(botClient, update, callbackQuery, ct),
				_ => OnUnknown(update)
			});
		}

		// В Telegram.Bot v22 у HandleErrorAsync 4 параметра.
		public Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception,
			HandleErrorSource source, CancellationToken ct)
		{
			var prevColor = Console.ForegroundColor;
			Console.ForegroundColor = ConsoleColor.Red;
			Console.WriteLine($"HandleError [{source}]: {exception.GetType().Name}: {exception.Message}");
			Console.ForegroundColor = prevColor;
			return Task.CompletedTask;
		}

		private static Task OnUnknown(Update update) => Task.CompletedTask;

		// === Обработка текстовых сообщений ===================================

		private async Task OnMessage(ITelegramBotClient botClient, Update update, Message message, CancellationToken ct)
		{
			if (message.Text is null) return;

			var chat = message.Chat;
			var from = message.From;
			if (from is null) return;

			var text = message.Text.Trim();
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

			// /cancel — прерывает активный сценарий. Обрабатывается ДО запуска ProcessScenario.
			if (command == "/cancel")
			{
				await _contextRepository.ResetContext(from.Id, ct);
				await SendAsync(botClient, chat.Id,
					currentUser is null ? KeyboardFactory.PreRegistration : KeyboardFactory.PostRegistration,
					"Действие отменено. Возвращаю к списку команд.", ct);
				return;
			}

			// Если сценарий активен — передаём обновление ему и завершаем обработку.
			var activeContext = await _contextRepository.GetContext(from.Id, ct);
			if (activeContext != null)
			{
				await ProcessScenario(botClient, activeContext, update, ct);
				return;
			}

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
				case "/show":
					await HandleShow(botClient, chat, currentUser, ct);
					break;
				case "/addtask":
					// Запуск сценария создания задачи с сохранением состояния
					var addTaskContext = new ScenarioContext(ScenarioType.AddTask);
					await ProcessScenario(botClient, addTaskContext, update, ct);
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

		// === Обработка нажатий на Inline-кнопки ==============================

		private async Task OnCallbackQuery(ITelegramBotClient botClient, Update update,
			CallbackQuery query, CancellationToken ct)
		{
			// Убираем "часики" на кнопке
			await botClient.AnswerCallbackQuery(query.Id, cancellationToken: ct);

			var from = query.From;

			// Незарегистрированным пользователям CallbackQuery не обрабатываем
			var currentUser = await _userService.GetUserAsync(from.Id, ct);
			if (currentUser is null) return;

			if (query.Data is null || query.Message is null) return;

			// Если сценарий активен — передаём обновление ему.
			var activeContext = await _contextRepository.GetContext(from.Id, ct);
			if (activeContext != null)
			{
				await ProcessScenario(botClient, activeContext, update, ct);
				return;
			}

			var callback = CallbackDto.FromString(query.Data);

			switch (callback.Action)
			{
				case "show":
				{
					var listCallback = ToDoListCallbackDto.FromString(query.Data);
					await HandleShowList(botClient, query.Message.Chat, currentUser,
						listCallback.ToDoListId, ct);
					break;
				}
				case "addlist":
				{
					var context = new ScenarioContext(ScenarioType.AddList);
					await ProcessScenario(botClient, context, update, ct);
					break;
				}
				case "deletelist":
				{
					var context = new ScenarioContext(ScenarioType.DeleteList);
					await ProcessScenario(botClient, context, update, ct);
					break;
				}
			}
		}

		// === Сценарии ========================================================

		private IScenario GetScenario(ScenarioType scenario)
		{
			return _scenarios.FirstOrDefault(s => s.CanHandle(scenario))
				?? throw new InvalidOperationException($"Сценарий {scenario} не найден.");
		}

		// Обрабатывает один шаг сценария и сохраняет/сбрасывает его состояние.
		private async Task ProcessScenario(ITelegramBotClient bot, ScenarioContext context,
			Update update, CancellationToken ct)
		{
			var scenario = GetScenario(context.CurrentScenario);
			var result = await scenario.HandleMessageAsync(bot, context, update, ct);

			var userId = update.Message?.From?.Id ?? update.CallbackQuery!.From.Id;

			if (result == ScenarioResult.Completed)
				await _contextRepository.ResetContext(userId, ct);
			else
				await _contextRepository.SetContext(userId, context, ct);
		}

		// === Вспомогательные методы ==========================================

		private async Task<ReplyKeyboardMarkup> KeyboardForAsync(long telegramUserId, CancellationToken ct)
		{
			var user = await _userService.GetUserAsync(telegramUserId, ct);
			return user is null ? KeyboardFactory.PreRegistration : KeyboardFactory.PostRegistration;
		}

		private static Task<Message> SendAsync(ITelegramBotClient bot, ChatId chatId,
			ReplyKeyboardMarkup keyboard, string text, CancellationToken ct)
		{
			return bot.SendMessage(
				chatId: chatId,
				text: text,
				replyMarkup: keyboard,
				cancellationToken: ct);
		}

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
			sb.AppendLine("/addtask                  - Добавить заказ (пошаговый сценарий:");
			sb.AppendLine("                            название, дедлайн dd.MM.yyyy, выбор списка)");
			sb.AppendLine("/cancel                   - Отменить текущий сценарий");
			sb.AppendLine("/show                     - Показать списки и задачи. Выбор списка");
			sb.AppendLine("                            кнопками; ниже — добавить/удалить список");
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
			sb.AppendLine("  AutoParts Hub Bot v9.0 (Telegram.Bot)");
			sb.AppendLine("  Система управления заказами автозапчастей");
			sb.AppendLine("==================================================");

			ReplyKeyboardMarkup keyboard;
			if (currentUser != null)
			{
				var all = await _toDoService.GetAllByUserIdAsync(currentUser.UserId, ct);
				var active = await _toDoService.GetActiveByUserIdAsync(currentUser.UserId, ct);
				var lists = await _toDoListService.GetUserLists(currentUser.UserId, ct);
				sb.AppendLine($"  Клиент:          {currentUser.TelegramUserName}");
				sb.AppendLine($"  UserId:          {currentUser.UserId}");
				sb.AppendLine($"  Зарегистрирован: {currentUser.RegisteredAt:dd.MM.yyyy HH:mm}");
				sb.AppendLine($"  Заказов всего:   {all.Count}");
				sb.AppendLine($"  Активных:        {active.Count}");
				sb.AppendLine($"  Списков:         {lists.Count}");
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

		// /show — отправляет сообщение "Выберите список" с Inline-кнопками списков.
		private async Task HandleShow(ITelegramBotClient bot, Chat chat,
			ToDoUser user, CancellationToken ct)
		{
			var lists = await _toDoListService.GetUserLists(user.UserId, ct);

			await bot.SendMessage(chat.Id, "Выберите список",
				replyMarkup: KeyboardFactory.ListsInline(lists, "show",
					includeNoList: true, includeManageButtons: true),
				cancellationToken: ct);
		}

		// Показ задач выбранного списка (или задач без списка при listId == null).
		private async Task HandleShowList(ITelegramBotClient bot, Chat chat,
			ToDoUser user, Guid? listId, CancellationToken ct)
		{
			var orders = await _toDoService.GetByUserIdAndList(user.UserId, listId, ct);

			var listName = "📌Без списка";
			if (listId is { } id)
			{
				var list = await _toDoListService.Get(id, ct);
				listName = list?.Name ?? "Список";
			}

			if (orders.Count == 0)
			{
				await SendAsync(bot, chat.Id, KeyboardFactory.PostRegistration,
					$"Список \"{listName}\": задач нет.", ct);
				return;
			}

			var sb = new StringBuilder();
			sb.AppendLine($"Список \"{listName}\":");
			sb.AppendLine("======================================================================");
			for (int i = 0; i < orders.Count; i++)
			{
				var o = orders[i];
				var stateText = o.State == Core.Enums.ToDoItemState.Active ? "(Active)" : "(Completed)";
				sb.AppendLine(
					$"{i + 1}. {stateText} {o.Name} — {o.CreatedAt:dd.MM.yyyy HH:mm:ss} — `{o.Id}`");
			}
			sb.AppendLine("======================================================================");
			sb.AppendLine($"Всего задач: {orders.Count}");

			await SendMarkdownAsync(bot, chat.Id, KeyboardFactory.PostRegistration, sb.ToString(), ct);
		}

		private async Task HandleCompleteOrder(ITelegramBotClient bot, Chat chat,
			ToDoUser user, string argument, CancellationToken ct)
		{
			if (!Guid.TryParse(argument, out var orderId))
			{
				await SendAsync(bot, chat.Id, KeyboardFactory.PostRegistration,
					"Укажите корректный ID заказа в формате GUID.\n" +
					"ID заказа можно найти в списке /show.\n" +
					"Пример: /completetask 3fa85f64-5717-4562-b3fc-2c963f66afa6", ct);
				return;
			}

			var orders = await _toDoService.GetAllByUserIdAsync(user.UserId, ct);
			var order = orders.FirstOrDefault(t => t.Id == orderId);

			if (order == null)
			{
				await SendAsync(bot, chat.Id, KeyboardFactory.PostRegistration,
					$"Заказ с ID {orderId} не найден в вашем списке.\n" +
					"Проверьте ID командой /show", ct);
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
					"Список заказов: /show", ct);
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
