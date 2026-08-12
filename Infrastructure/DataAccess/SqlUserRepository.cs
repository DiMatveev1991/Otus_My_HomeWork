using System;
using System.Threading;
using System.Threading.Tasks;
using Core.DataAccess;
using Core.Entities;
using LinqToDB;

namespace Infrastructure.DataAccess
{
	public class SqlUserRepository : IUserRepository
	{
		private readonly IDataContextFactory<ToDoDataContext> _factory;

		public SqlUserRepository(IDataContextFactory<ToDoDataContext> factory)
		{
			_factory = factory ?? throw new ArgumentNullException(nameof(factory));
		}

		public async Task<ToDoUser?> GetUserAsync(Guid userId, CancellationToken ct)
		{
			using var dbContext = _factory.CreateDataContext();
			var model = await dbContext.ToDoUsers
				.FirstOrDefaultAsync(user => user.UserId == userId, ct);

			return model is null ? null : ModelMapper.MapFromModel(model);
		}

		public async Task<ToDoUser?> GetUserByTelegramUserIdAsync(
			long telegramUserId, CancellationToken ct)
		{
			using var dbContext = _factory.CreateDataContext();
			var model = await dbContext.ToDoUsers
				.FirstOrDefaultAsync(user => user.TelegramUserId == telegramUserId, ct);

			return model is null ? null : ModelMapper.MapFromModel(model);
		}

		public async Task AddAsync(ToDoUser user, CancellationToken ct)
		{
			ArgumentNullException.ThrowIfNull(user);
			using var dbContext = _factory.CreateDataContext();
			await dbContext.InsertAsync(ModelMapper.MapToModel(user), token: ct);
		}
	}
}
