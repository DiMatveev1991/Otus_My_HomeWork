using System;
using System.Threading;
using System.Threading.Tasks;
using Core.Entities;
using Core.Services;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace TelegramBot.Scenarios
{
	/// <summary>
	/// Сценарий создания списка задач.
	/// Шаги: запрос названия → создание списка.
	/// </summary>
	public class AddListScenario : IScenario
	{
		private const string DataUserKey = "User";

		private readonly IUserService _userService;
		private readonly IToDoListService _toDoListService;

		public AddListScenario(IUserService userService, IToDoListService toDoListService)
		{
			_userService = userService;
			_toDoListService = toDoListService;
		}

		public bool CanHandle(ScenarioType scenario) => scenario == ScenarioType.AddList;

		public async Task<ScenarioResult> HandleMessageAsync(
			ITelegramBotClient bot, ScenarioContext context, Update update, CancellationToken ct)
		{
			var message = update.Message;
			var callback = update.CallbackQuery;
			var chatId = message?.Chat.Id ?? callback!.Message!.Chat.Id;
			var fromId = message?.From?.Id ?? callback!.From.Id;

			switch (context.CurrentStep)
			{
				case null:
				{
					var user = await _userService.GetUserAsync(fromId, ct);
					context.Data[DataUserKey] = user!;

					await bot.SendMessage(chatId, "Введите название списка:",
						replyMarkup: KeyboardFactory.Cancel, cancellationToken: ct);

					context.CurrentStep = "Name";
					return ScenarioResult.Transition;
				}

				case "Name":
				{
					if (message?.Text is null) return ScenarioResult.Transition;

					var user = (ToDoUser)context.Data[DataUserKey];
					var name = message.Text.Trim();

					try
					{
						var list = await _toDoListService.Add(user, name, ct);
						await bot.SendMessage(chatId,
							$"Список \"{list.Name}\" создан.",
							replyMarkup: KeyboardFactory.PostRegistration, cancellationToken: ct);
					}
					catch (ArgumentException ex)
					{
						// Нарушены правила (длина/уникальность имени) — сообщаем пользователю
						await bot.SendMessage(chatId,
							$"Не удалось создать список: {ex.Message}",
							replyMarkup: KeyboardFactory.PostRegistration, cancellationToken: ct);
					}

					return ScenarioResult.Completed;
				}

				default:
					return ScenarioResult.Completed;
			}
		}
	}
}
