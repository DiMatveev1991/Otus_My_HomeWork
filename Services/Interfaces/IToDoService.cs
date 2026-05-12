using Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Otus_My_HomeWork.Services.Interfaces
{
	public interface IToDoService
	{
		IReadOnlyList<ToDoItem> GetAllByUserId(Guid userId);
		/// <summary>Возвращает ToDoItem для UserId со статусом Active</summary>
		IReadOnlyList<ToDoItem> GetActiveByUserId(Guid userId);
		ToDoItem Add(ToDoUser user, string name);
		void MarkCompleted(Guid id);
		void Delete(Guid id);
	}
}
