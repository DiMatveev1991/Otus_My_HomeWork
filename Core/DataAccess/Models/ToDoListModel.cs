using System;
using LinqToDB.Mapping;

namespace Core.DataAccess.Models
{
	[Table("ToDoList")]
	public class ToDoListModel
	{
		[PrimaryKey, Column("Id")]
		public Guid Id { get; set; }

		[Column("UserId"), NotNull]
		public Guid UserId { get; set; }

		[Column("Name"), NotNull]
		public string Name { get; set; } = string.Empty;

		[Column("CreatedAt"), NotNull]
		public DateTime CreatedAt { get; set; }

		[Association(ThisKey = nameof(UserId), OtherKey = nameof(ToDoUserModel.UserId), CanBeNull = false)]
		public ToDoUserModel User { get; set; } = null!;
	}
}
