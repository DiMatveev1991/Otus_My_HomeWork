using System;
using Enums;


namespace Models
{
	// Класс задачи
	public class ToDoItem
	{
		public Guid Id { get; }
		public ToDoUser User { get; }
		public string Name { get; }
		public DateTime CreatedAt { get; }
		public ToDoItemState State { get; private set; }
		public DateTime? StateChangedAt { get; private set; }

		public ToDoItem(ToDoUser user, string name)
		{
			Id = Guid.NewGuid();
			User = user;
			Name = name;
			CreatedAt = DateTime.UtcNow;
			State = ToDoItemState.Active;
			StateChangedAt = null;
		}

		public void MarkAsCompleted()
		{
			State = ToDoItemState.Completed;
			StateChangedAt = DateTime.UtcNow;
		}

		public override string ToString()
		{
			var stateText = State == ToDoItemState.Active ? "Active" : "Completed";
			var stateChangedText = StateChangedAt.HasValue
				? $" | Изменено: {StateChangedAt.Value:dd.MM.yyyy HH:mm:ss}"
				: "";

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
