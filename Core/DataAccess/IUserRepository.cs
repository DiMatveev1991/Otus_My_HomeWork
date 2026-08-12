using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Core.Entities;

namespace Core.DataAccess
{
	public interface IUserRepository
	{
		Task<IReadOnlyList<ToDoUser>> GetUsers(CancellationToken ct);
		Task<ToDoUser?> GetUserAsync(Guid userId, CancellationToken ct);
		Task<ToDoUser?> GetUserByTelegramUserIdAsync(long telegramUserId, CancellationToken ct);
		Task AddAsync(ToDoUser user, CancellationToken ct);
	}
}
