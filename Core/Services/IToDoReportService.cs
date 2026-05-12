using System;

namespace Core.Services
{
	public interface IToDoReportService
	{
		/// <summary>
		/// Возвращает статистику по задачам пользователя в виде кортежа.
		/// </summary>
		(int total, int completed, int active, DateTime generatedAt) GetUserStats(Guid userId);
	}
}
