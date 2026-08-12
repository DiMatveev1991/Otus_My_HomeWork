using LinqToDB.Data;

namespace Infrastructure.DataAccess
{
	public interface IDataContextFactory<out TDataContext>
		where TDataContext : DataConnection
	{
		TDataContext CreateDataContext();
	}
}
