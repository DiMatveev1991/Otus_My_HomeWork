namespace TelegramBot.Scenarios
{
	/// <summary>
	/// Результат обработки одного шага сценария.
	/// </summary>
	public enum ScenarioResult
	{
		/// <summary>Переход к следующему шагу. Сообщение обработано, но сценарий ещё не завершён.</summary>
		Transition,

		/// <summary>Сценарий завершён.</summary>
		Completed
	}
}
