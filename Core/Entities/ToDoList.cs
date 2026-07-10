using System;
using System.Text.Json.Serialization;

namespace Core.Entities
{
	// Список для группировки задач по смыслу.
	public class ToDoList
	{
		public Guid Id { get; }
		public string Name { get; }
		public ToDoUser User { get; }
		public DateTime CreatedAt { get; }

		// Конструктор для создания нового списка в рантайме
		public ToDoList(ToDoUser user, string name)
		{
			Id = Guid.NewGuid();
			Name = name;
			User = user;
			CreatedAt = DateTime.UtcNow;
		}

		// Конструктор для JsonSerializer — восстанавливает объект из JSON.
		[JsonConstructor]
		public ToDoList(Guid id, string name, ToDoUser user, DateTime createdAt)
		{
			Id = id;
			Name = name;
			User = user;
			CreatedAt = createdAt;
		}

		public override string ToString()
		{
			return $"{Name} - {CreatedAt:dd.MM.yyyy HH:mm:ss} - {Id}";
		}
	}
}
