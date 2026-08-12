using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
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
			ct.ThrowIfCancellationRequested();
			_contexts.TryGetValue(userId, out var context);
			return Task.FromResult(context);
		}

		public Task<IReadOnlyList<ScenarioContext>> GetContexts(CancellationToken ct)
		{
			ct.ThrowIfCancellationRequested();
			IReadOnlyList<ScenarioContext> contexts = _contexts.Values.ToArray();
			return Task.FromResult(contexts);
		}

		public Task SetContext(long userId, ScenarioContext context, CancellationToken ct)
		{
			ct.ThrowIfCancellationRequested();
			context.UserId = userId;
			_contexts[userId] = context;
			return Task.CompletedTask;
		}

		public Task ResetContext(long userId, CancellationToken ct)
		{
			ct.ThrowIfCancellationRequested();
			_contexts.TryRemove(userId, out _);
			return Task.CompletedTask;
		}
	}
}
