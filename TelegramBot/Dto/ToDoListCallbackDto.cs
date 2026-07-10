using System;

namespace TelegramBot.Dto
{
	/// <summary>
	/// DTO кнопки, связанной со списком задач.
	/// Формат строки: {action}|{toDoListId}.
	/// </summary>
	public class ToDoListCallbackDto : CallbackDto
	{
		public Guid? ToDoListId { get; set; }

		/// <summary>
		/// На вход принимает строку вида {action}|{toDoListId}|{prop2}....
		/// Создаёт ToDoListCallbackDto с Action = action и ToDoListId = toDoListId.
		/// </summary>
		public static new ToDoListCallbackDto FromString(string input)
		{
			var parts = input.Split('|');
			var dto = new ToDoListCallbackDto { Action = parts[0] };

			if (parts.Length > 1 && Guid.TryParse(parts[1], out var id))
				dto.ToDoListId = id;

			return dto;
		}

		public override string ToString() => $"{base.ToString()}|{ToDoListId}";
	}
}
