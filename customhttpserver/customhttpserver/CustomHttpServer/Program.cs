using CustomHttpServer.Core;
using CustomHttpServer.Framework.Configuration;

/*
 * 1) Чтобы сервер не запускался, если settings.json отсутствует.
 * 2) Если файл настроек некорректен, показать сообщение об ошибке и завершить работу.
 * 3) Работу с настройками реализовать через паттерн Singlton
 * 4) HTML нужно брать из файла static/index.html, путь к которому хранится в settings.json.
 */

var config = ConfigurationManager.GetInstance().Config;

if (config is null)
{
    Console.WriteLine("Сервер не запущен из-за отсутствия корректного файла настроек.");
    return;
}

var server = new HttpServer(config);
server.Start();

Console.WriteLine("Введите 'stop' для остановки сервера.");
while (true)
{
    string? command = Console.ReadLine();
    if (command?.ToLower() == "stop")
    {
        server.Stop();
        break;
    }
}