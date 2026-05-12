using Otus.ToDoList.ConsoleBot.Types;

namespace Otus.ToDoList.ConsoleBot;
/// <summary>
///  Интерфейс обработчика обновлений
/// </summary>
public interface IUpdateHandler
{
    void HandleUpdateAsync(ITelegramBotClient botClient, Update update);
}
