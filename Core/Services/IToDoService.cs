using System;
using System.Collections.Generic;
using Core.Entities;

namespace Core.Services
{
	public interface IToDoService
	{
		IReadOnlyList<ToDoItem> GetAllByUserId(Guid userId);

		/// <summary>Возвращает ToDoItem для UserId со статусом Active</summary>
		IReadOnlyList<ToDoItem> GetActiveByUserId(Guid userId);

		ToDoItem Add(ToDoUser user, string name);
		void MarkCompleted(Guid id);
		void Delete(Guid id);

		/// <summary>
		/// Возвращает все задачи пользователя, имя которых начинается на namePrefix.
		/// Использует IToDoRepository.Find с предикатом-лямбдой.
		/// </summary>
		IReadOnlyList<ToDoItem> Find(ToDoUser user, string namePrefix);
	}
}
