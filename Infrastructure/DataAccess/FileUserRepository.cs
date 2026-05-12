using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Core.DataAccess;
using Core.Entities;

namespace Infrastructure.DataAccess
{
	/// <summary>
	/// Файловое хранилище ToDoUser.
	/// Структура каталога:
	///   {basePath}/
	///     {UserId-1}.json
	///     {UserId-2}.json
	///
	/// Поиск по TelegramUserId — линейный по всем файлам (пользователей
	/// заведомо немного, отдельный индекс в задании не требуется).
	/// </summary>
	public class FileUserRepository : IUserRepository
	{
		private readonly string _basePath;
		private readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };

		public FileUserRepository(string basePath)
		{
			if (string.IsNullOrWhiteSpace(basePath))
				throw new ArgumentException("Базовая папка не может быть пустой", nameof(basePath));

			_basePath = basePath;

			// По заданию: папку нужно создавать, если её нет
			Directory.CreateDirectory(_basePath);
		}

		public async Task<ToDoUser?> GetUserAsync(Guid userId, CancellationToken ct)
		{
			var path = GetUserPath(userId);
			if (!File.Exists(path)) return null;

			await using var fs = File.OpenRead(path);
			return await JsonSerializer.DeserializeAsync<ToDoUser>(fs, _jsonOptions, ct);
		}

		public async Task<ToDoUser?> GetUserByTelegramUserIdAsync(long telegramUserId, CancellationToken ct)
		{
			// Файла-индекса по TelegramUserId по заданию нет — обходим все файлы.
			// Пользователей в системе очень мало, это приемлемо.
			foreach (var file in Directory.EnumerateFiles(_basePath, "*.json"))
			{
				ct.ThrowIfCancellationRequested();

				ToDoUser? user;
				await using (var fs = File.OpenRead(file))
				{
					user = await JsonSerializer.DeserializeAsync<ToDoUser>(fs, _jsonOptions, ct);
				}

				if (user != null && user.TelegramUserId == telegramUserId)
					return user;
			}
			return null;
		}

		public async Task AddAsync(ToDoUser user, CancellationToken ct)
		{
			if (user == null) throw new ArgumentNullException(nameof(user));

			var path = GetUserPath(user.UserId);
			await using var fs = File.Create(path);
			await JsonSerializer.SerializeAsync(fs, user, _jsonOptions, ct);
		}

		private string GetUserPath(Guid userId) =>
			Path.Combine(_basePath, $"{userId}.json");
	}
}