using System;

 namespace Exeptions
{
	public class DuplicateTaskException : Exception
	{
		public DuplicateTaskException(string task)
			: base($"Задача '{task}' уже существует")
		{
		}
	}
}
