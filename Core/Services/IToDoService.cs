using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Core.Entities;

namespace Core.Services
{
	public interface IToDoService
	{
		Task<ToDoItem?> Get(Guid toDoItemId, CancellationToken ct);

		Task<IReadOnlyList<ToDoItem>> GetAllByUserIdAsync(Guid userId, CancellationToken ct);

		/// <summary>Возвращает ToDoItem для UserId со статусом Active</summary>
		Task<IReadOnlyList<ToDoItem>> GetActiveByUserIdAsync(Guid userId, CancellationToken ct);

		/// <summary>
		/// Возвращает задачи пользователя, привязанные к списку listId.
		/// Если listId == null — задачи без списка.
		/// </summary>
		Task<IReadOnlyList<ToDoItem>> GetByUserIdAndList(Guid userId, Guid? listId, CancellationToken ct);

		Task<ToDoItem> AddAsync(ToDoUser user, string name, DateTime deadline, ToDoList? list, CancellationToken ct);
		Task MarkCompletedAsync(Guid id, CancellationToken ct);
		Task DeleteAsync(Guid id, CancellationToken ct);

		/// <summary>
		/// Возвращает все задачи пользователя, имя которых начинается на namePrefix.
		/// Использует IToDoRepository.FindAsync с предикатом-лямбдой.
		/// </summary>
		Task<IReadOnlyList<ToDoItem>> FindAsync(ToDoUser user, string namePrefix, CancellationToken ct);
	}
}
