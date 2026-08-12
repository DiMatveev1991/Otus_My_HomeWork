using System;
using LinqToDB.Mapping;

namespace Core.DataAccess.Models
{
	[Table("ToDoUser")]
	public class ToDoUserModel
	{
		[PrimaryKey, Column("UserId")]
		public Guid UserId { get; set; }

		[Column("TelegramUserId"), NotNull]
		public long TelegramUserId { get; set; }

		[Column("TelegramUserName"), NotNull]
		public string TelegramUserName { get; set; } = string.Empty;

		[Column("RegisteredAt"), NotNull]
		public DateTime RegisteredAt { get; set; }
	}
}
