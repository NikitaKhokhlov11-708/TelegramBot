using System;

namespace TelegramBot
{
    class Program
    {
        static void Main(string[] args)
        {
            //инициализация бота
            KitchenHelper bot = new KitchenHelper();
            //запуск
            bot.TestApiAsync();
            Console.ReadLine();
        }
    }
}
