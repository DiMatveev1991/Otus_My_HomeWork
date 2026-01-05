using System;

namespace Models
{
	// Класс пользователя
	public class ToDoUser
	{
		public Guid UserId { get; }
		public string TelegramUserName { get; }
		public DateTime RegisteredAt { get; }

		public ToDoUser(string telegramUserName)
		{
			UserId = Guid.NewGuid();
			TelegramUserName = telegramUserName;
			RegisteredAt = DateTime.UtcNow;
		}

		public override string ToString()
		{
			return $"{TelegramUserName} (зарегистрирован: {RegisteredAt:dd.MM.yyyy HH:mm})";
		}
	}
}
