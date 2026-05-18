using System;
using System.Text.Json.Serialization;
using Core.Enums;

namespace Core.Entities
{
	// Класс задачи (заказа на автозапчасть)
	public class ToDoItem
	{
		public Guid Id { get; }
		public ToDoUser User { get; }
		public string Name { get; }
		public DateTime CreatedAt { get; }

		// [JsonInclude] — иначе System.Text.Json не запишет свойство с private set
		[JsonInclude]
		public ToDoItemState State { get; private set; }

		[JsonInclude]
		public DateTime? StateChangedAt { get; private set; }

		// Конструктор для создания новой задачи в рантайме
		public ToDoItem(ToDoUser user, string name)
		{
			Id = Guid.NewGuid();
			User = user;
			Name = name;
			CreatedAt = DateTime.UtcNow;
			State = ToDoItemState.Active;
			StateChangedAt = null;
		}

		// Конструктор для JsonSerializer — восстанавливает объект из JSON.
		// Имена параметров должны совпадать с именами свойств (case-insensitive).
		[JsonConstructor]
		public ToDoItem(Guid id, ToDoUser user, string name, DateTime createdAt,
			ToDoItemState state, DateTime? stateChangedAt)
		{
			Id = id;
			User = user;
			Name = name;
			CreatedAt = createdAt;
			State = state;
			StateChangedAt = stateChangedAt;
		}

		public void MarkAsCompleted()
		{
			State = ToDoItemState.Completed;
			StateChangedAt = DateTime.UtcNow;
		}

		public override string ToString()
		{
			return $"{Name} - {CreatedAt:dd.MM.yyyy HH:mm:ss} - {Id}";
		}

		public string ToStringWithState()
		{
			var stateText = State == ToDoItemState.Active ? "(Active)" : "(Completed)";
			var stateChangedText = StateChangedAt.HasValue
				? $" | Изменено: {StateChangedAt.Value:dd.MM.yyyy HH:mm:ss}"
				: "";

			return $"{stateText} {Name} - {CreatedAt:dd.MM.yyyy HH:mm:ss} - {Id}{stateChangedText}";
		}
	}
}