using System;

namespace MiniHttpServer.Framework.ServerClasses;

public class ConsoleListener
{
    private Server? Server{get;}
    public ConsoleListener(Server server)
    {
        Server = server;
    }
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
