using System;
using System.Threading;
using System.Threading.Tasks;
using Core.Services;
using Telegram.Bot;

namespace BackgroundTasks
{
	public class NotificationBackgroundTask(
		INotificationService notificationService,
		ITelegramBotClient bot)
		: BackgroundTask(TimeSpan.FromMinutes(1), nameof(NotificationBackgroundTask))
	{
		protected override async Task Execute(CancellationToken ct)
		{
			var notifications = await notificationService.GetScheduledNotification(DateTime.UtcNow, ct);

			foreach (var notification in notifications)
			{
				ct.ThrowIfCancellationRequested();

				await bot.SendMessage(
					chatId: notification.User.TelegramUserId,
					text: notification.Text,
					cancellationToken: ct);

				await notificationService.MarkNotified(notification.Id, ct);
			}
		}
	}
}
