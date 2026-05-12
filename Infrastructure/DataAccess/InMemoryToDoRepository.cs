using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Core.DataAccess;
using Core.Entities;
using Core.Enums;

namespace Infrastructure.DataAccess
{
	public class InMemoryToDoRepository : IToDoRepository
	{
		// По заданию хранилище - List
		private readonly List<ToDoItem> _items = new();

		// Защита List от одновременной модификации/чтения.
		// SemaphoreSlim, а не lock, чтобы можно было использовать совместно с async/await.
		private readonly SemaphoreSlim _gate = new(1, 1);

		public async Task<IReadOnlyList<ToDoItem>> GetAllByUserIdAsync(Guid userId, CancellationToken ct)
		{
			await _gate.WaitAsync(ct);
			try
			{
				return _items.Where(i => i.User.UserId == userId).ToList();
			}
			finally { _gate.Release(); }
		}

		public async Task<IReadOnlyList<ToDoItem>> GetActiveByUserIdAsync(Guid userId, CancellationToken ct)
		{
			await _gate.WaitAsync(ct);
			try
			{
				return _items
					.Where(i => i.User.UserId == userId && i.State == ToDoItemState.Active)
					.ToList();
			}
			finally { _gate.Release(); }
		}

		public async Task<ToDoItem?> GetAsync(Guid id, CancellationToken ct)
		{
			await _gate.WaitAsync(ct);
			try
			{
				return _items.FirstOrDefault(i => i.Id == id);
			}
			finally { _gate.Release(); }
		}

		public async Task AddAsync(ToDoItem item, CancellationToken ct)
		{
			if (item == null) throw new ArgumentNullException(nameof(item));

			await _gate.WaitAsync(ct);
			try
			{
				_items.Add(item);
			}
			finally { _gate.Release(); }
		}

		public async Task UpdateAsync(ToDoItem item, CancellationToken ct)
		{
			if (item == null) throw new ArgumentNullException(nameof(item));

			await _gate.WaitAsync(ct);
			try
			{
				var index = _items.FindIndex(i => i.Id == item.Id);
				if (index >= 0)
					_items[index] = item;
			}
			finally { _gate.Release(); }
		}

		public async Task DeleteAsync(Guid id, CancellationToken ct)
		{
			await _gate.WaitAsync(ct);
			try
			{
				_items.RemoveAll(i => i.Id == id);
			}
			finally { _gate.Release(); }
		}

		public async Task<bool> ExistsByNameAsync(Guid userId, string name, CancellationToken ct)
		{
			await _gate.WaitAsync(ct);
			try
			{
				return _items.Any(i =>
					i.User.UserId == userId &&
					i.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
			}
			finally { _gate.Release(); }
		}

		public async Task<int> CountActiveAsync(Guid userId, CancellationToken ct)
		{
			await _gate.WaitAsync(ct);
			try
			{
				return _items.Count(i =>
					i.User.UserId == userId &&
					i.State == ToDoItemState.Active);
			}
			finally { _gate.Release(); }
		}

		// Поиск по предикату (лямбде)
		public async Task<IReadOnlyList<ToDoItem>> FindAsync(
			Guid userId, Func<ToDoItem, bool> predicate, CancellationToken ct)
		{
			if (predicate == null) throw new ArgumentNullException(nameof(predicate));

			await _gate.WaitAsync(ct);
			try
			{
				return _items
					.Where(i => i.User.UserId == userId)
					.Where(predicate)
					.ToList();
			}
			finally { _gate.Release(); }
		}
	}
}
