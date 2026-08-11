using System.Threading;
using System.Threading.Tasks;
using Core.Entities;
using Core.Services;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using TelegramBot.Dto;

namespace TelegramBot.Scenarios
{
	/// <summary>
	/// Сценарий удаления задачи: показ подтверждения и удаление после согласия.
	/// </summary>
	public class DeleteTaskScenario : IScenario
	{
		private const string DataTaskKey = "Task";

		private readonly IToDoService _toDoService;

		public DeleteTaskScenario(IToDoService toDoService)
		{
			_toDoService = toDoService;
		}

		public bool CanHandle(ScenarioType scenario) => scenario == ScenarioType.DeleteTask;

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
					if (callback?.Data is null || callback.Message is null)
						return ScenarioResult.Completed;

					var dto = ToDoItemCallbackDto.FromString(callback.Data);
					var item = await _toDoService.Get(dto.ToDoItemId, ct);

					if (item is null || item.User.TelegramUserId != callback.From.Id)
					{
						await bot.EditMessageText(chatId, callback.Message.Id,
							"Задача не найдена", cancellationToken: ct);
						return ScenarioResult.Completed;
					}

					context.Data[DataTaskKey] = item;

					var confirmKeyboard = new InlineKeyboardMarkup(new[]
					{
						new[]
						{
							InlineKeyboardButton.WithCallbackData("✅Да", "yes"),
							InlineKeyboardButton.WithCallbackData("❌Нет", "no")
						}
					});

					await bot.EditMessageText(chatId, callback.Message.Id,
						$"Подтверждаете удаление задачи \"{item.Name}\"?",
						replyMarkup: confirmKeyboard, cancellationToken: ct);

					context.CurrentStep = "Delete";
					return ScenarioResult.Transition;
				}

				case "Delete":
				{
					if (callback?.Message is null)
					{
						await bot.SendMessage(chatId,
							"Подтвердите удаление кнопками ✅Да / ❌Нет.",
							cancellationToken: ct);
						return ScenarioResult.Transition;
					}

					var item = (ToDoItem)context.Data[DataTaskKey];

					if (callback.Data == "yes")
					{
						var currentItem = await _toDoService.Get(item.Id, ct);
						if (currentItem is not null &&
							currentItem.User.TelegramUserId == callback.From.Id)
						{
							await _toDoService.DeleteAsync(item.Id, ct);
						}

						await bot.EditMessageText(chatId, callback.Message.Id,
							$"Задача \"{item.Name}\" удалена.", cancellationToken: ct);
						return ScenarioResult.Completed;
					}

					if (callback.Data == "no")
					{
						await bot.EditMessageText(chatId, callback.Message.Id,
							"Удаление отменено.", cancellationToken: ct);
						return ScenarioResult.Completed;
					}

					await bot.SendMessage(chatId,
						"Подтвердите удаление кнопками ✅Да / ❌Нет.",
						cancellationToken: ct);
					return ScenarioResult.Transition;
				}

				default:
					return ScenarioResult.Completed;
			}
		}
	}
}
