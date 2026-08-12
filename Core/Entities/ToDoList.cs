using System;

namespace Core.Entities
{
	// Список для группировки задач по смыслу.
	public class ToDoList
	{
		public Guid Id { get; set; }
		public string Name { get; set; } = string.Empty;
		public ToDoUser User { get; set; } = null!;
		public DateTime CreatedAt { get; set; }

		public override string ToString()
		{
			return $"{Name} - {CreatedAt:dd.MM.yyyy HH:mm:ss} - {Id}";
		}
	}
}
