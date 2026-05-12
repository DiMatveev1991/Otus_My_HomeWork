using Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Otus_My_HomeWork.Services.Interfaces
{
	public interface IUserService
	{
		ToDoUser RegisterUser(long telegramUserId, string telegramUserName);
		ToDoUser? GetUser(long telegramUserId);
	}
}
