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

	public class TaskNotFoundException : Exception
	{
		public TaskNotFoundException(Guid taskId)
			: base($"Задача с ID '{taskId}' не найдена")
		{
		}
	}

	// Enum для состояния задачи
	public enum ToDoItemState
	{
		Active,
		Completed
	}

	// Класс пользователя
	public class ToDoUser
	{
		public Guid UserId { get; }
		public string TelegramUserName { get; }
		public DateTime RegisteredAt { get; }

		public ToDoUser(string telegramUserName)
		{
			UserId = Guid.NewGuid();
			TelegramUserName = telegramUserName;
			RegisteredAt = DateTime.UtcNow;
		}

		public override string ToString()
		{
			return $"{TelegramUserName} (зарегистрирован: {RegisteredAt:dd.MM.yyyy HH:mm})";
		}
	}

	// Класс задачи
	public class ToDoItem
	{
		public Guid Id { get; }
		public ToDoUser User { get; }
		public string Name { get; }
		public DateTime CreatedAt { get; }
		public ToDoItemState State { get; private set; }
		public DateTime? StateChangedAt { get; private set; }

		public ToDoItem(ToDoUser user, string name)
		{
			Id = Guid.NewGuid();
			User = user;
			Name = name;
			CreatedAt = DateTime.UtcNow;
			State = ToDoItemState.Active;
			StateChangedAt = null;
		}

		public void MarkAsCompleted()
		{
			State = ToDoItemState.Completed;
			StateChangedAt = DateTime.UtcNow;
		}

		public override string ToString()
		{
			var stateText = State == ToDoItemState.Active ? "Active" : "Completed";
			var stateChangedText = StateChangedAt.HasValue
				? $" | Изменено: {StateChangedAt.Value:dd.MM.yyyy HH:mm:ss}"
				: "";

			return $"{Name} - {CreatedAt:dd.MM.yyyy HH:mm:ss} - {Id}";
		}

		public string ToStringWithState()
		{
			var stateText = State == ToDoItemState.Active ? "(Active)" : "(Completed)";
			var stateChangedText = StateChangedAt.HasValue
				? $" | Изменено: {StateChangedAt.Value:dd.MM.yyyy HH:mm:ss}"
				: "";

			return $"{stateText} {Name} - {CreatedAt:dd.MM.yyyy HH:mm:ss} - {Id}{stateChangedText}";
		}
	}

	class Program
	{
		// Глобальные переменные для ограничений
		private static int maxTaskCount = 10;
		private static int maxTaskLength = 50;

		// Список задач и пользователь
		private static List<ToDoItem> tasks = new List<ToDoItem>();
		private static ToDoUser currentUser = null;
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
			catch (TaskNotFoundException ex)
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
			Console.WriteLine($"  AutoParts Bot v4.0 (ООП версия) | Лимит задач: {maxTaskCount} | Лим.длина: {maxTaskLength}");
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
				catch (TaskNotFoundException ex)
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
			if (currentUser == null)
			{
				Console.Write("Введите команду: ");
			}
			else
			{
				Console.Write($"{currentUser.TelegramUserName}, введите команду: ");
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
				case "/showalltasks":
					HandleShowAllTasksCommand();
					break;
				case "/completetask":
					HandleCompleteTaskCommand();
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
			if (currentUser != null)
			{
				int activeTasksCount = tasks.Count(t => t.State == ToDoItemState.Active);
				int completedTasksCount = tasks.Count(t => t.State == ToDoItemState.Completed);

				Console.WriteLine($"До свидания, {currentUser.TelegramUserName}! Ваш список задач сохранен.");
				Console.WriteLine($"Всего задач: {tasks.Count} (активных: {activeTasksCount}, завершенных: {completedTasksCount})");
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
				Console.WriteLine(currentUser == null
					? "Работа уже начата!"
					: $"{currentUser.TelegramUserName}, мы уже начали работу!");
				return;
			}

			Console.WriteLine("Привет! Я бот для поиска и заказа автозапчастей.");
			Console.WriteLine("Для персонализации сервиса, пожалуйста, введите ваше имя:");
			Console.Write("Имя: ");
			string userName = Console.ReadLine();

			ValidateString(userName);
			userName = userName.Trim();

			// Создаем нового пользователя
			currentUser = new ToDoUser(userName);
			isStarted = true;

			Console.WriteLine($"Отлично, {currentUser.TelegramUserName}! Теперь вы можете использовать все функции бота.");
			Console.WriteLine($"Ограничения: макс. задач - {maxTaskCount}, макс. длина - {maxTaskLength}");
			Console.WriteLine($"Ваш UserId: {currentUser.UserId}");
			Console.WriteLine();
		}

		static void HandleHelpCommand()
		{
			if (currentUser != null)
			{
				Console.WriteLine($"{currentUser.TelegramUserName}, вот список доступных команд:");
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
			Console.WriteLine("  /showtasks              - показать активные заказы");
			Console.WriteLine("  /showalltasks           - показать все заказы (активные и завершенные)");
			Console.WriteLine("  /completetask <ID>      - завершить задачу по ID (пример: /completetask 73c7940a-ca8c-4327-8a15-9119bffd1d5e)");
			Console.WriteLine("  /removetask             - удалить заказ по номеру");
			Console.WriteLine();
		}

		static void HandleInfoCommand()
		{
			if (currentUser != null)
			{
				Console.WriteLine($"{currentUser.TelegramUserName}, информация о программе:");
			}
			else
			{
				Console.WriteLine("Информация о программе:");
			}

			int activeTasksCount = tasks.Count(t => t.State == ToDoItemState.Active);
			int completedTasksCount = tasks.Count(t => t.State == ToDoItemState.Completed);

			Console.WriteLine($"  Версия: 4.0 (ООП с классами)");
			Console.WriteLine($"  Дата создания: 24.10.2023");
			Console.WriteLine($"  Текущие ограничения:");
			Console.WriteLine($"    • Максимальное количество задач: {maxTaskCount}");
			Console.WriteLine($"    • Максимальная длина задачи: {maxTaskLength}");
			Console.WriteLine($"    • Текущее количество задач: {tasks.Count} (активных: {activeTasksCount}, завершенных: {completedTasksCount})");

			if (currentUser != null)
			{
				Console.WriteLine($"  Информация о пользователе:");
				Console.WriteLine($"    • UserId: {currentUser.UserId}");
				Console.WriteLine($"    • Имя: {currentUser.TelegramUserName}");
				Console.WriteLine($"    • Зарегистрирован: {currentUser.RegisteredAt:dd.MM.yyyy HH:mm}");
			}

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

			Console.WriteLine($"{currentUser.TelegramUserName}, ищу запчасти по запросу: \"{echoText}\"");
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

			Console.WriteLine($"{currentUser.TelegramUserName}, введите описание задачи (например, 'заказать масляный фильтр'):");
			Console.Write("Описание: ");
			string taskDescription = Console.ReadLine();

			ValidateString(taskDescription);
			taskDescription = taskDescription.Trim();

			// Проверка ограничения по длине задачи
			if (taskDescription.Length > maxTaskLength)
			{
				throw new TaskLengthLimitException(taskDescription.Length, maxTaskLength);
			}

			// Проверка на дубликаты (только среди активных задач)
			if (tasks.Any(t => t.Name.Equals(taskDescription, StringComparison.OrdinalIgnoreCase)
							&& t.State == ToDoItemState.Active))
			{
				throw new DuplicateTaskException(taskDescription);
			}

			// Создаем новую задачу
			ToDoItem newTask = new ToDoItem(currentUser, taskDescription);
			tasks.Add(newTask);

			Console.WriteLine($"Задача добавлена! ID задачи: {newTask.Id}");
			Console.WriteLine($"Всего задач в списке: {tasks.Count}");
		}

		static void HandleShowTasksCommand()
		{
			if (!isStarted)
			{
				Console.WriteLine("Пожалуйста, сначала выполните команду /start для начала работы.");
				return;
			}

			var activeTasks = tasks.Where(t => t.State == ToDoItemState.Active).ToList();

			if (activeTasks.Count == 0)
			{
				Console.WriteLine($"{currentUser.TelegramUserName}, список ваших активных задач/заказов пуст.");
				Console.WriteLine($"Можно добавить до {maxTaskCount} задач.");
			}
			else
			{
				Console.WriteLine($"{currentUser.TelegramUserName}, вот ваш список активных задач/заказов:");
				Console.WriteLine("======================================================================");
				for (int i = 0; i < activeTasks.Count; i++)
				{
					Console.WriteLine($"{i + 1}. {activeTasks[i]}");
				}
				Console.WriteLine("======================================================================");
				Console.WriteLine($"Активных задач: {activeTasks.Count} из {maxTaskCount}");
			}
			Console.WriteLine();
		}

		static void HandleShowAllTasksCommand()
		{
			if (!isStarted)
			{
				Console.WriteLine("Пожалуйста, сначала выполните команду /start для начала работы.");
				return;
			}

			if (tasks.Count == 0)
			{
				Console.WriteLine($"{currentUser.TelegramUserName}, список ваших задач/заказов пуст.");
				Console.WriteLine($"Можно добавить до {maxTaskCount} задач.");
			}
			else
			{
				int activeTasksCount = tasks.Count(t => t.State == ToDoItemState.Active);
				int completedTasksCount = tasks.Count(t => t.State == ToDoItemState.Completed);

				Console.WriteLine($"{currentUser.TelegramUserName}, вот ваш список всех задач/заказов:");
				Console.WriteLine("======================================================================");
				for (int i = 0; i < tasks.Count; i++)
				{
					Console.WriteLine($"{i + 1}. {tasks[i].ToStringWithState()}");
				}
				Console.WriteLine("======================================================================");
				Console.WriteLine($"Всего задач: {tasks.Count} (активных: {activeTasksCount}, завершенных: {completedTasksCount}) из {maxTaskCount}");
			}
			Console.WriteLine();
		}

		static void HandleCompleteTaskCommand()
		{
			if (!isStarted)
			{
				Console.WriteLine("Пожалуйста, сначала выполните команду /start для начала работы.");
				return;
			}

			Console.WriteLine($"{currentUser.TelegramUserName}, введите ID задачи для завершения:");
			Console.Write("ID задачи: ");
			string taskIdInput = Console.ReadLine();

			ValidateString(taskIdInput);

			if (!Guid.TryParse(taskIdInput.Trim(), out Guid taskId))
			{
				Console.WriteLine("Ошибка: неверный формат ID. ID должен быть в формате GUID.");
				return;
			}

			// Ищем задачу по ID
			ToDoItem taskToComplete = tasks.FirstOrDefault(t => t.Id == taskId);

			if (taskToComplete == null)
			{
				throw new TaskNotFoundException(taskId);
			}

			if (taskToComplete.State == ToDoItemState.Completed)
			{
				Console.WriteLine($"Задача '{taskToComplete.Name}' уже завершена.");
				return;
			}

			// Помечаем задачу как завершенную
			taskToComplete.MarkAsCompleted();

			Console.WriteLine($"Задача '{taskToComplete.Name}' успешно завершена!");
			Console.WriteLine($"Время завершения: {taskToComplete.StateChangedAt:dd.MM.yyyy HH:mm:ss}");
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
				Console.WriteLine($"{currentUser.TelegramUserName}, список задач пуст. Нечего удалять.");
				return;
			}

			Console.WriteLine($"{currentUser.TelegramUserName}, вот ваш текущий список всех задач:");
			Console.WriteLine("======================================================================");
			for (int i = 0; i < tasks.Count; i++)
			{
				Console.WriteLine($"{i + 1}. {tasks[i].ToStringWithState()}");
			}
			Console.WriteLine("======================================================================");

			Console.WriteLine("Введите номер задачи для удаления (или 'отмена' для отмены):");
			Console.Write("Номер: ");
			string removeInput = Console.ReadLine();

			if (removeInput.ToLower() == "отмена")
			{
				Console.WriteLine("Удаление отменено.");
				return;
			}

			int taskNumber = ParseAndValidateInt(removeInput, 1, tasks.Count);
			ToDoItem removedTask = tasks[taskNumber - 1];
			tasks.RemoveAt(taskNumber - 1);
			Console.WriteLine($"Задача удалена: \"{removedTask.Name}\"");
			Console.WriteLine($"Осталось задач: {tasks.Count} из {maxTaskCount}");
			Console.WriteLine();
		}

		static void HandleUnknownCommand(string input)
		{
			if (currentUser != null)
			{
				Console.WriteLine($"{currentUser.TelegramUserName}, извините, я не понимаю команду \"{input}\".");
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
			Console.WriteLine("  /start        - Начать работу с ботом");
			Console.WriteLine("  /help         - Получить справку по командам");
			Console.WriteLine("  /info         - Информация о программе");
			Console.WriteLine("  /echo         - Поиск запчастей (после /start)");
			Console.WriteLine("  /addtask      - Добавить задачу/заказ");
			Console.WriteLine("  /showtasks    - Показать список активных задач/заказов");
			Console.WriteLine("  /showalltasks - Показать все задачи/заказы (включая завершенные)");
			Console.WriteLine("  /completetask - Завершить задачу по ID");
			Console.WriteLine("  /removetask   - Удалить задачу/заказ по номеру");
			Console.WriteLine("  /exit         - Выйти из программы");
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