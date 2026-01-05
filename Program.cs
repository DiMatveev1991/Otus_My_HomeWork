using System;

namespace AutoPartsBot
{
	class Program
	{
		static void Main(string[] args)
		{
			// Данные о программе
			string version = "1.0";
			string creationDate = "24.10.2023";

			// Переменные состояния
			string userName = null;
			bool isStarted = false;

			// Приветственное сообщение
			Console.WriteLine("==================================================================");
			Console.WriteLine("  Добро пожаловать в AutoParts Bot - ваш помощник в поиске       ");
			Console.WriteLine("  автозапчастей!                                                  ");
			Console.WriteLine("==================================================================");
			Console.WriteLine();
			Console.WriteLine("Доступные команды:");
			Console.WriteLine("  /start  - Начать работу с ботом");
			Console.WriteLine("  /help   - Получить справку по командам");
			Console.WriteLine("  /info   - Информация о программе");
			Console.WriteLine("  /exit   - Выйти из программы");
			Console.WriteLine();

			// Основной цикл программы
			while (true)
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
				if (string.IsNullOrWhiteSpace(input))
				{
					Console.WriteLine("Пожалуйста, введите команду.");
					continue;
				}

				// Приведение команды к нижнему регистру для удобства сравнения
				string command = input.Trim().ToLower();

				// Обработка команды /exit
				if (command == "/exit")
				{
					if (!string.IsNullOrEmpty(userName))
					{
						Console.WriteLine($"До свидания, {userName}! Ждем вас в магазине автозапчастей!");
					}
					else
					{
						Console.WriteLine("До свидания! Ждем вас в магазине автозапчастей!");
					}
					break;
				}

				// Обработка команды /start
				else if (command == "/start")
				{
					if (isStarted)
					{
						if (!string.IsNullOrEmpty(userName))
						{
							Console.WriteLine($"{userName}, мы уже начали работу!");
						}
						else
						{
							Console.WriteLine("Работа уже начата!");
						}
					}
					else
					{
						Console.WriteLine("Привет! Я бот для поиска и заказа автозапчастей.");
						Console.WriteLine("Для персонализации сервиса, пожалуйста, введите ваше имя:");
						Console.Write("Имя: ");
						userName = Console.ReadLine();

						// Проверка на пустое имя
						if (string.IsNullOrWhiteSpace(userName))
						{
							userName = "Пользователь";
							Console.WriteLine("Будем звать вас 'Пользователь'.");
						}

						isStarted = true;
						Console.WriteLine($"Отлично, {userName}! Теперь вы можете использовать все функции бота.");
						Console.WriteLine("Для поиска запчастей используйте команду /echo с параметрами,");
						Console.WriteLine("например: /echo фильтр воздушный Toyota Camry");
						Console.WriteLine();
					}
				}

				// Обработка команды /help
				else if (command == "/help")
				{
					if (!string.IsNullOrEmpty(userName))
					{
						Console.WriteLine($"{userName}, вот список доступных команд:");
					}
					else
					{
						Console.WriteLine("Список доступных команд:");
					}

					Console.WriteLine("  /start - Начать работу с ботом (ввести имя)");
					Console.WriteLine("  /help  - Показать эту справку");
					Console.WriteLine("  /info  - Информация о версии программы");
					Console.WriteLine("  /echo  - Поиск запчастей (доступно после /start)");
					Console.WriteLine("  /exit  - Завершить работу с ботом");
					Console.WriteLine();
					Console.WriteLine("Примеры использования:");
					Console.WriteLine("  /start - начать диалог");
					Console.WriteLine("  /echo тормозные колодки BMW X5 - поиск запчастей");
					Console.WriteLine();
				}

				// Обработка команды /info
				else if (command == "/info")
				{
					if (!string.IsNullOrEmpty(userName))
					{
						Console.WriteLine($"{userName}, информация о программе:");
					}
					else
					{
						Console.WriteLine("Информация о программе:");
					}

					Console.WriteLine($"  Версия: {version}");
					Console.WriteLine($"  Дата создания: {creationDate}");
					Console.WriteLine("  Назначение: Консольный бот для подбора автозапчастей");
					Console.WriteLine("  Разработчик: Команда AutoParts Hub");
					Console.WriteLine();
				}

				// Обработка команды /echo
				else if (command.StartsWith("/echo"))
				{
					if (!isStarted)
					{
						Console.WriteLine("Пожалуйста, сначала выполните команду /start для начала работы.");
						continue;
					}

					// Извлекаем текст после команды /echo
					string echoText = input.Substring(5).Trim();

					if (string.IsNullOrWhiteSpace(echoText))
					{
						Console.WriteLine($"{userName}, пожалуйста, укажите параметры для поиска.");
						Console.WriteLine("Пример: /echo масляный фильтр Volkswagen Passat");
					}
					else
					{
						// Эмуляция поиска запчастей
						Console.WriteLine($"{userName}, ищу запчасти по запросу: \"{echoText}\"");
						Console.WriteLine("...");

						// Имитация задержки поиска
						System.Threading.Thread.Sleep(800);

						Console.WriteLine($"Найдено 15 предложений по запросу \"{echoText}\"");
						Console.WriteLine("1. Масляный фильтр Mann, артикул W-67/1 - 850 руб.");
						Console.WriteLine("2. Масляный фильтр Bosch, артикул F-026-4-077-041 - 920 руб.");
						Console.WriteLine("3. Масляный фильтр Fram, артикул PH-4967 - 780 руб.");
						Console.WriteLine("... и еще 12 предложений");
						Console.WriteLine("Для детального просмотра перейдите на сайт autoparts-hub.ru");
						Console.WriteLine();
					}
				}

				// Обработка неизвестной команды
				else
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
			}

			// Завершение программы
			Console.WriteLine("Нажмите любую клавишу для выхода...");
			Console.ReadKey();
		}
	}
}