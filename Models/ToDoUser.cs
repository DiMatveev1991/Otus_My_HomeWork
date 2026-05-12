using System;

namespace Models
{
	// Класс пользователя магазина автозапчастей
	public class ToDoUser
	{
		public Guid UserId { get; }
		public long TelegramUserId { get; }
		public string TelegramUserName { get; }
		public DateTime RegisteredAt { get; }

		public ToDoUser(long telegramUserId, string telegramUserName)
		{
			UserId = Guid.NewGuid();
			TelegramUserId = telegramUserId;
			TelegramUserName = telegramUserName;
			RegisteredAt = DateTime.UtcNow;
		}

		public override string ToString()
		{
			return $"{TelegramUserName} (зарегистрирован: {RegisteredAt:dd.MM.yyyy HH:mm})";
		}
	}
}