using System;
using System.Threading;
using System.Threading.Tasks;

namespace Core.Services
{
	public interface IToDoReportService
	{
		/// <summary>
		/// Возвращает статистику по задачам пользователя в виде кортежа.
		/// </summary>
		Task<(int total, int completed, int active, DateTime generatedAt)> GetUserStatsAsync(
			Guid userId, CancellationToken ct);
	}
}
