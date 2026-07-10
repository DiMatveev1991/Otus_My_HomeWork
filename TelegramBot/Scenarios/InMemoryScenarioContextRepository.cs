using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace TelegramBot.Scenarios
{
	/// <summary>
	/// Реализация <see cref="IScenarioContextRepository"/> в оперативной памяти.
	/// В качестве хранилища используется потокобезопасный
	/// <see cref="ConcurrentDictionary{TKey,TValue}"/>, поскольку HandleUpdateAsync
	/// может вызываться параллельно для разных обновлений.
	/// </summary>
	public class InMemoryScenarioContextRepository : IScenarioContextRepository
	{
		private readonly ConcurrentDictionary<long, ScenarioContext> _contexts = new();

		public Task<ScenarioContext?> GetContext(long userId, CancellationToken ct)
		{
			_contexts.TryGetValue(userId, out var context);
			return Task.FromResult(context);
		}

		public Task SetContext(long userId, ScenarioContext context, CancellationToken ct)
		{
			_contexts[userId] = context;
			return Task.CompletedTask;
		}

		public Task ResetContext(long userId, CancellationToken ct)
		{
			_contexts.TryRemove(userId, out _);
			return Task.CompletedTask;
		}
	}
}
