using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Core.Entities;
using Core.Services;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace TelegramBot.Scenarios
{
	/// <summary>
	/// Сценарий создания задачи. Пошагово запрашивает название и дедлайн,
	/// сохраняя промежуточные данные в <see cref="ScenarioContext.Data"/>.
	/// </summary>
	public class AddTaskScenario : IScenario
	{
		private const string DataUserKey = "User";
		private const string DataNameKey = "Name";
		private const string DeadlineFormat = "dd.MM.yyyy";

		private readonly IUserService _userService;
		private readonly IToDoService _toDoService;

		public AddTaskScenario(IUserService userService, IToDoService toDoService)
		{
			_userService = userService;
			_toDoService = toDoService;
		}

		public bool CanHandle(ScenarioType scenario) => scenario == ScenarioType.AddTask;

		public async Task<ScenarioResult> HandleMessageAsync(
			ITelegramBotClient bot, ScenarioContext context, Message message, CancellationToken ct)
		{
			var chatId = message.Chat.Id;

			switch (context.CurrentStep)
			{
				case null:
				{
					// Получаем пользователя и сохраняем его в контексте сценария
					var user = await _userService.GetUserAsync(message.From!.Id, ct);
					context.Data[DataUserKey] = user!;

					await bot.SendMessage(chatId, "Введите название задачи:",
						replyMarkup: KeyboardFactory.Cancel, cancellationToken: ct);

					context.CurrentStep = "Name";
					return ScenarioResult.Transition;
				}

				case "Name":
				{
					context.Data[DataNameKey] = message.Text!.Trim();

					await bot.SendMessage(chatId,
						$"Введите дедлайн задачи в формате {DeadlineFormat}:",
						replyMarkup: KeyboardFactory.Cancel, cancellationToken: ct);

					context.CurrentStep = "Deadline";
					return ScenarioResult.Transition;
				}

				case "Deadline":
				{
					// Неверный формат — не прерываем сценарий, просим ввести дату ещё раз
					if (!DateTime.TryParseExact(message.Text!.Trim(), DeadlineFormat,
						CultureInfo.InvariantCulture, DateTimeStyles.None, out var deadline))
					{
						await bot.SendMessage(chatId,
							$"Неверный формат даты. Введите дедлайн в формате {DeadlineFormat}:",
							replyMarkup: KeyboardFactory.Cancel, cancellationToken: ct);
						return ScenarioResult.Transition;
					}

					var user = (ToDoUser)context.Data[DataUserKey];
					var name = (string)context.Data[DataNameKey];

					var item = await _toDoService.AddAsync(user, name, deadline, ct);

					await bot.SendMessage(chatId,
						$"Задача добавлена!\n" +
						$"Название: {item.Name}\n" +
						$"Дедлайн: {item.Deadline:dd.MM.yyyy}\n" +
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
