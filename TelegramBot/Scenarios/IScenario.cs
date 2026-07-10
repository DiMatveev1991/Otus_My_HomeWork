using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace TelegramBot.Scenarios
{
	/// <summary>
	/// Пошаговый сценарий обработки обновлений пользователя.
	/// Принимает Update целиком, чтобы иметь доступ и к Message, и к CallbackQuery.
	/// </summary>
	public interface IScenario
	{
		bool CanHandle(ScenarioType scenario);

		Task<ScenarioResult> HandleMessageAsync(
			ITelegramBotClient bot, ScenarioContext context, Update update, CancellationToken ct);
	}
}
