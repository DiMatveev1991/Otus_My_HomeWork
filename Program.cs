using System;
using System.Collections.Generic;
using System.Linq;

namespace AutoPartsBot
{
	// Собственные типы исключений
	public class TaskCountLimitException : Exception
	{
		public TaskCountLimitException(int taskCountLimit)
			: base($"Превышено максимальное количество задач равное {taskCountLimit}")
		{
		}
	}

	public class TaskLengthLimitException : Exception
	{
		public TaskLengthLimitException(int taskLength, int taskLengthLimit)
			: base($"Длина задачи '{taskLength}' превышает максимально допустимое значение {taskLengthLimit}")
		{
		}
	}

	public class DuplicateTaskException : Exception
	{
		public DuplicateTaskException(string task)
			: base($"Задача '{task}' уже существует")
		{
		}
	}

	class Program
	{
		// Глобальные переменные для ограничений
		private static int maxTaskCount = 10;
		private static int maxTaskLength = 50;

		// Список задач
		private static List<string> tasks = new List<string>();
		private static string userName = null;
		private static bool isStarted = false;

		static void Main(string[] args)
		{
			try
			{
				RunApplication();
			}
			catch (TaskCountLimitException ex)
			{
				Console.WriteLine(ex.Message);
				ContinueApplication();
			}
			catch (TaskLengthLimitException ex)
			{
				Console.WriteLine(ex.Message);
				ContinueApplication();
			}
			catch (DuplicateTaskException ex)
			{
				Console.WriteLine(ex.Message);
				ContinueApplication();
			}
			catch (ArgumentException ex)
			{
				Console.WriteLine(ex.Message);
				ContinueApplication();
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Произошла непредвиденная ошибка:");
				Console.WriteLine($"Type: {ex.GetType().Name}");
				Console.WriteLine($"Message: {ex.Message}");
				Console.WriteLine($"StackTrace: {ex.StackTrace}");

				if (ex.InnerException != null)
				{
					Console.WriteLine($"InnerException: {ex.InnerException.Message}");
				}

				ContinueApplication();
			}
		}

		static void RunApplication()
		{
			// Запрос максимального количества задач
			Console.WriteLine("Введите максимально допустимое количество задач (от 1 до 100):");
			string maxTaskCountInput = Console.ReadLine();
			maxTaskCount = ParseAndValidateInt(maxTaskCountInput, 1, 100);

			// Запрос максимальной длины задачи
			Console.WriteLine("Введите максимально допустимую длину задачи (от 1 до 100):");
			string maxTaskLengthInput = Console.ReadLine();
			maxTaskLength = ParseAndValidateInt(maxTaskLengthInput, 1, 100);

			// Приветственное сообщение
			Console.WriteLine("==================================================================");
			Console.WriteLine($"  AutoParts Bot v3.0 | Лимит задач: {maxTaskCount} | Лим.длина: {maxTaskLength}");
			Console.WriteLine("==================================================================");
			Console.WriteLine();
			Console.WriteLine("Доступные команды:");
			ShowAvailableCommands();
			Console.WriteLine();

			// Основной цикл программы
			MainLoop();
		}

		static void MainLoop()
		{
			while (true)
			{
				try
				{
					ProcessUserInput();
				}
				catch (TaskCountLimitException ex)
				{
					Console.WriteLine(ex.Message);
				}
				catch (TaskLengthLimitException ex)
				{
					Console.WriteLine(ex.Message);
				}
				catch (DuplicateTaskException ex)
				{
					Console.WriteLine(ex.Message);
				}
				catch (ArgumentException ex)
				{
					Console.WriteLine(ex.Message);
				}
				catch (Exception ex)
				{
					Console.WriteLine($"Произошла непредвиденная ошибка: {ex.Message}");
				}
			}
		}

		static void ProcessUserInput()
		{
			// Вывод приглашения для ввода
			if (string.IsNullOrEmpty(userName))
			{
				Console.Write("Введите команду: ");
			}
			else
			{
				Console.Write($"{userName}, введите команду: ");
			}

			// Получение ввода от пользователя
			string input = Console.ReadLine();

			// Проверка на пустой ввод
			ValidateString(input);

			// Приведение команды к нижнему регистру для удобства сравнения
			string command = input.Trim().ToLower();

			// Обработка команд
			switch (command)
			{
				case "/exit":
					HandleExitCommand();
					break;
				case "/start":
					HandleStartCommand();
					break;
				case "/help":
					HandleHelpCommand();
					break;
				case "/info":
					HandleInfoCommand();
					break;
				case string s when s.StartsWith("/echo"):
					HandleEchoCommand(input);
					break;
				case "/addtask":
					HandleAddTaskCommand();
					break;
				case "/showtasks":
					HandleShowTasksCommand();
					break;
				case "/removetask":
					HandleRemoveTaskCommand();
					break;
				default:
					HandleUnknownCommand(input);
					break;
			}
		}

