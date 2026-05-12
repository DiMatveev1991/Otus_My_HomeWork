using Otus.ToDoList.ConsoleBot.Types;

namespace Otus.ToDoList.ConsoleBot;
/// <summary>
/// Интерфейс клиента для будущего телеграм-бота
/// </summary>
public interface ITelegramBotClient
{
    void StartReceiving(IUpdateHandler handler);
    void SendMessage(Chat chat, string text);
}
