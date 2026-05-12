using Enums;
using Exeptions;
using Models;
using Otus_My_HomeWork.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Otus_My_HomeWork.Services
{
	// Сервис управления заказами автозапчастей
	public class AutoPartsToDoService : IToDoService
	{
		private readonly List<ToDoItem> _orders = new();
		private readonly int _maxTaskCount;
		private readonly int _maxTaskLength;

		public AutoPartsToDoService(int maxTaskCount = 10, int maxTaskLength = 100)
		{
			_maxTaskCount = maxTaskCount;
			_maxTaskLength = maxTaskLength;
		}

		public IReadOnlyList<ToDoItem> GetAllByUserId(Guid userId)
		{
			return _orders.Where(t => t.User.UserId == userId).ToList();
		}

		public IReadOnlyList<ToDoItem> GetActiveByUserId(Guid userId)
		{
			return _orders
				.Where(t => t.User.UserId == userId && t.State == ToDoItemState.Active)
				.ToList();
		}

		public ToDoItem Add(ToDoUser user, string name)
		{
			var userOrders = GetAllByUserId(user.UserId);

			if (userOrders.Count >= _maxTaskCount)
				throw new TaskCountLimitException(_maxTaskCount);

			if (name.Length > _maxTaskLength)
				throw new TaskLengthLimitException(name.Length, _maxTaskLength);

			if (userOrders.Any(t =>
					t.Name.Equals(name, StringComparison.OrdinalIgnoreCase) &&
					t.State == ToDoItemState.Active))
				throw new DuplicateTaskException(name);

			var order = new ToDoItem(user, name);
			_orders.Add(order);
			return order;
		}

		public void MarkCompleted(Guid id)
		{
			var order = _orders.FirstOrDefault(t => t.Id == id)
						?? throw new TaskNotFoundException(id);
			order.MarkAsCompleted();
		}

		public void Delete(Guid id)
		{
			var order = _orders.FirstOrDefault(t => t.Id == id)
						?? throw new TaskNotFoundException(id);
			_orders.Remove(order);
		}
	}
}
