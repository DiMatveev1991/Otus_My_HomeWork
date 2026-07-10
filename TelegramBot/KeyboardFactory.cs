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
		public static ReplyKeyboardMarkup PostRegistration { get; } =
			new(new[]
			{
				new[]
				{
					new KeyboardButton("/addtask"),
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

		// Клавиатура во время выполнения сценария — доступна только отмена
		public static ReplyKeyboardMarkup Cancel { get; } =
			new(new[] { new KeyboardButton("/cancel") })
			{
				ResizeKeyboard = true,
				IsPersistent = true
			};
	}
}
