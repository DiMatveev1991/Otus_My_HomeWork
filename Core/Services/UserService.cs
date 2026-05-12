using System;
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

		public ToDoUser RegisterUser(long telegramUserId, string telegramUserName)
		{
			var user = new ToDoUser(telegramUserId, telegramUserName);
			_userRepository.Add(user);
			return user;
		}

		public ToDoUser? GetUser(long telegramUserId)
		{
			return _userRepository.GetUserByTelegramUserId(telegramUserId);
		}
	}
}
