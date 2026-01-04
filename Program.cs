using System;

namespace Otus_My_HomeWork
{
	internal class Program
	{
		static void Main(string[] args)
		{
			string? name = "";
			bool isStarted = false;
			string version = "1.0";
			string creationDate = "12.01.2026";
			Console.WriteLine("Добро пожаловать! Доступные команды: /start, /help, /info, /exit");

			while (true)
			{
				string? input = Console.ReadLine();

				if (string.IsNullOrWhiteSpace(input))
					continue;

				string[] parts = input.Split(' ', 2);
				string command = parts[0].ToLower(); 
				string argument = parts.Length > 1 ? parts[1] : "";

				switch (command)
				{
					case "/start":
						if (!isStarted)
						{
							Console.Write("Введите ваше имя: ");
							name = Console.ReadLine();
							isStarted = true;
							Console.WriteLine($"Привет, {name}! Теперь вам доступна команда /echo.");
						}
						else
						{
							Console.WriteLine($"{name}, вы уже начали работу.");
						}
						break;

					case "/help":
						if (isStarted)
							Console.WriteLine($"{name}, справка по программе:");
						else
							Console.WriteLine("Справка по программе:");

						Console.WriteLine("/start - начало работы, ввод имени");
						Console.WriteLine("/help - справочная информация");
						Console.WriteLine("/info - информация о версии и дате создания");
						Console.WriteLine("/echo [текст] - повтор введенного текста (доступна после /start)");
						Console.WriteLine("/exit - выход из программы");
						break;

					case "/info":
						if (isStarted)
							Console.WriteLine($"{name}, информация о программе:");
						else
							Console.WriteLine("Информация о программе:");

						Console.WriteLine($"Версия программы: {version}");
						Console.WriteLine($"Дата создания: {creationDate}");
						break;

					case "/echo":
						if (!isStarted)
						{
							Console.WriteLine("Сначала введите команду /start.");
						}
						else if (string.IsNullOrWhiteSpace(argument))
						{

							Console.WriteLine($"{name}, вы не ввели текст для повторения.");
						}
						else
						{
							Console.WriteLine($"{name}, вы ввели: {argument}");
						}
						break;

					case "/exit":
						Console.WriteLine("До свидания!");
						return;

					default:
						if (isStarted)
							Console.WriteLine($"{name}, неизвестная команда. Введите /help для справки.");
						else
							Console.WriteLine("Неизвестная команда. Введите /help для справки.");
						break;
				}
			}
		}
	}
}

