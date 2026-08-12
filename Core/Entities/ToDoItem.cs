using System;
using Core.Enums;

namespace Core.Entities
{
	// Класс задачи (заказа на автозапчасть)
	public class ToDoItem
	{
		public Guid Id { get; set; }
		public ToDoUser User { get; set; } = null!;
		public string Name { get; set; } = string.Empty;
		public DateTime CreatedAt { get; set; }
		public DateTime Deadline { get; set; }

		// Список, к которому привязана задача. null — задача без списка.
		public ToDoList? List { get; set; }
		public ToDoItemState State { get; set; }
		public DateTime? StateChangedAt { get; set; }

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
