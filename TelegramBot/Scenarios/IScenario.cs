using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace TelegramBot.Scenarios
{
	/// <summary>
	/// Пошаговый сценарий обработки сообщений пользователя.
	/// </summary>
	public interface IScenario
	{
		bool CanHandle(ScenarioType scenario);

		Task<ScenarioResult> HandleMessageAsync(
			ITelegramBotClient bot, ScenarioContext context, Message message, CancellationToken ct);
	}
}
