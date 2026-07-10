using System;
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
	/// Сценарий удаления списка задач.
	/// Шаги: выбор списка → подтверждение → удаление списка и всех его задач.
	/// </summary>
	public class DeleteListScenario : IScenario
	{
		private const string DataUserKey = "User";
		private const string DataListKey = "List";
		private const string DeleteAction = "deletelist";

		private readonly IUserService _userService;
		private readonly IToDoListService _toDoListService;
		private readonly IToDoService _toDoService;

		public DeleteListScenario(
			IUserService userService,
			IToDoListService toDoListService,
			IToDoService toDoService)
		{
			_userService = userService;
			_toDoListService = toDoListService;
			_toDoService = toDoService;
		}

		public bool CanHandle(ScenarioType scenario) => scenario == ScenarioType.DeleteList;

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

					var lists = await _toDoListService.GetUserLists(user!.UserId, ct);
					if (lists.Count == 0)
					{
						await bot.SendMessage(chatId,
							"У вас нет списков для удаления.",
							replyMarkup: KeyboardFactory.PostRegistration, cancellationToken: ct);
						return ScenarioResult.Completed;
					}

					await bot.SendMessage(chatId, "Выберете список для удаления:",
						replyMarkup: KeyboardFactory.ListsInline(lists, DeleteAction,
							includeNoList: false, includeManageButtons: false),
						cancellationToken: ct);

					context.CurrentStep = "Approve";
					return ScenarioResult.Transition;
				}

				case "Approve":
				{
					if (callback?.Data is null)
					{
						await bot.SendMessage(chatId,
							"Выберите список, используя кнопки под сообщением.",
							cancellationToken: ct);
						return ScenarioResult.Transition;
					}

					var dto = ToDoListCallbackDto.FromString(callback.Data);
					if (dto.ToDoListId is not { } listId)
						return ScenarioResult.Transition;

					var toDoList = await _toDoListService.Get(listId, ct);
					if (toDoList is null)
					{
						await bot.SendMessage(chatId,
							"Список не найден. Удаление отменено.",
							replyMarkup: KeyboardFactory.PostRegistration, cancellationToken: ct);
						return ScenarioResult.Completed;
					}

					context.Data[DataListKey] = toDoList;

					var confirmKeyboard = new InlineKeyboardMarkup(new[]
					{
						new[]
						{
							InlineKeyboardButton.WithCallbackData("✅Да", "yes"),
							InlineKeyboardButton.WithCallbackData("❌Нет", "no")
						}
					});

					await bot.SendMessage(chatId,
						$"Подтверждаете удаление списка {toDoList.Name} и всех его задач",
						replyMarkup: confirmKeyboard, cancellationToken: ct);

					context.CurrentStep = "Delete";
					return ScenarioResult.Transition;
				}

				case "Delete":
				{
					var data = callback?.Data;

					if (data == "yes")
					{
						var user = (ToDoUser)context.Data[DataUserKey];
						var toDoList = (ToDoList)context.Data[DataListKey];

						// Удаляем все задачи по ToDoUser и ToDoList, затем сам список
						var items = await _toDoService.GetByUserIdAndList(user.UserId, toDoList.Id, ct);
						foreach (var item in items)
						{
							ct.ThrowIfCancellationRequested();
							await _toDoService.DeleteAsync(item.Id, ct);
						}

						await _toDoListService.Delete(toDoList.Id, ct);

						await bot.SendMessage(chatId,
							$"Список {toDoList.Name} и все его задачи ({items.Count}) удалены.",
							replyMarkup: KeyboardFactory.PostRegistration, cancellationToken: ct);
					}
					else if (data == "no")
					{
						await bot.SendMessage(chatId, "Удаление отменено.",
							replyMarkup: KeyboardFactory.PostRegistration, cancellationToken: ct);
					}
					else
					{
						await bot.SendMessage(chatId,
							"Подтвердите удаление кнопками ✅Да / ❌Нет.",
							cancellationToken: ct);
						return ScenarioResult.Transition;
					}

					return ScenarioResult.Completed;
				}

				default:
					return ScenarioResult.Completed;
			}
		}
	}
}
