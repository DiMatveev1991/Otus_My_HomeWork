using System;

namespace Infrastructure.DataAccess
{
	public class DataContextFactory : IDataContextFactory<ToDoDataContext>
	{
		private readonly string _connectionString;

		public DataContextFactory(string connectionString)
		{
			if (string.IsNullOrWhiteSpace(connectionString))
				throw new ArgumentException("Строка подключения не может быть пустой.", nameof(connectionString));

			_connectionString = connectionString;
		}

		public ToDoDataContext CreateDataContext() => new(_connectionString);
	}
}
