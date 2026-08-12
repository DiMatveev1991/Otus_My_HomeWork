using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Core.DataAccess;
using Core.DataAccess.Models;
using Core.Entities;
using Core.Enums;
using LinqToDB;
using LinqToDB.Async;

namespace Infrastructure.DataAccess
{
	public class SqlToDoRepository : IToDoRepository
	{
		private readonly IDataContextFactory<ToDoDataContext> _factory;

		public SqlToDoRepository(IDataContextFactory<ToDoDataContext> factory)
		{
			_factory = factory ?? throw new ArgumentNullException(nameof(factory));
		}

		public async Task<IReadOnlyList<ToDoItem>> GetAllByUserIdAsync(
			Guid userId, CancellationToken ct)
		{
			using var dbContext = _factory.CreateDataContext();
			var models = await WithAssociations(dbContext)
				.Where(item => item.UserId == userId)
				.OrderBy(item => item.CreatedAt)
				.ThenBy(item => item.Id)
				.ToListAsync(ct);

			return models.Select(ModelMapper.MapFromModel).ToList();
		}

		public async Task<IReadOnlyList<ToDoItem>> GetActiveByUserIdAsync(
			Guid userId, CancellationToken ct)
		{
			using var dbContext = _factory.CreateDataContext();
			var models = await WithAssociations(dbContext)
				.Where(item => item.UserId == userId && item.State == ToDoItemState.Active)
				.OrderBy(item => item.CreatedAt)
				.ThenBy(item => item.Id)
				.ToListAsync(ct);

			return models.Select(ModelMapper.MapFromModel).ToList();
		}

		public async Task<ToDoItem?> GetAsync(Guid id, CancellationToken ct)
		{
			using var dbContext = _factory.CreateDataContext();
			var model = await WithAssociations(dbContext)
				.FirstOrDefaultAsync(item => item.Id == id, ct);

			return model is null ? null : ModelMapper.MapFromModel(model);
		}

		public async Task AddAsync(ToDoItem item, CancellationToken ct)
		{
			ArgumentNullException.ThrowIfNull(item);
			using var dbContext = _factory.CreateDataContext();
			await dbContext.InsertAsync(ModelMapper.MapToModel(item), token: ct);
		}

		public async Task UpdateAsync(ToDoItem item, CancellationToken ct)
		{
			ArgumentNullException.ThrowIfNull(item);
			using var dbContext = _factory.CreateDataContext();
			await dbContext.UpdateAsync(ModelMapper.MapToModel(item), token: ct);
		}

		public async Task DeleteAsync(Guid id, CancellationToken ct)
		{
			using var dbContext = _factory.CreateDataContext();
			await dbContext.ToDoItems
				.Where(item => item.Id == id)
				.DeleteAsync(ct);
		}

		public async Task<bool> ExistsByNameAsync(
			Guid userId, string name, CancellationToken ct)
		{
			ArgumentException.ThrowIfNullOrWhiteSpace(name);
			var normalizedName = name.Trim().ToLowerInvariant();
			using var dbContext = _factory.CreateDataContext();

			return await dbContext.ToDoItems.AnyAsync(
				item => item.UserId == userId && item.Name.ToLower() == normalizedName,
				ct);
		}

		public async Task<int> CountActiveAsync(Guid userId, CancellationToken ct)
		{
			using var dbContext = _factory.CreateDataContext();
			return await dbContext.ToDoItems.CountAsync(
				item => item.UserId == userId && item.State == ToDoItemState.Active,
				ct);
		}

		public async Task<IReadOnlyList<ToDoItem>> GetActiveWithDeadline(
			Guid userId,
			DateTime from,
			DateTime to,
			CancellationToken ct)
		{
			using var dbContext = _factory.CreateDataContext();
			var models = await WithAssociations(dbContext)
				.Where(item =>
					item.UserId == userId &&
					item.State == ToDoItemState.Active &&
					item.Deadline >= from &&
					item.Deadline < to)
				.OrderBy(item => item.Deadline)
				.ThenBy(item => item.Id)
				.ToListAsync(ct);

			return models.Select(ModelMapper.MapFromModel).ToList();
		}

		public async Task<IReadOnlyList<ToDoItem>> FindAsync(
			Guid userId, Func<ToDoItem, bool> predicate, CancellationToken ct)
		{
			ArgumentNullException.ThrowIfNull(predicate);
			using var dbContext = _factory.CreateDataContext();
			var models = await WithAssociations(dbContext)
				.Where(item => item.UserId == userId)
				.OrderBy(item => item.CreatedAt)
				.ThenBy(item => item.Id)
				.ToListAsync(ct);
			ct.ThrowIfCancellationRequested();

			return models.Select(ModelMapper.MapFromModel).Where(predicate).ToList();
		}

		private static IQueryable<ToDoItemModel> WithAssociations(ToDoDataContext dbContext)
		{
			return dbContext.ToDoItems
				.LoadWith(item => item.User)
				.LoadWith(item => item.List)
				.LoadWith(item => item.List!.User);
		}
	}
}
