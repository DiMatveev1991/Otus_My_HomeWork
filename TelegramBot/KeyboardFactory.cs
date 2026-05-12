using Telegram.Bot.Types.ReplyMarkups;

namespace TelegramBot
{
	/// <summary>
	/// Фабрика Reply-клавиатур.
	/// До регистрации пользователю доступна только кнопка /start.
	/// После регистрации — кнопки /showalltasks, /showtasks, /report.
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

		// Клавиатура для зарегистрированных пользователей
		// (ровно те три кнопки, что прописаны в задании)
		public static ReplyKeyboardMarkup PostRegistration { get; } =
			new(new[]
			{
				new[]
				{
					new KeyboardButton("/showtasks"),
					new KeyboardButton("/showalltasks")
				},
				new[]
				{
					new KeyboardButton("/report")
				}
			})
			{
				ResizeKeyboard = true,
				IsPersistent = true
			};
	}
}
