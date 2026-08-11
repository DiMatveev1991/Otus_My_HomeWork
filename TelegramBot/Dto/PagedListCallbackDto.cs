using System;

namespace TelegramBot.Dto
{
	/// <summary>
	/// DTO страницы задач выбранного списка.
	/// Формат строки: {action}|{toDoListId}|{page}.
	/// </summary>
	public class PagedListCallbackDto : ToDoListCallbackDto
	{
		public int Page { get; set; }

		public PagedListCallbackDto()
		{
		}

		public PagedListCallbackDto(string action, Guid? toDoListId, int page)
		{
			Action = action;
			ToDoListId = toDoListId;
			Page = page;
		}

		public static new PagedListCallbackDto FromString(string input)
		{
			var listDto = ToDoListCallbackDto.FromString(input);
			var parts = input.Split('|');

			var dto = new PagedListCallbackDto
			{
				Action = listDto.Action,
				ToDoListId = listDto.ToDoListId
			};

			if (parts.Length > 2 && int.TryParse(parts[2], out var page))
				dto.Page = page;

			return dto;
		}

		public override string ToString() => $"{base.ToString()}|{Page}";
	}
}
