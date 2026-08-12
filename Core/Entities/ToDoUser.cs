using System;

namespace Core.Entities
{
	// Класс пользователя магазина автозапчастей
	public class ToDoUser
	{
		public Guid UserId { get; set; }
		public long TelegramUserId { get; set; }
		public string TelegramUserName { get; set; } = string.Empty;
		public DateTime RegisteredAt { get; set; }

		public override string ToString()
		{
			return $"{TelegramUserName} (зарегистрирован: {RegisteredAt:dd.MM.yyyy HH:mm})";
		}
	}
}
