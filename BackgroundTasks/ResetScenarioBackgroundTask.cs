using System;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using TelegramBot;
using TelegramBot.Scenarios;

namespace BackgroundTasks
{
	public class ResetScenarioBackgroundTask(
		TimeSpan resetScenarioTimeout,
		IScenarioContextRepository scenarioRepository,
		ITelegramBotClient bot)
		: BackgroundTask(TimeSpan.FromHours(1), nameof(ResetScenarioBackgroundTask))
	{
		protected override async Task Execute(CancellationToken ct)
		{
			var contexts = await scenarioRepository.GetContexts(ct);
			var now = DateTime.UtcNow;

			foreach (var context in contexts)
			{
				ct.ThrowIfCancellationRequested();

				if (now - context.CreatedAt < resetScenarioTimeout)
					continue;

				await scenarioRepository.ResetContext(context.UserId, ct);
				await bot.SendMessage(
					chatId: context.UserId,
					text: $"Сценарий отменен, так как не поступил ответ в течение {resetScenarioTimeout}",
					replyMarkup: KeyboardFactory.PostRegistration,
					cancellationToken: ct);
			}
		}
	}
}
