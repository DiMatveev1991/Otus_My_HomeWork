using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Core.DataAccess;
using Core.Entities;
using Core.Enums;

namespace Infrastructure.DataAccess
{
	/// <summary>
	/// Файловое хранилище ToDoItem.
	/// Структура каталога:
	///   {basePath}/
	///     index.json                          — словарь { ToDoItemId : UserId }
	///     {UserId-1}/
	///       {ItemId-A}.json
	///       {ItemId-B}.json
	///     {UserId-2}/
	///       {ItemId-C}.json
	///
	/// Группировка по UserId ускоряет операции в контексте одного пользователя
	/// (GetAllByUserId, CountActive, ExistsByName, Find): не нужно сканировать чужие папки.
	///
	/// Индекс ускоряет операции по Id без UserId (Get, Delete): без него
	/// пришлось бы обходить все папки.
	/// </summary>
	public class FileToDoRepository : IToDoRepository
	{
		private const string IndexFileName = "index.json";

		private readonly string _basePath;
		private readonly string _indexPath;
		private readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };

		// Защищаем индекс от одновременной модификации.
		// Согласно ознакомительному примечанию задания, в идеале нужно несколько
		// SemaphoreSlim по UserId — но это вне рамок курса, поэтому один общий.
		private readonly SemaphoreSlim _gate = new(1, 1);

		public FileToDoRepository(string basePath)
		{
			if (string.IsNullOrWhiteSpace(basePath))
				throw new ArgumentException("Базовая папка не может быть пустой", nameof(basePath));

			_basePath = basePath;
			_indexPath = Path.Combine(_basePath, IndexFileName);

			// По заданию: папку создаём только если её ещё нет
			if (!Directory.Exists(_basePath))
				Directory.CreateDirectory(_basePath);
		}

		// === IToDoRepository =================================================

		public async Task<IReadOnlyList<ToDoItem>> GetAllByUserIdAsync(Guid userId, CancellationToken ct)
		{
			var userDir = GetUserDirectory(userId);
			if (!Directory.Exists(userDir))
				return Array.Empty<ToDoItem>();

			var result = new List<ToDoItem>();
			foreach (var file in Directory.EnumerateFiles(userDir, "*.json"))
			{
				ct.ThrowIfCancellationRequested();
				var item = await ReadItemAsync(file, ct);
				if (item != null) result.Add(item);
			}
			return result;
		}

		public async Task<IReadOnlyList<ToDoItem>> GetActiveByUserIdAsync(Guid userId, CancellationToken ct)
		{
			var all = await GetAllByUserIdAsync(userId, ct);
			return all.Where(i => i.State == ToDoItemState.Active).ToList();
		}

		public async Task<ToDoItem?> GetAsync(Guid id, CancellationToken ct)
		{
			// Используем индекс, чтобы найти UserId по ItemId,
			// иначе пришлось бы обходить все папки
			var index = await GetOrBuildIndexAsync(ct);
			if (!index.TryGetValue(id, out var userId))
				return null;

			var path = GetItemPath(userId, id);
			if (!File.Exists(path))
				return null;

			return await ReadItemAsync(path, ct);
		}

		public async Task AddAsync(ToDoItem item, CancellationToken ct)
		{
			if (item == null) throw new ArgumentNullException(nameof(item));

			var userDir = GetUserDirectory(item.User.UserId);
			if (!Directory.Exists(userDir))
				Directory.CreateDirectory(userDir);

			var path = GetItemPath(item.User.UserId, item.Id);
			await WriteItemAsync(path, item, ct);

			// По заданию: индекс наполняется в Add
			await _gate.WaitAsync(ct);
			try
			{
				var index = await LoadOrBuildIndexAsync(ct);
				index[item.Id] = item.User.UserId;
				await SaveIndexAsync(index, ct);
			}
			finally { _gate.Release(); }
		}

		public async Task UpdateAsync(ToDoItem item, CancellationToken ct)
		{
			if (item == null) throw new ArgumentNullException(nameof(item));

			var path = GetItemPath(item.User.UserId, item.Id);
			if (!File.Exists(path)) return; // нечего обновлять

			await WriteItemAsync(path, item, ct);
			// Индекс не трогаем — пара (ItemId, UserId) не меняется при Update
		}

