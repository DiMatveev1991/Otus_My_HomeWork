using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Core.DataAccess;
using Core.Entities;

namespace Infrastructure.DataAccess
{
	public class InMemoryUserRepository : IUserRepository
	{
		// По заданию хранилище - List
		private readonly List<ToDoUser> _users = new();

		// Защита List от одновременной модификации/чтения.
		// SemaphoreSlim, а не lock, чтобы можно было использовать совместно с async/await.
		private readonly SemaphoreSlim _gate = new(1, 1);

		public async Task<ToDoUser?> GetUserAsync(Guid userId, CancellationToken ct)
		{
			await _gate.WaitAsync(ct);
			try
			{
				return _users.FirstOrDefault(u => u.UserId == userId);
			}
			finally { _gate.Release(); }
		}

		public async Task<ToDoUser?> GetUserByTelegramUserIdAsync(long telegramUserId, CancellationToken ct)
		{
			await _gate.WaitAsync(ct);
			try
			{
				return _users.FirstOrDefault(u => u.TelegramUserId == telegramUserId);
			}
			finally { _gate.Release(); }
		}

		public async Task AddAsync(ToDoUser user, CancellationToken ct)
		{
			if (user == null) throw new ArgumentNullException(nameof(user));

			await _gate.WaitAsync(ct);
			try
			{
				_users.Add(user);
			}
			finally { _gate.Release(); }
		}
	}
}