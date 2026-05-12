using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Core.Entities;

namespace Core.DataAccess
{
	public interface IToDoRepository
	{
		Task<IReadOnlyList<ToDoItem>> GetAllByUserIdAsync(Guid userId, CancellationToken ct);

		/// <summary>Возвращает ToDoItem для UserId со статусом Active</summary>
		Task<IReadOnlyList<ToDoItem>> GetActiveByUserIdAsync(Guid userId, CancellationToken ct);

		Task<ToDoItem?> GetAsync(Guid id, CancellationToken ct);
		Task AddAsync(ToDoItem item, CancellationToken ct);
		Task UpdateAsync(ToDoItem item, CancellationToken ct);
		Task DeleteAsync(Guid id, CancellationToken ct);

		/// <summary>Проверяет, есть ли задача с таким именем у пользователя</summary>
		Task<bool> ExistsByNameAsync(Guid userId, string name, CancellationToken ct);

		/// <summary>Возвращает количество активных задач у пользователя</summary>
		Task<int> CountActiveAsync(Guid userId, CancellationToken ct);

		/// <summary>Возвращает все задачи пользователя, удовлетворяющие предикату</summary>
		Task<IReadOnlyList<ToDoItem>> FindAsync(Guid userId, Func<ToDoItem, bool> predicate, CancellationToken ct);
	}
}
