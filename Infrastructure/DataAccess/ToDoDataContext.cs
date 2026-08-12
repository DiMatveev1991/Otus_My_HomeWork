using Core.DataAccess.Models;
using Infrastructure.DataAccess.Models;
using LinqToDB;
using LinqToDB.Data;
using LinqToDB.DataProvider;

namespace Infrastructure.DataAccess
{
	public class ToDoDataContext : DataConnection
	{
		#pragma warning disable CS0618 // Сигнатура конструктора задана условиями домашней работы.
		public ToDoDataContext(string connectionString)
			: base(ProviderName.PostgreSQL, connectionString)
		{
		}
		#pragma warning restore CS0618

		public ITable<ToDoUserModel> ToDoUsers => this.GetTable<ToDoUserModel>();
		public ITable<ToDoListModel> ToDoLists => this.GetTable<ToDoListModel>();
		public ITable<ToDoItemModel> ToDoItems => this.GetTable<ToDoItemModel>();
		public ITable<NotificationModel> Notifications => this.GetTable<NotificationModel>();
	}
}
