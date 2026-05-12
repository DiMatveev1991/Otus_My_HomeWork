using System;
using System.Collections.Generic;
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

		// lock для атомарности операций "проверил-добавил" / "проверил-удалил"
		// Нужен потому что новый ConsoleBotClient вызывает HandleUpdateAsync
		// через Task.Run, т.е. обработчики могут идти параллельно.
		private readonly object _sync = new();

		public AutoPartsToDoService(IToDoRepository toDoRepository,
			int maxTaskCount = 10, int maxTaskLength = 100)
		{
			_toDoRepository = toDoRepository ?? throw new ArgumentNullException(nameof(toDoRepository));
			_maxTaskCount = maxTaskCount;
			_maxTaskLength = maxTaskLength;
		}

		public IReadOnlyList<ToDoItem> GetAllByUserId(Guid userId)
		{
			lock (_sync)
			{
				return _toDoRepository.GetAllByUserId(userId);
			}
		}

		public IReadOnlyList<ToDoItem> GetActiveByUserId(Guid userId)
		{
			lock (_sync)
			{
				return _toDoRepository.GetActiveByUserId(userId);
			}
		}

		public ToDoItem Add(ToDoUser user, string name)
		{
			if (user == null) throw new ArgumentNullException(nameof(user));
			if (string.IsNullOrWhiteSpace(name))
				throw new ArgumentException("Имя задачи не может быть пустым", nameof(name));

			// Проверка длины не требует доступа к хранилищу — вне lock
			if (name.Length > _maxTaskLength)
				throw new TaskLengthLimitException(name.Length, _maxTaskLength);

			lock (_sync)
			{
				// Все три шага (count, exists, add) должны быть атомарны
				if (_toDoRepository.CountActive(user.UserId) >= _maxTaskCount)
					throw new TaskCountLimitException(_maxTaskCount);

				if (_toDoRepository.ExistsByName(user.UserId, name))
					throw new DuplicateTaskException(name);

				var item = new ToDoItem(user, name);
				_toDoRepository.Add(item);
				return item;
			}
		}

		public void MarkCompleted(Guid id)
		{
			lock (_sync)
			{
				var item = _toDoRepository.Get(id) ?? throw new TaskNotFoundException(id);
				item.MarkAsCompleted();
				_toDoRepository.Update(item);
			}
		}

		public void Delete(Guid id)
		{
			lock (_sync)
			{
				var item = _toDoRepository.Get(id) ?? throw new TaskNotFoundException(id);
				_toDoRepository.Delete(id);
			}
		}

		// Поиск задач по префиксу имени. Использует IToDoRepository.Find с лямбдой.
		public IReadOnlyList<ToDoItem> Find(ToDoUser user, string namePrefix)
		{
			if (user == null) throw new ArgumentNullException(nameof(user));
			namePrefix ??= string.Empty;

			lock (_sync)
			{
				return _toDoRepository.Find(
					user.UserId,
					item => item.Name.StartsWith(namePrefix, StringComparison.OrdinalIgnoreCase));
			}
		}
	}
}