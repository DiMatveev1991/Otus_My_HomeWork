using System;
using System.Collections.Generic;
using System.Linq;
using Core.DataAccess;
using Core.Entities;
using Core.Enums;

namespace Infrastructure.DataAccess
{
	public class InMemoryToDoRepository : IToDoRepository
	{
		// По заданию хранилище - List
		private readonly List<ToDoItem> _items = new();

		public IReadOnlyList<ToDoItem> GetAllByUserId(Guid userId)
		{
			return _items.Where(i => i.User.UserId == userId).ToList();
		}

		public IReadOnlyList<ToDoItem> GetActiveByUserId(Guid userId)
		{
			return _items
				.Where(i => i.User.UserId == userId && i.State == ToDoItemState.Active)
				.ToList();
		}

		public ToDoItem? Get(Guid id)
		{
			return _items.FirstOrDefault(i => i.Id == id);
		}

		public void Add(ToDoItem item)
		{
			if (item == null) throw new ArgumentNullException(nameof(item));
			_items.Add(item);
		}

		public void Update(ToDoItem item)
		{
			if (item == null) throw new ArgumentNullException(nameof(item));
			// В in-memory реализации объект изменяется по ссылке.
			// Метод оставляем для согласованности контракта.
			var index = _items.FindIndex(i => i.Id == item.Id);
			if (index >= 0)
				_items[index] = item;
		}

		public void Delete(Guid id)
		{
			_items.RemoveAll(i => i.Id == id);
		}

		public bool ExistsByName(Guid userId, string name)
		{
			return _items.Any(i =>
				i.User.UserId == userId &&
				i.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
		}

		public int CountActive(Guid userId)
		{
			return _items.Count(i =>
				i.User.UserId == userId &&
				i.State == ToDoItemState.Active);
		}

		// Поиск по предикату (лямбде)
		public IReadOnlyList<ToDoItem> Find(Guid userId, Func<ToDoItem, bool> predicate)
		{
			if (predicate == null) throw new ArgumentNullException(nameof(predicate));

			return _items
				.Where(i => i.User.UserId == userId)
				.Where(predicate)
				.ToList();
		}
	}
}
