using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Core.DataAccess;
using Core.Services;

namespace BackgroundTasks
{
	public class TodayBackgroundTask(
		INotificationService notificationService,
		IUserRepository userRepository,
		IToDoRepository toDoRepository)
		: BackgroundTask(TimeSpan.FromDays(1), nameof(TodayBackgroundTask))
	{
		protected override async Task Execute(CancellationToken ct)
		{
			var now = DateTime.UtcNow;
			var today = now.Date;
			var tomorrow = today.AddDays(1);
			var users = await userRepository.GetUsers(ct);

			foreach (var user in users)
			{
				ct.ThrowIfCancellationRequested();
				var tasks = await toDoRepository.GetActiveWithDeadline(
					user.UserId, today, tomorrow, ct);

				if (tasks.Count == 0)
					continue;

				var taskList = string.Join(
					Environment.NewLine,
					tasks.Select(task => $"- {task.Name}"));

				await notificationService.ScheduleNotification(
					user.UserId,
					$"Today_{DateOnly.FromDateTime(now)}",
					$"Задачи на сегодня:{Environment.NewLine}{taskList}",
					now,
					ct);
			}
		}
	}
}
