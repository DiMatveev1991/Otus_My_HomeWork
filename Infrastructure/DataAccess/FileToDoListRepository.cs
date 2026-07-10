using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Core.DataAccess;
using Core.Entities;

namespace Infrastructure.DataAccess
{
	/// <summary>
	/// Файловое хранилище ToDoList.
	/// Структура каталога:
	///   {basePath}/
	///     {ListId-1}.json
	///     {ListId-2}.json
	///
	/// Поиск по UserId — линейный по всем файлам (списков заведомо немного),
	/// реализовано аналогично <see cref="FileUserRepository"/>.
	/// </summary>
	public class FileToDoListRepository : IToDoListRepository
	{
		private readonly string _basePath;
		private readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };

		public FileToDoListRepository(string basePath)
		{
			if (string.IsNullOrWhiteSpace(basePath))
				throw new ArgumentException("Базовая папка не может быть пустой", nameof(basePath));

			_basePath = basePath;

			// По заданию: папку создаём только если её ещё нет
			if (!Directory.Exists(_basePath))
				Directory.CreateDirectory(_basePath);
		}

		public async Task<ToDoList?> Get(Guid id, CancellationToken ct)
		{
			var path = GetListPath(id);
			if (!File.Exists(path)) return null;

			await using var fs = File.OpenRead(path);
			return await JsonSerializer.DeserializeAsync<ToDoList>(fs, _jsonOptions, ct);
		}

		public async Task<IReadOnlyList<ToDoList>> GetByUserId(Guid userId, CancellationToken ct)
		{
			var result = new List<ToDoList>();

			foreach (var file in Directory.EnumerateFiles(_basePath, "*.json"))
			{
				ct.ThrowIfCancellationRequested();

				var list = await ReadListAsync(file, ct);
				if (list != null && list.User.UserId == userId)
					result.Add(list);
			}

			return result;
		}

		public async Task Add(ToDoList list, CancellationToken ct)
		{
			if (list == null) throw new ArgumentNullException(nameof(list));

			var path = GetListPath(list.Id);
			await using var fs = File.Create(path);
			await JsonSerializer.SerializeAsync(fs, list, _jsonOptions, ct);
		}

		public Task Delete(Guid id, CancellationToken ct)
		{
			var path = GetListPath(id);
			if (File.Exists(path))
				File.Delete(path);

			return Task.CompletedTask;
		}

		public async Task<bool> ExistsByName(Guid userId, string name, CancellationToken ct)
		{
			foreach (var file in Directory.EnumerateFiles(_basePath, "*.json"))
			{
				ct.ThrowIfCancellationRequested();

				var list = await ReadListAsync(file, ct);
				if (list != null &&
					list.User.UserId == userId &&
					list.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
					return true;
			}

			return false;
		}

		private async Task<ToDoList?> ReadListAsync(string path, CancellationToken ct)
		{
			await using var fs = File.OpenRead(path);
			return await JsonSerializer.DeserializeAsync<ToDoList>(fs, _jsonOptions, ct);
		}

		private string GetListPath(Guid id) =>
			Path.Combine(_basePath, $"{id}.json");
	}
}