		// Методы обработки команд
		static void HandleExitCommand()
		{
			if (!string.IsNullOrEmpty(userName))
			{
				Console.WriteLine($"До свидания, {userName}! Ваш список задач сохранен.");
				if (tasks.Count > 0)
				{
					Console.WriteLine($"Всего задач: {tasks.Count}");
				}
			}
			else
			{
				Console.WriteLine("До свидания! Ждем вас в магазине автозапчастей!");
			}
			Environment.Exit(0);
		}

		static void HandleStartCommand()
		{
			if (isStarted)
			{
				Console.WriteLine(string.IsNullOrEmpty(userName)
					? "Работа уже начата!"
					: $"{userName}, мы уже начали работу!");
				return;
			}

			Console.WriteLine("Привет! Я бот для поиска и заказа автозапчастей.");
			Console.WriteLine("Для персонализации сервиса, пожалуйста, введите ваше имя:");
			Console.Write("Имя: ");
			userName = Console.ReadLine();

			ValidateString(userName);
			userName = userName.Trim();

			isStarted = true;
			Console.WriteLine($"Отлично, {userName}! Теперь вы можете использовать все функции бота.");
			Console.WriteLine($"Ограничения: макс. задач - {maxTaskCount}, макс. длина - {maxTaskLength}");
			Console.WriteLine();
		}

		static void HandleHelpCommand()
		{
			if (!string.IsNullOrEmpty(userName))
			{
				Console.WriteLine($"{userName}, вот список доступных команд:");
			}
			else
			{
				Console.WriteLine("Список доступных команд:");
			}

			ShowAvailableCommands();
			Console.WriteLine();
			Console.WriteLine("Примеры использования:");
			Console.WriteLine("  /start                   - начать диалог");
			Console.WriteLine("  /echo тормозные колодки - поиск запчастей");
			Console.WriteLine("  /addtask                - добавить новый заказ");
			Console.WriteLine("  /showtasks              - показать все заказы");
			Console.WriteLine("  /removetask             - удалить заказ по номеру");
			Console.WriteLine();
		}

		static void HandleInfoCommand()
		{
			if (!string.IsNullOrEmpty(userName))
			{
				Console.WriteLine($"{userName}, информация о программе:");
			}
			else
			{
				Console.WriteLine("Информация о программе:");
			}

			Console.WriteLine($"  Версия: 3.0 (с валидацией)");
			Console.WriteLine($"  Дата создания: 24.10.2023");
			Console.WriteLine($"  Текущие ограничения:");
			Console.WriteLine($"    • Максимальное количество задач: {maxTaskCount}");
			Console.WriteLine($"    • Максимальная длина задачи: {maxTaskLength}");
			Console.WriteLine($"    • Текущее количество задач: {tasks.Count}");
			Console.WriteLine("  Разработчик: Команда AutoParts Hub");
			Console.WriteLine();
		}

		static void HandleEchoCommand(string input)
		{
			if (!isStarted)
			{
				Console.WriteLine("Пожалуйста, сначала выполните команду /start для начала работы.");
				return;
			}

			string echoText = input.Substring(5).Trim();
			ValidateString(echoText);

			Console.WriteLine($"{userName}, ищу запчасти по запросу: \"{echoText}\"");
			Console.WriteLine("...");
			System.Threading.Thread.Sleep(800);

			Console.WriteLine($"Найдено предложений по запросу \"{echoText}\"");
			Console.WriteLine($"Совет: используйте /addtask чтобы добавить этот запрос в список заказов");
			Console.WriteLine();
		}

		static void HandleAddTaskCommand()
		{
			if (!isStarted)
			{
				Console.WriteLine("Пожалуйста, сначала выполните команду /start для начала работы.");
				return;
			}

			// Проверка ограничения по количеству задач
			if (tasks.Count >= maxTaskCount)
			{
				throw new TaskCountLimitException(maxTaskCount);
			}

			Console.WriteLine($"{userName}, введите описание задачи (например, 'заказать масляный фильтр'):");
			Console.Write("Описание: ");
			string taskDescription = Console.ReadLine();

			ValidateString(taskDescription);
			taskDescription = taskDescription.Trim();

			// Проверка ограничения по длине задачи
			if (taskDescription.Length > maxTaskLength)
			{
				throw new TaskLengthLimitException(taskDescription.Length, maxTaskLength);
			}

			// Проверка на дубликаты
			if (tasks.Any(t => t.Equals(taskDescription, StringComparison.OrdinalIgnoreCase)))
			{
				throw new DuplicateTaskException(taskDescription);
			}

			// Добавляем задачу
			string taskWithDate = $"{DateTime.Now:dd.MM.yyyy HH:mm} - {taskDescription}";
			tasks.Add(taskWithDate);
			Console.WriteLine($"Задача добавлена! Всего задач в списке: {tasks.Count}");
		}

