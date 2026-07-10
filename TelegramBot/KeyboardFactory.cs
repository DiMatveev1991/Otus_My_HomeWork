using System.Collections.Generic;
using Core.Entities;
using Telegram.Bot.Types.ReplyMarkups;
using TelegramBot.Dto;

namespace TelegramBot
{
	/// <summary>
	/// Фабрика клавиатур.
	/// До регистрации пользователю доступна только кнопка /start.
	/// После регистрации — кнопки /addtask, /show, /report.
	/// </summary>
	public static class KeyboardFactory
	{
		// Клавиатура для незарегистрированных пользователей
		public static ReplyKeyboardMarkup PreRegistration { get; } =
			new(new[] { new KeyboardButton("/start") })
			{
				ResizeKeyboard = true,
				IsPersistent = true
			};

		// Клавиатура для зарегистрированных пользователей (вне сценариев)
		public static ReplyKeyboardMarkup PostRegistration { get; } =
			new(new[]
			{
				new[]
				{
					new KeyboardButton("/addtask"),
					new KeyboardButton("/show"),
					new KeyboardButton("/report")
				}
			})
			{
				ResizeKeyboard = true,
				IsPersistent = true
			};

		// Клавиатура во время выполнения сценария — доступна только отмена
		public static ReplyKeyboardMarkup Cancel { get; } =
			new(new[] { new KeyboardButton("/cancel") })
			{
				ResizeKeyboard = true,
				IsPersistent = true
			};

		/// <summary>
		/// Inline-клавиатура выбора списка.
		/// </summary>
		/// <param name="lists">Списки пользователя.</param>
		/// <param name="action">Action, который будет записан в callbackData каждой кнопки списка.</param>
		/// <param name="includeNoList">Добавить кнопку "📌Без списка" (ToDoListId = null).</param>
		/// <param name="includeManageButtons">Добавить строку "🆕Добавить" / "❌Удалить".</param>
		public static InlineKeyboardMarkup ListsInline(
			IReadOnlyList<ToDoList> lists,
			string action,
			bool includeNoList,
			bool includeManageButtons)
		{
			var rows = new List<InlineKeyboardButton[]>();

			if (includeNoList)
			{
				var noList = new ToDoListCallbackDto { Action = action, ToDoListId = null };
				rows.Add(new[]
				{
					InlineKeyboardButton.WithCallbackData("📌Без списка", noList.ToString())
				});
			}

			foreach (var list in lists)
			{
				var dto = new ToDoListCallbackDto { Action = action, ToDoListId = list.Id };
				rows.Add(new[]
				{
					InlineKeyboardButton.WithCallbackData(list.Name, dto.ToString())
				});
			}

			if (includeManageButtons)
			{
				rows.Add(new[]
				{
					InlineKeyboardButton.WithCallbackData("Добавить", "addlist"),
					InlineKeyboardButton.WithCallbackData("Удалить", "deletelist")
				});
			}

			return new InlineKeyboardMarkup(rows);
		}
	}
}
