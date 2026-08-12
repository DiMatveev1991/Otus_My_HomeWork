using System;
using System.Threading;
using System.Threading.Tasks;
using Core.DataAccess;
using Core.Services;

namespace BackgroundTasks
{
	public class DeadlineBackgroundTask(
		INotificationService notificationService,
		IUserRepository userRepository,
		IToDoRepository toDoRepository)
		: BackgroundTask(TimeSpan.FromHours(1), nameof(DeadlineBackgroundTask))
	{
		protected override async Task Execute(CancellationToken ct)
		{
			var now = DateTime.UtcNow;
			var from = now.AddDays(-1).Date;
			var to = now.Date;
			var users = await userRepository.GetUsers(ct);

			foreach (var user in users)
			{
				ct.ThrowIfCancellationRequested();
				var tasks = await toDoRepository.GetActiveWithDeadline(
					user.UserId, from, to, ct);

				foreach (var task in tasks)
				{
					await notificationService.ScheduleNotification(
						user.UserId,
						$"Dealine_{task.Id}",
						$"Ой! Вы пропустили дедлайн по задаче {task.Name}",
						now,
						ct);
				}
			}
		}
	}
}