		static void HandleShowTasksCommand()
		{
			if (!isStarted)
			{
				Console.WriteLine("Пожалуйста, сначала выполните команду /start для начала работы.");
				return;
			}

			if (tasks.Count == 0)
			{
				Console.WriteLine($"{userName}, список ваших задач/заказов пуст.");
				Console.WriteLine($"Можно добавить до {maxTaskCount} задач.");
			}
			else
			{
				Console.WriteLine($"{userName}, вот ваш список задач/заказов:");
				Console.WriteLine("=========================================");
				for (int i = 0; i < tasks.Count; i++)
				{
					Console.WriteLine($"{i + 1}. {tasks[i]}");
				}
				Console.WriteLine("=========================================");
				Console.WriteLine($"Всего задач: {tasks.Count} из {maxTaskCount}");
			}
			Console.WriteLine();
		}

		static void HandleRemoveTaskCommand()
		{
			if (!isStarted)
			{
				Console.WriteLine("Пожалуйста, сначала выполните команду /start для начала работы.");
				return;
			}

			if (tasks.Count == 0)
			{
				Console.WriteLine($"{userName}, список задач пуст. Нечего удалять.");
				return;
			}

			Console.WriteLine($"{userName}, вот ваш текущий список задач:");
			Console.WriteLine("=========================================");
			for (int i = 0; i < tasks.Count; i++)
			{
				Console.WriteLine($"{i + 1}. {tasks[i]}");
			}
			Console.WriteLine("=========================================");

			Console.WriteLine("Введите номер задачи для удаления (или 'отмена' для отмены):");
			Console.Write("Номер: ");
			string removeInput = Console.ReadLine();

			if (removeInput.ToLower() == "отмена")
			{
				Console.WriteLine("Удаление отменено.");
				return;
			}

			int taskNumber = ParseAndValidateInt(removeInput, 1, tasks.Count);
			string removedTask = tasks[taskNumber - 1];
			tasks.RemoveAt(taskNumber - 1);
			Console.WriteLine($"Задача удалена: \"{removedTask}\"");
			Console.WriteLine($"Осталось задач: {tasks.Count} из {maxTaskCount}");
			Console.WriteLine();
		}

		static void HandleUnknownCommand(string input)
		{
			if (!string.IsNullOrEmpty(userName))
			{
				Console.WriteLine($"{userName}, извините, я не понимаю команду \"{input}\".");
			}
			else
			{
				Console.WriteLine($"Извините, я не понимаю команду \"{input}\".");
			}
			Console.WriteLine("Введите /help для просмотра списка доступных команд.");
			Console.WriteLine();
		}

		// Вспомогательные методы
		static void ShowAvailableCommands()
		{
			Console.WriteLine("  /start      - Начать работу с ботом");
			Console.WriteLine("  /help       - Получить справку по командам");
			Console.WriteLine("  /info       - Информация о программе");
			Console.WriteLine("  /echo       - Поиск запчастей (после /start)");
			Console.WriteLine("  /addtask    - Добавить задачу/заказ");
			Console.WriteLine("  /showtasks  - Показать список задач/заказов");
			Console.WriteLine("  /removetask - Удалить задачу/заказ");
			Console.WriteLine("  /exit       - Выйти из программы");
		}

		static void ContinueApplication()
		{
			Console.WriteLine();
			Console.WriteLine("Нажмите любую клавишу для продолжения...");
			Console.ReadKey();
			Console.WriteLine();

			// Продолжаем работу приложения
			MainLoop();
		}

		// Методы валидации
		static int ParseAndValidateInt(string? str, int min, int max)
		{
			if (!int.TryParse(str, out int result))
			{
				throw new ArgumentException($"Значение '{str}' не является числом.");
			}

			if (result < min || result > max)
			{
				throw new ArgumentException($"Число {result} должно быть в диапазоне от {min} до {max}.");
			}

			return result;
		}

		static void ValidateString(string? str)
		{
			if (str == null)
			{
				throw new ArgumentException("Строка не может быть null.");
			}

			if (string.IsNullOrWhiteSpace(str))
			{
				throw new ArgumentException("Строка не может быть пустой или состоять только из пробелов.");
			}
		}
	}
}