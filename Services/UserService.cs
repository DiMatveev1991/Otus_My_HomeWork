using Models;
using Otus_My_HomeWork.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Otus_My_HomeWork.Services
{
	public class UserService : IUserService
	{
		private readonly Dictionary<long, ToDoUser> _users = new();

		public ToDoUser RegisterUser(long telegramUserId, string telegramUserName)
		{
			var user = new ToDoUser(telegramUserId, telegramUserName);
			_users[telegramUserId] = user;
			return user;
		}

		public ToDoUser? GetUser(long telegramUserId)
		{
			_users.TryGetValue(telegramUserId, out var user);
			return user;
		}
	}
}
