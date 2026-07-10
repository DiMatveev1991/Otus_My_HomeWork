namespace TelegramBot.Dto
{
	/// <summary>
	/// Базовый DTO для данных Inline-кнопок (CallbackQuery.Data).
	/// Компактный строковый формат {action}|{prop1}|{prop2}...,
	/// т.к. максимальный размер callbackData — 64 символа.
	/// </summary>
	public class CallbackDto
	{
		// По нему определяем, за какое действие отвечает кнопка
		public string Action { get; set; } = string.Empty;

		/// <summary>
		/// На вход принимает строку вида {action}|{prop1}|{prop2}....
		/// Если | в строке нет — вся строка сохраняется в Action.
		/// </summary>
		public static CallbackDto FromString(string input)
		{
			var parts = input.Split('|');
			return new CallbackDto { Action = parts[0] };
		}

		public override string ToString() => Action;
	}
}
