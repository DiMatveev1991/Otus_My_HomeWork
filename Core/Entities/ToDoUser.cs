using System;
using System.Text.Json.Serialization;

namespace Core.Entities
{
	// Класс пользователя магазина автозапчастей
	public class ToDoUser
	{
		public Guid UserId { get; }
		public long TelegramUserId { get; }
		public string TelegramUserName { get; }
		public DateTime RegisteredAt { get; }

		// Конструктор для регистрации нового пользователя
		public ToDoUser(long telegramUserId, string telegramUserName)
		{
			UserId = Guid.NewGuid();
			TelegramUserId = telegramUserId;
			TelegramUserName = telegramUserName;
			RegisteredAt = DateTime.UtcNow;
		}

		// Конструктор для JsonSerializer
		[JsonConstructor]
		public ToDoUser(Guid userId, long telegramUserId, string telegramUserName, DateTime registeredAt)
		{
			UserId = userId;
			TelegramUserId = telegramUserId;
			TelegramUserName = telegramUserName;
			RegisteredAt = registeredAt;
		}

		public override string ToString()
		{
			return $"{TelegramUserName} (зарегистрирован: {RegisteredAt:dd.MM.yyyy HH:mm})";
		}
	}
}