using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Core.Services;
using Infrastructure.DataAccess;
using Infrastructure.DataAccess.Models;
using LinqToDB;
using LinqToDB.Async;
using Npgsql;

namespace Infrastructure
{
	public class NotificationService : INotificationService
	{
		private readonly IDataContextFactory<ToDoDataContext> _factory;

		public NotificationService(IDataContextFactory<ToDoDataContext> factory)
		{
			_factory = factory ?? throw new ArgumentNullException(nameof(factory));
		}

		public async Task<bool> ScheduleNotification(
			Guid userId,
			string type,
			string text,
			DateTime scheduledAt,
			CancellationToken ct)
		{
			ArgumentException.ThrowIfNullOrWhiteSpace(type);
			ArgumentException.ThrowIfNullOrWhiteSpace(text);

			var model = new NotificationModel
			{
				Id = Guid.NewGuid(),
				UserId = userId,
				Type = type,
				Text = text,
				ScheduledAt = scheduledAt,
				IsNotified = false,
				NotifiedAt = null
			};

			using var dbContext = _factory.CreateDataContext();
			try
			{
				await dbContext.InsertAsync(model, token: ct);
				return true;
			}
			catch (Exception ex) when (IsUniqueViolation(ex))
			{
				return false;
			}
		}

		public async Task<IReadOnlyList<Core.Entities.Notification>> GetScheduledNotification(
			DateTime scheduledBefore,
			CancellationToken ct)
		{
			using var dbContext = _factory.CreateDataContext();
			var models = await dbContext.Notifications
				.LoadWith(notification => notification.User)
				.Where(notification =>
					!notification.IsNotified && notification.ScheduledAt <= scheduledBefore)
				.OrderBy(notification => notification.ScheduledAt)
				.ThenBy(notification => notification.Id)
				.ToListAsync(ct);

			return models.Select(ModelMapper.MapFromModel).ToList();
		}

		public async Task MarkNotified(Guid notificationId, CancellationToken ct)
		{
			using var dbContext = _factory.CreateDataContext();
			var notifiedAt = DateTime.UtcNow;
			await dbContext.Notifications
				.Where(notification =>
					notification.Id == notificationId && !notification.IsNotified)
				.Set(notification => notification.IsNotified, true)
				.Set(notification => notification.NotifiedAt, (DateTime?)notifiedAt)
				.UpdateAsync(ct);
		}

		private static bool IsUniqueViolation(Exception exception)
		{
			for (Exception? current = exception; current is not null; current = current.InnerException)
			{
				if (current is PostgresException postgresException &&
					postgresException.SqlState == PostgresErrorCodes.UniqueViolation)
				{
					return true;
				}
			}

			return false;
		}
	}
}
