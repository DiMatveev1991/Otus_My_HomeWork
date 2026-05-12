using System;
using Core.DataAccess;
using Core.Enums;

namespace Core.Services
{
	public class ToDoReportService : IToDoReportService
	{
		private readonly IToDoRepository _toDoRepository;

		public ToDoReportService(IToDoRepository toDoRepository)
		{
			_toDoRepository = toDoRepository ?? throw new ArgumentNullException(nameof(toDoRepository));
		}

		// Возвращает кортеж со статистикой пользователя
		public (int total, int completed, int active, DateTime generatedAt) GetUserStats(Guid userId)
		{
			var all = _toDoRepository.GetAllByUserId(userId);
			var active = _toDoRepository.CountActive(userId);
			var total = all.Count;
			var completed = total - active;

			return (total, completed, active, DateTime.UtcNow);
		}
	}
}
