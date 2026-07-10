using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Core.Entities;
using Core.Services;
using Telegram.Bot;
using Telegram.Bot.Types;
using TelegramBot.Dto;

namespace TelegramBot.Scenarios
{
	/// <summary>
	/// Сценарий создания задачи. Пошагово запрашивает название, дедлайн и список,
	/// сохраняя промежуточные данные в <see cref="ScenarioContext.Data"/>.
	/// Выбор списка выполняется через Inline-кнопки (CallbackQuery).
	/// </summary>
	public class AddTaskScenario : IScenario
	{
		private const string DataUserKey = "User";
		private const string DataNameKey = "Name";
		private const string DataDeadlineKey = "Deadline";
		private const string DeadlineFormat = "dd.MM.yyyy";
		private const string ListAction = "addtask";

		private readonly IUserService _userService;
		private readonly IToDoService _toDoService;
		private readonly IToDoListService _toDoListService;

		public AddTaskScenario(
			IUserService userService,
			IToDoService toDoService,
			IToDoListService toDoListService)
		{
			_userService = userService;
			_toDoService = toDoService;
			_toDoListService = toDoListService;
		}

		public bool CanHandle(ScenarioType scenario) => scenario == ScenarioType.AddTask;

		public async Task<ScenarioResult> HandleMessageAsync(
			ITelegramBotClient bot, ScenarioContext context, Update update, CancellationToken ct)
		{
			var message = update.Message;
			var callback = update.CallbackQuery;
			var chatId = message?.Chat.Id ?? callback!.Message!.Chat.Id;

			switch (context.CurrentStep)
			{
				case null:
				{
					// Получаем пользователя и сохраняем его в контексте сценария
					var user = await _userService.GetUserAsync(update.Message!.From!.Id, ct);
					context.Data[DataUserKey] = user!;

					await bot.SendMessage(chatId, "Введите название задачи:",
						replyMarkup: KeyboardFactory.Cancel, cancellationToken: ct);

					context.CurrentStep = "Name";
					return ScenarioResult.Transition;
				}

				case "Name":
				{
					if (message?.Text is null) return ScenarioResult.Transition;

					context.Data[DataNameKey] = message.Text.Trim();

					await bot.SendMessage(chatId,
						$"Введите дедлайн задачи в формате {DeadlineFormat}:",
						replyMarkup: KeyboardFactory.Cancel, cancellationToken: ct);

					context.CurrentStep = "Deadline";
					return ScenarioResult.Transition;
				}

				case "Deadline":
				{
					if (message?.Text is null) return ScenarioResult.Transition;

					// Неверный формат — не прерываем сценарий, просим ввести дату ещё раз
					if (!DateTime.TryParseExact(message.Text.Trim(), DeadlineFormat,
						CultureInfo.InvariantCulture, DateTimeStyles.None, out var deadline))
					{
						await bot.SendMessage(chatId,
							$"Неверный формат даты. Введите дедлайн в формате {DeadlineFormat}:",
							replyMarkup: KeyboardFactory.Cancel, cancellationToken: ct);
						return ScenarioResult.Transition;
					}

					context.Data[DataDeadlineKey] = deadline;

					// Предлагаем выбрать список через Inline-кнопки
					var user = (ToDoUser)context.Data[DataUserKey];
					var lists = await _toDoListService.GetUserLists(user.UserId, ct);

					await bot.SendMessage(chatId, "Выберите список для задачи:",
						replyMarkup: KeyboardFactory.ListsInline(lists, ListAction,
							includeNoList: true, includeManageButtons: false),
						cancellationToken: ct);

					context.CurrentStep = "List";
					return ScenarioResult.Transition;
				}

				case "List":
				{
					// На этом шаге ожидаем нажатие Inline-кнопки
					if (callback?.Data is null)
					{
						await bot.SendMessage(chatId,
							"Выберите список, используя кнопки под сообщением.",
							cancellationToken: ct);
						return ScenarioResult.Transition;
					}

					var dto = ToDoListCallbackDto.FromString(callback.Data);

					ToDoList? list = null;
					if (dto.ToDoListId is { } listId)
						list = await _toDoListService.Get(listId, ct);

					var user = (ToDoUser)context.Data[DataUserKey];
					var name = (string)context.Data[DataNameKey];
					var deadline = (DateTime)context.Data[DataDeadlineKey];

					var item = await _toDoService.AddAsync(user, name, deadline, list, ct);

					await bot.SendMessage(chatId,
						$"Задача добавлена!\n" +
						$"Название: {item.Name}\n" +
						$"Дедлайн: {item.Deadline:dd.MM.yyyy}\n" +
						$"Список: {item.List?.Name ?? "Без списка"}\n" +
						$"ID: {item.Id}",
						replyMarkup: KeyboardFactory.PostRegistration, cancellationToken: ct);

					return ScenarioResult.Completed;
				}

				default:
					return ScenarioResult.Completed;
			}
		}
	}
}
