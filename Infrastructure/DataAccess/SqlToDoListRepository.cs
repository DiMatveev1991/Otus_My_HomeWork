using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Core.DataAccess;
using Core.Entities;
using LinqToDB;

namespace Infrastructure.DataAccess
{
	public class SqlToDoListRepository : IToDoListRepository
	{
		private readonly IDataContextFactory<ToDoDataContext> _factory;

		public SqlToDoListRepository(IDataContextFactory<ToDoDataContext> factory)
		{
			_factory = factory ?? throw new ArgumentNullException(nameof(factory));
		}

		public async Task<ToDoList?> Get(Guid id, CancellationToken ct)
		{
			using var dbContext = _factory.CreateDataContext();
			var model = await dbContext.ToDoLists
				.LoadWith(list => list.User)
				.FirstOrDefaultAsync(list => list.Id == id, ct);

			return model is null ? null : ModelMapper.MapFromModel(model);
		}

		public async Task<IReadOnlyList<ToDoList>> GetByUserId(
			Guid userId, CancellationToken ct)
		{
			using var dbContext = _factory.CreateDataContext();
			var models = await dbContext.ToDoLists
				.LoadWith(list => list.User)
				.Where(list => list.UserId == userId)
				.OrderBy(list => list.CreatedAt)
				.ThenBy(list => list.Id)
				.ToListAsync(ct);

			return models.Select(ModelMapper.MapFromModel).ToList();
		}

		public async Task Add(ToDoList list, CancellationToken ct)
		{
			ArgumentNullException.ThrowIfNull(list);
			using var dbContext = _factory.CreateDataContext();
			await dbContext.InsertAsync(ModelMapper.MapToModel(list), token: ct);
		}

		public async Task Delete(Guid id, CancellationToken ct)
		{
			using var dbContext = _factory.CreateDataContext();
			using var transaction = dbContext.BeginTransaction();

			// ListId допускает null, поэтому задачи сохраняются после удаления списка.
			await dbContext.ToDoItems
				.Where(item => item.ListId == id)
				.Set(item => item.ListId, (Guid?)null)
				.UpdateAsync(ct);

			await dbContext.ToDoLists
				.Where(list => list.Id == id)
				.DeleteAsync(ct);

			transaction.Commit();
		}

		public async Task<bool> ExistsByName(
			Guid userId, string name, CancellationToken ct)
		{
			ArgumentException.ThrowIfNullOrWhiteSpace(name);
			var normalizedName = name.Trim().ToLowerInvariant();
			using var dbContext = _factory.CreateDataContext();

			return await dbContext.ToDoLists.AnyAsync(
				list => list.UserId == userId && list.Name.ToLower() == normalizedName,
				ct);
		}
	}
}
