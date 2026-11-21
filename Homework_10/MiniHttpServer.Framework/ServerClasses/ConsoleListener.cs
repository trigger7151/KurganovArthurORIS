using System;

namespace MiniHttpServer.Framework.ServerClasses;

/// <summary>
/// Управление сервером из консоли командами start, stop, restart и exit.
/// </summary>
public class ConsoleListener
{
    /// <summary>
    /// Управляемый сервер.
    /// </summary>
    private Server? Server{get;}
    /// <summary>
    /// Создаёт консольное управление для сервера.
    /// </summary>
    /// <param name="server">Управляемый сервер.</param>
    public ConsoleListener(Server server)
    {
        Server = server;
    }
    /// <summary>
    /// Читает команды из консоли до команды exit или конца ввода, затем останавливает сервер.
    /// </summary>
    /// <returns>Задача, завершающаяся после выхода из цикла и остановки сервера.</returns>
    public async Task ListenConsoleLoopAsync()
    {
        Console.WriteLine("Команды: start | stop | restart | exit");
        while (true)
        {
            var userInput = Console.ReadLine();
            if (userInput is null)
            {
                break;
            }
            // Переводим в нижний регистр, удаляем пробелы с начала и конца. Ждем пока 
            // выполнится команда и не принимаем текст с консоли
            bool keepGoing = await ExecuteCommandAsync(userInput.Trim().ToLowerInvariant());
            if (!keepGoing) break;
        }

        await Server.StopAsync();
    }
    /// <summary>
    /// Выполняет одну консольную команду.
    /// </summary>
    /// <param name="command">Команда в нижнем регистре без пробелов по краям.</param>
    /// <returns>false, если нужно завершить работу; иначе true.</returns>
    private async Task<bool> ExecuteCommandAsync(string command)
    {
        switch (command)
        {
            case "start":
                Server.Start();
                return true;

            case "stop":
                await Server.StopAsync();
                return true;

            case "restart":
                await Server.StopAsync();
                Server.Start();
                return true;

            case "exit":
            case "quit":
                return false;   

            case "":
                return true;

            default:
                Console.WriteLine("Неизвестная команда. Доступно: start | stop | restart | exit");
                return true;
        }
    }
}
