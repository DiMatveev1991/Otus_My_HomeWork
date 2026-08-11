using System;

namespace TelegramBot.Dto
{
	/// <summary>
	/// DTO кнопки, связанной с задачей.
	/// Формат строки: {action}|{toDoItemId}.
	/// </summary>
	public class ToDoItemCallbackDto : CallbackDto
	{
		public Guid ToDoItemId { get; set; }

		public ToDoItemCallbackDto()
		{
		}

		public ToDoItemCallbackDto(string action, Guid toDoItemId)
		{
			Action = action;
			ToDoItemId = toDoItemId;
		}

		public static new ToDoItemCallbackDto FromString(string input)
		{
			var parts = input.Split('|');
			var dto = new ToDoItemCallbackDto { Action = parts[0] };

			if (parts.Length > 1 && Guid.TryParse(parts[1], out var id))
				dto.ToDoItemId = id;

			return dto;
		}

		public override string ToString() => $"{base.ToString()}|{ToDoItemId}";
	}
}
