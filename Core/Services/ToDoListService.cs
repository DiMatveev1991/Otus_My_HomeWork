using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Core.DataAccess;
using Core.Entities;

namespace Core.Services
{
	/// <summary>
	/// Сервис управления списками задач.
	/// Правила:
	///   * длина имени списка не может быть больше 10 символов;
	///   * имя списка уникально в рамках одного ToDoUser.
	/// </summary>
	public class ToDoListService : IToDoListService
	{
		private const int MaxNameLength = 10;

		private readonly IToDoListRepository _listRepository;

		// SemaphoreSlim делает атомарной операцию "проверил-добавил",
		// т.к. HandleUpdateAsync может вызываться параллельно.
		private readonly SemaphoreSlim _gate = new(1, 1);

		public ToDoListService(IToDoListRepository listRepository)
		{
			_listRepository = listRepository ?? throw new ArgumentNullException(nameof(listRepository));
		}

		public async Task<ToDoList> Add(ToDoUser user, string name, CancellationToken ct)
		{
			if (user == null) throw new ArgumentNullException(nameof(user));
			if (string.IsNullOrWhiteSpace(name))
				throw new ArgumentException("Название списка не может быть пустым", nameof(name));

			name = name.Trim();

			if (name.Length > MaxNameLength)
				throw new ArgumentException(
					$"Название списка не может быть длиннее {MaxNameLength} символов.", nameof(name));

			await _gate.WaitAsync(ct);
			try
			{
				if (await _listRepository.ExistsByName(user.UserId, name, ct))
					throw new ArgumentException($"Список с именем \"{name}\" уже существует.", nameof(name));

				var list = new ToDoList
				{
					Id = Guid.NewGuid(),
					Name = name,
					User = user,
					CreatedAt = DateTime.UtcNow
				};
				await _listRepository.Add(list, ct);
				return list;
			}
			finally { _gate.Release(); }
		}

		public Task<ToDoList?> Get(Guid id, CancellationToken ct) =>
			_listRepository.Get(id, ct);

		public Task Delete(Guid id, CancellationToken ct) =>
			_listRepository.Delete(id, ct);

		public Task<IReadOnlyList<ToDoList>> GetUserLists(Guid userId, CancellationToken ct) =>
			_listRepository.GetByUserId(userId, ct);
	}
}