		public async Task DeleteAsync(Guid id, CancellationToken ct)
		{
			// По заданию: индекс используется и обновляется в Delete
			await _gate.WaitAsync(ct);
			try
			{
				var index = await LoadOrBuildIndexAsync(ct);

				if (!index.TryGetValue(id, out var userId))
					return; // нет в индексе — ничего не делаем

				var path = GetItemPath(userId, id);
				if (File.Exists(path))
					File.Delete(path);

				index.Remove(id);
				await SaveIndexAsync(index, ct);
			}
			finally { _gate.Release(); }
		}

		public async Task<bool> ExistsByNameAsync(Guid userId, string name, CancellationToken ct)
		{
			var all = await GetAllByUserIdAsync(userId, ct);
			return all.Any(i => i.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
		}

		public async Task<int> CountActiveAsync(Guid userId, CancellationToken ct)
		{
			var active = await GetActiveByUserIdAsync(userId, ct);
			return active.Count;
		}

		public async Task<IReadOnlyList<ToDoItem>> FindAsync(
			Guid userId, Func<ToDoItem, bool> predicate, CancellationToken ct)
		{
			if (predicate == null) throw new ArgumentNullException(nameof(predicate));

			var all = await GetAllByUserIdAsync(userId, ct);
			return all.Where(predicate).ToList();
		}

		// === Пути ============================================================

		private string GetUserDirectory(Guid userId) =>
			Path.Combine(_basePath, userId.ToString());

		private string GetItemPath(Guid userId, Guid itemId) =>
			Path.Combine(GetUserDirectory(userId), $"{itemId}.json");

		// === I/O для одного ToDoItem =========================================

		private async Task<ToDoItem?> ReadItemAsync(string path, CancellationToken ct)
		{
			await using var fs = File.OpenRead(path);
			return await JsonSerializer.DeserializeAsync<ToDoItem>(fs, _jsonOptions, ct);
		}

		private async Task WriteItemAsync(string path, ToDoItem item, CancellationToken ct)
		{
			await using var fs = File.Create(path);
			await JsonSerializer.SerializeAsync(fs, item, _jsonOptions, ct);
		}

		// === Индекс ==========================================================

		/// <summary>
		/// Возвращает индекс под блокировкой (для операций чтения, которым нужен индекс).
		/// </summary>
		private async Task<Dictionary<Guid, Guid>> GetOrBuildIndexAsync(CancellationToken ct)
		{
			await _gate.WaitAsync(ct);
			try
			{
				return await LoadOrBuildIndexAsync(ct);
			}
			finally { _gate.Release(); }
		}

		/// <summary>
		/// Загружает индекс с диска. Если файла нет — строит его сканированием
		/// всех папок и сохраняет на диск.
		/// Вызывается ВНУТРИ уже взятой блокировки _gate.
		/// </summary>
		private async Task<Dictionary<Guid, Guid>> LoadOrBuildIndexAsync(CancellationToken ct)
		{
			if (File.Exists(_indexPath))
			{
				await using var fs = File.OpenRead(_indexPath);
				var loaded = await JsonSerializer.DeserializeAsync<Dictionary<Guid, Guid>>(
					fs, _jsonOptions, ct);
				return loaded ?? new Dictionary<Guid, Guid>();
			}

			// Файла нет — строим индекс сканированием
			var index = BuildIndexFromDisk();
			await SaveIndexAsync(index, ct);
			return index;
		}

		/// <summary>
		/// Сканирует все подпапки вида {basePath}/{UserId}/{ItemId}.json
		/// и собирает словарь ItemId → UserId.
		/// </summary>
		private Dictionary<Guid, Guid> BuildIndexFromDisk()
		{
			var index = new Dictionary<Guid, Guid>();

			foreach (var userDir in Directory.EnumerateDirectories(_basePath))
			{
				var dirName = Path.GetFileName(userDir);
				if (!Guid.TryParse(dirName, out var userId))
					continue; // пропускаем посторонние папки

				foreach (var file in Directory.EnumerateFiles(userDir, "*.json"))
				{
					var fileName = Path.GetFileNameWithoutExtension(file);
					if (Guid.TryParse(fileName, out var itemId))
						index[itemId] = userId;
				}
			}

			return index;
		}

		private async Task SaveIndexAsync(Dictionary<Guid, Guid> index, CancellationToken ct)
		{
			await using var fs = File.Create(_indexPath);
			await JsonSerializer.SerializeAsync(fs, index, _jsonOptions, ct);
		}
	}
}