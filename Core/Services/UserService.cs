using System;
using System.Threading;
using System.Threading.Tasks;
using Core.DataAccess;
using Core.Entities;

namespace Core.Services
{
	public class UserService : IUserService
	{
		private readonly IUserRepository _userRepository;

		// Получаем репозиторий через конструктор (DI)
		public UserService(IUserRepository userRepository)
		{
			_userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
		}

		public async Task<ToDoUser> RegisterUserAsync(
			long telegramUserId, string telegramUserName, CancellationToken ct)
		{
			var user = new ToDoUser(telegramUserId, telegramUserName);
			await _userRepository.AddAsync(user, ct);
			return user;
		}

		public async Task<ToDoUser?> GetUserAsync(long telegramUserId, CancellationToken ct)
		{
			return await _userRepository.GetUserByTelegramUserIdAsync(telegramUserId, ct);
		}
	}
}
