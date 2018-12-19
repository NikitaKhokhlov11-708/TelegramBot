using System;

namespace TelegramBot
{
    class Program
    {
        static void Main(string[] args)
        {
            KitchenHelper bot = new KitchenHelper();
            bot.TestApiAsync();
            Console.ReadLine();
        }
    }
}
