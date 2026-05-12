using System;
using System.Collections.Generic;
using Core.Entities;

namespace Core.DataAccess
{
	public interface IToDoRepository
	{
		IReadOnlyList<ToDoItem> GetAllByUserId(Guid userId);

		/// <summary>Возвращает ToDoItem для UserId со статусом Active</summary>
		IReadOnlyList<ToDoItem> GetActiveByUserId(Guid userId);

		ToDoItem? Get(Guid id);
		void Add(ToDoItem item);
		void Update(ToDoItem item);
		void Delete(Guid id);

		/// <summary>Проверяет, есть ли задача с таким именем у пользователя</summary>
		bool ExistsByName(Guid userId, string name);

		/// <summary>Возвращает количество активных задач у пользователя</summary>
		int CountActive(Guid userId);

		/// <summary>Возвращает все задачи пользователя, удовлетворяющие предикату</summary>
		IReadOnlyList<ToDoItem> Find(Guid userId, Func<ToDoItem, bool> predicate);
	}
}
