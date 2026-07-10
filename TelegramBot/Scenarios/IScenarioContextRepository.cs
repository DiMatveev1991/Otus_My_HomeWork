using System.Threading;
using System.Threading.Tasks;

namespace TelegramBot.Scenarios
{
	/// <summary>
	/// Хранилище промежуточного состояния сценариев по пользователям.
	/// </summary>
	public interface IScenarioContextRepository
	{
		Task<ScenarioContext?> GetContext(long userId, CancellationToken ct);
		Task SetContext(long userId, ScenarioContext context, CancellationToken ct);
		Task ResetContext(long userId, CancellationToken ct);
	}
}
