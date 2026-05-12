using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Core.DataAccess;
using Core.Entities;
using Core.Exceptions;

namespace Core.Services
{
	// Сервис управления заказами автозапчастей
	public class AutoPartsToDoService : IToDoService
	{
		private readonly IToDoRepository _toDoRepository;
		private readonly int _maxTaskCount;
		private readonly int _maxTaskLength;

		// SemaphoreSlim вместо lock — потому что внутри секции есть await
		// (lock не работает с await). Назначение то же: сделать атомарными
		// операции "проверил-добавил" / "проверил-удалил" в условиях, когда
		// HandleUpdateAsync вызывается параллельно через Task.Run.
		private readonly SemaphoreSlim _gate = new(1, 1);

		public AutoPartsToDoService(IToDoRepository toDoRepository,
			int maxTaskCount = 10, int maxTaskLength = 100)
		{
			_toDoRepository = toDoRepository ?? throw new ArgumentNullException(nameof(toDoRepository));
			_maxTaskCount = maxTaskCount;
			_maxTaskLength = maxTaskLength;
		}

		public async Task<IReadOnlyList<ToDoItem>> GetAllByUserIdAsync(Guid userId, CancellationToken ct)
		{
			return await _toDoRepository.GetAllByUserIdAsync(userId, ct);
		}

		public async Task<IReadOnlyList<ToDoItem>> GetActiveByUserIdAsync(Guid userId, CancellationToken ct)
		{
			return await _toDoRepository.GetActiveByUserIdAsync(userId, ct);
		}

		public async Task<ToDoItem> AddAsync(ToDoUser user, string name, CancellationToken ct)
		{
			if (user == null) throw new ArgumentNullException(nameof(user));
			if (string.IsNullOrWhiteSpace(name))
				throw new ArgumentException("Имя задачи не может быть пустым", nameof(name));

			// Проверка длины не требует доступа к хранилищу — вне семафора
			if (name.Length > _maxTaskLength)
				throw new TaskLengthLimitException(name.Length, _maxTaskLength);

			await _gate.WaitAsync(ct);
			try
			{
				// Все три шага должны быть атомарны
				if (await _toDoRepository.CountActiveAsync(user.UserId, ct) >= _maxTaskCount)
					throw new TaskCountLimitException(_maxTaskCount);

				if (await _toDoRepository.ExistsByNameAsync(user.UserId, name, ct))
					throw new DuplicateTaskException(name);

				var item = new ToDoItem(user, name);
				await _toDoRepository.AddAsync(item, ct);
				return item;
			}
			finally { _gate.Release(); }
		}

		public async Task MarkCompletedAsync(Guid id, CancellationToken ct)
		{
			await _gate.WaitAsync(ct);
			try
			{
				var item = await _toDoRepository.GetAsync(id, ct)
					?? throw new TaskNotFoundException(id);
				item.MarkAsCompleted();
				await _toDoRepository.UpdateAsync(item, ct);
			}
			finally { _gate.Release(); }
		}

		public async Task DeleteAsync(Guid id, CancellationToken ct)
		{
			await _gate.WaitAsync(ct);
			try
			{
				var item = await _toDoRepository.GetAsync(id, ct)
					?? throw new TaskNotFoundException(id);
				await _toDoRepository.DeleteAsync(id, ct);
			}
			finally { _gate.Release(); }
		}

		// Поиск задач по префиксу имени. Использует IToDoRepository.FindAsync с лямбдой.
		public async Task<IReadOnlyList<ToDoItem>> FindAsync(
			ToDoUser user, string namePrefix, CancellationToken ct)
		{
			if (user == null) throw new ArgumentNullException(nameof(user));
			namePrefix ??= string.Empty;

			return await _toDoRepository.FindAsync(
				user.UserId,
				item => item.Name.StartsWith(namePrefix, StringComparison.OrdinalIgnoreCase),
				ct);
		}
	}
}