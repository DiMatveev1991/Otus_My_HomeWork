using System;

namespace Exeptions
{
	public class TaskLengthLimitException : Exception
	{
		public TaskLengthLimitException(int taskLength, int taskLengthLimit)
			: base($"Длина задачи '{taskLength}' превышает максимально допустимое значение {taskLengthLimit}")
		{
		}
	}

}