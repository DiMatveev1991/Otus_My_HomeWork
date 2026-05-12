using System;

namespace Core.Exceptions
{
	public class TaskNotFoundException : Exception
	{
		public TaskNotFoundException(Guid taskId)
			: base($"Задача с ID '{taskId}' не найдена")
		{
		}
	}
}
