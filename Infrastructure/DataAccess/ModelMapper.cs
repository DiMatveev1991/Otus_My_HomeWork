using System;
using Core.DataAccess.Models;
using Core.Entities;

namespace Infrastructure.DataAccess
{
	internal static class ModelMapper
	{
		public static ToDoUser MapFromModel(ToDoUserModel model)
		{
			ArgumentNullException.ThrowIfNull(model);

			return new ToDoUser
			{
				UserId = model.UserId,
				TelegramUserId = model.TelegramUserId,
				TelegramUserName = model.TelegramUserName,
				RegisteredAt = model.RegisteredAt
			};
		}

		public static ToDoUserModel MapToModel(ToDoUser entity)
		{
			ArgumentNullException.ThrowIfNull(entity);

			return new ToDoUserModel
			{
				UserId = entity.UserId,
				TelegramUserId = entity.TelegramUserId,
				TelegramUserName = entity.TelegramUserName,
				RegisteredAt = entity.RegisteredAt
			};
		}

		public static ToDoItem MapFromModel(ToDoItemModel model)
		{
			ArgumentNullException.ThrowIfNull(model);
			var user = MapFromModel(model.User
				?? throw new InvalidOperationException("Пользователь задачи не был загружен."));

			return new ToDoItem
			{
				Id = model.Id,
				User = user,
				Name = model.Name,
				CreatedAt = model.CreatedAt,
				Deadline = model.Deadline,
				List = model.List is null ? null : MapList(model.List, user),
				State = model.State,
				StateChangedAt = model.StateChangedAt
			};
		}

		public static ToDoItemModel MapToModel(ToDoItem entity)
		{
			ArgumentNullException.ThrowIfNull(entity);

			return new ToDoItemModel
			{
				Id = entity.Id,
				UserId = entity.User.UserId,
				Name = entity.Name,
				CreatedAt = entity.CreatedAt,
				Deadline = entity.Deadline,
				ListId = entity.List?.Id,
				State = entity.State,
				StateChangedAt = entity.StateChangedAt,
				User = MapToModel(entity.User),
				List = entity.List is null ? null : MapToModel(entity.List)
			};
		}

		public static ToDoList MapFromModel(ToDoListModel model)
		{
			ArgumentNullException.ThrowIfNull(model);
			var user = MapFromModel(model.User
				?? throw new InvalidOperationException("Пользователь списка не был загружен."));

			return MapList(model, user);
		}

		public static ToDoListModel MapToModel(ToDoList entity)
		{
			ArgumentNullException.ThrowIfNull(entity);

			return new ToDoListModel
			{
				Id = entity.Id,
				UserId = entity.User.UserId,
				Name = entity.Name,
				CreatedAt = entity.CreatedAt,
				User = MapToModel(entity.User)
			};
		}

		private static ToDoList MapList(ToDoListModel model, ToDoUser fallbackUser)
		{
			return new ToDoList
			{
				Id = model.Id,
				Name = model.Name,
				User = model.User is null ? fallbackUser : MapFromModel(model.User),
				CreatedAt = model.CreatedAt
			};
		}
	}
}
