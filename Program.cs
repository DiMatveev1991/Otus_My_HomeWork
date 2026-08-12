using System;
using System.Threading;
using System.Threading.Tasks;
using BackgroundTasks;
using Core.DataAccess;
using Core.Services;
using Infrastructure.DataAccess;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using TelegramBot;
using TelegramBot.Scenarios;

try
{
	// 1. Токен — только из переменной окружения, никаких хардкодов
	var token = Environment.GetEnvironmentVariable("TELEGRAM_BOT_TOKEN");
	if (string.IsNullOrWhiteSpace(token))
	{
		Console.WriteLine("Не задана переменная окружения TELEGRAM_BOT_TOKEN.");
		Console.WriteLine("Получите токен у @BotFather и задайте его перед запуском:");
		Console.WriteLine("  Windows (PowerShell):  $env:TELEGRAM_BOT_TOKEN = \"<токен>\"");
		Console.WriteLine("  Linux/macOS (bash):    export TELEGRAM_BOT_TOKEN=\"<токен>\"");
		return;
	}

	var connectionString = Environment.GetEnvironmentVariable("TODO_DB_CONNECTION_STRING");
	if (string.IsNullOrWhiteSpace(connectionString))
	{
		Console.WriteLine("Не задана переменная окружения TODO_DB_CONNECTION_STRING.");
		Console.WriteLine("Пример для локальной базы ToDoList:");
		Console.WriteLine("  Host=localhost;Port=5432;Database=ToDoList;Username=postgres;Password=<пароль>");
		return;
	}

	// 2. Отдельно храним сигнал завершения и токен выполняющихся операций.
	//    Это позволяет сначала дождаться фоновых задач, а затем остановить бота.
	using var shutdownCts = new CancellationTokenSource();
	using var appCts = new CancellationTokenSource();

	Console.CancelKeyPress += (_, e) =>
	{
		e.Cancel = true;
		shutdownCts.Cancel();
	};

	// 3. Каждый метод SQL-репозитория получает отдельный DataConnection из фабрики.
	IDataContextFactory<ToDoDataContext> dataContextFactory =
		new DataContextFactory(connectionString);

	IUserRepository userRepository = new SqlUserRepository(dataContextFactory);
	IToDoRepository toDoRepository = new SqlToDoRepository(dataContextFactory);
	IToDoListRepository toDoListRepository = new SqlToDoListRepository(dataContextFactory);

	Console.WriteLine("Хранилище данных: PostgreSQL, база ToDoList");

	// 4. Сервисы с DI через конструктор
	IUserService userService = new UserService(userRepository);
	IToDoService toDoService = new AutoPartsToDoService(toDoRepository,
		maxTaskCount: 10, maxTaskLength: 100);
	IToDoListService toDoListService = new ToDoListService(toDoListRepository);
	IToDoReportService toDoReportService = new ToDoReportService(toDoRepository);

	// 4a. Сценарии и хранилище их промежуточного состояния
	IScenarioContextRepository scenarioContextRepository = new InMemoryScenarioContextRepository();
	var scenarios = new IScenario[]
	{
		new AddTaskScenario(userService, toDoService, toDoListService),
		new AddListScenario(userService, toDoListService),
		new DeleteListScenario(userService, toDoListService, toDoService),
		new DeleteTaskScenario(toDoService)
	};

	// 5. Обработчик с возможностью инициировать отмену из /exit
	var handler = new UpdateHandler(userService, toDoService, toDoListService,
		toDoReportService, scenarios, scenarioContextRepository, shutdownCts);

	// 6. HttpClient с прокси (если задан TELEGRAM_BOT_PROXY)
	using var httpClient = ProxyFactory.CreateHttpClient(out var proxyDescription);
	if (proxyDescription != null)
		Console.WriteLine($"Прокси: {proxyDescription}");

	// 7. TelegramBotClient — реальный клиент Bot API.
	//    Передаём HttpClient только если есть прокси, иначе библиотека создаёт свой.
	var botClient = httpClient is null
		? new TelegramBotClient(token)
		: new TelegramBotClient(token, httpClient);

	// 8. Регистрируем команды в нативной кнопке Menu рядом с полем ввода.
	await botClient.SetMyCommands(new[]
	{
		new BotCommand { Command = "start",        Description = "Регистрация в системе" },
		new BotCommand { Command = "help",         Description = "Справка по командам" },
		new BotCommand { Command = "info",         Description = "Информация о программе и аккаунте" },
		new BotCommand { Command = "addtask",      Description = "Добавить заказ (пошаговый сценарий)" },
		new BotCommand { Command = "cancel",       Description = "Отменить текущий сценарий" },
		new BotCommand { Command = "show",         Description = "Показать списки и задачи" },
		new BotCommand { Command = "report",       Description = "Статистика по заказам" },
		new BotCommand { Command = "find",         Description = "Найти заказы: /find <префикс>" },
		new BotCommand { Command = "exit",         Description = "Остановить бота" }
	}, cancellationToken: appCts.Token);

	// 9. Запуск приёма обновлений (long polling).
	var receiverOptions = new ReceiverOptions
	{
		AllowedUpdates = new[] { UpdateType.Message, UpdateType.CallbackQuery },
		DropPendingUpdates = true
	};

	// 9a. Регистрируем и запускаем фоновые задачи до запуска Telegram-бота.
	using var backgroundTaskRunner = new BackgroundTaskRunner();
	backgroundTaskRunner.AddTask(new ResetScenarioBackgroundTask(
		TimeSpan.FromHours(1), scenarioContextRepository, botClient));
	backgroundTaskRunner.StartTasks(appCts.Token);

	try
	{
		botClient.StartReceiving(handler, receiverOptions, appCts.Token);

		var me = await botClient.GetMe(appCts.Token);
		Console.WriteLine($"{me.FirstName} (@{me.Username}) запущен!");
		Console.WriteLine();
		Console.WriteLine("Нажмите клавишу A для выхода");

		// 10. Цикл ожидания сигнала завершения.
		while (!shutdownCts.IsCancellationRequested)
		{
			if (!Console.KeyAvailable)
			{
				try
				{
					await Task.Delay(100, shutdownCts.Token);
				}
				catch (OperationCanceledException) when (shutdownCts.IsCancellationRequested)
				{
					break;
				}

				continue;
			}

			var key = Console.ReadKey(intercept: true);
			if (key.Key == ConsoleKey.A)
			{
				Console.WriteLine("Выход по команде пользователя…");
				shutdownCts.Cancel();
				break;
			}

			// Любая другая клавиша — выводим информацию о боте.
			Console.WriteLine($"Bot: {me.FirstName} (@{me.Username})");
			Console.WriteLine($"  Id:                       {me.Id}");
			Console.WriteLine($"  Может присоединяться:     {me.CanJoinGroups}");
			Console.WriteLine($"  Читает все сообщения:     {me.CanReadAllGroupMessages}");
			Console.WriteLine($"  Поддерживает inline:      {me.SupportsInlineQueries}");
			Console.WriteLine();
			Console.WriteLine("Нажмите клавишу A для выхода");
		}
	}
	finally
	{
		// 11. Сначала завершаем фоновые задачи, затем отменяем операции бота.
		await backgroundTaskRunner.StopTasks(CancellationToken.None);
		appCts.Cancel();
	}

	// Даём отмене распространиться.
	await Task.Delay(200);
}
catch (OperationCanceledException)
{
	// Штатное завершение — не ошибка
}
catch (Exception ex)
{
	Console.WriteLine($"Критическая ошибка: {ex.GetType().Name}");
	Console.WriteLine($"Сообщение: {ex.Message}");
	Console.WriteLine($"StackTrace: {ex.StackTrace}");
}
