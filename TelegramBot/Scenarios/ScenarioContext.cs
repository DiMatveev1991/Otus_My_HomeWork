using System.Collections.Generic;

namespace TelegramBot.Scenarios
{
	/// <summary>
	/// Промежуточное состояние сценария для конкретного пользователя.
	/// Хранит текущий шаг и накопленные данные между сообщениями.
	/// </summary>
	public class ScenarioContext
	{
		/// <summary>Идентификатор пользователя в Telegram.</summary>
		public long UserId { get; set; }

		public ScenarioType CurrentScenario { get; set; }

		/// <summary>Текущий шаг сценария. null — сценарий только начат.</summary>
		public string? CurrentStep { get; set; }

		/// <summary>Данные, накопленные в ходе сценария (пользователь, название задачи и т.д.).</summary>
		public Dictionary<string, object> Data { get; set; }

		/// <summary>Время создания контекста в UTC.</summary>
		public DateTime CreatedAt { get; }

		public ScenarioContext(ScenarioType scenario)
		{
			CurrentScenario = scenario;
			CurrentStep = null;
			Data = new Dictionary<string, object>();
			CreatedAt = DateTime.UtcNow;
		}
	}
}
