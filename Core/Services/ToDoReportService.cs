using System;
using System.Threading;
using System.Threading.Tasks;
using Core.DataAccess;

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
		public async Task<(int total, int completed, int active, DateTime generatedAt)> GetUserStatsAsync(
			Guid userId, CancellationToken ct)
		{
			var all = await _toDoRepository.GetAllByUserIdAsync(userId, ct);
			var active = await _toDoRepository.CountActiveAsync(userId, ct);
			var total = all.Count;
			var completed = total - active;

			return (total, completed, active, DateTime.UtcNow);
		}
	}
}