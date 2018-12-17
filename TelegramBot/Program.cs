using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types.ReplyMarkups;

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

    class KitchenHelper
    {
        private string token = "783289055:AAGzkG4PHu8fA0RaPvk8E7Qp2TorRCWdzt0";
        static TelegramBotClient Bot;

        public void TestApiAsync()
        {
            try
            {
                Bot = new TelegramBotClient(token);
                var me = Bot.GetMeAsync().Result;
                Console.WriteLine(
                  $"Hello, World! I am user {me.Id} and my name is {me.FirstName}."
                );

                Thread newThread = new Thread(ReceiveMessage);
                newThread.Start();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Hello! My name is " + ex.Message);
            }
        }

        private async void ReceiveMessage()
        {
            var lastMessageId = 0;
            while (true)
            {
                var messages = await Bot.GetUpdatesAsync();
                if (messages.Length > 0)
                {
                    var last = messages[messages.Length - 1];
                    if (lastMessageId != last.Id && last.Message != null)
                    {
                        lastMessageId = last.Id;
                        Console.WriteLine(last.Message.Text);
                        string message = "";

                        switch (last.Message.Text)
                        {
                            case "/start":
                                message = "Здравствуйте! Я — кухонный бот-помощник.";
                                break;

                            case "/hello":
                                message = "Привет!";
                                break;

                            case "/fridge":
                                SqlConnection conn = new SqlConnection("server=localhost;" +
                                       "Trusted_Connection=yes;" +
                                       "database=TelegramBot;");
                                conn.Open();

                                try
                                {
                                    SqlDataReader myReader = null;
                                    SqlCommand myCommand = new SqlCommand("select * from Users where id = " + last.Message.From.Id,
                                                                             conn);
                                    myReader = myCommand.ExecuteReader();
                                    if (!myReader.HasRows)
                                    {
                                        myReader.Close();
                                        myCommand = new SqlCommand("insert into Users values (" + last.Message.From.Id + ")", conn);
                                        myCommand.ExecuteNonQuery();
                                        message = "Вы успешно зарегистрировались.";
                                    }
                                    else
                                        message = "Ваш холодильник:";
                                }
                                catch (Exception e)
                                {
                                    Console.WriteLine(e.ToString());
                                }
                                conn.Close();
                                break;

                            default:
                                message = "Неизвестная команда.";
                                break;
                        }

                        await Bot.SendTextMessageAsync(last.Message.From.Id, message);
                    }
                }
                Thread.Sleep(100);
            }
        }
    }
}
