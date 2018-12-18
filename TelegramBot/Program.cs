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
                                    SqlCommand myCommand = new SqlCommand("select * from Fridge where id = " + last.Message.From.Id,
                                                                             conn);
                                    myReader = myCommand.ExecuteReader();

                                    if (!myReader.HasRows)
                                    {
                                        myReader.Close();
                                        myCommand = new SqlCommand("insert into Fridge values (" + last.Message.From.Id + ",'')", conn);
                                        myCommand.ExecuteNonQuery();
                                        message = "Вы успешно зарегистрировались. ";
                                    }
                                    else
                                    {
                                        message = "Ваш холодильник: \n";
                                        myReader.Close();
                                        SqlDataReader getReader = null;
                                        myCommand = new SqlCommand("select * from Fridge where id = " + last.Message.From.Id,
                                                                             conn);
                                        var products = new List<string>();
                                        getReader = myCommand.ExecuteReader();
                                        Console.WriteLine(getReader.HasRows);
                                        getReader.Read();
                                        var temp = getReader.GetString(1);
                                        Console.WriteLine(temp == null);
                                        var productsArr = temp.Split(';');
                                        if (productsArr != null && temp != "")
                                        {
                                            foreach (var product in productsArr)
                                            {
                                                var arr = product.Split(',');
                                                if (arr.Length == 3)
                                                    products.Add(arr[0] + ' ' + arr[1] + ' ' + arr[2]);
                                                else
                                                    continue;
                                            }
                                            foreach (var elem in products)
                                            {
                                                message += elem + "\n";
                                            }
                                        }
                                        else
                                            message += "Пусто\n";
                                    }
                                    message += "\nДобавить ингредиенты можно при помощи команды /fridge_add Название, кол-во, ед. измерения \nПример: \n/fridge_add Томаты, 9, шт";
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

                        if (last.Message.Text.Contains("/fridge_add "))
                        {
                            SqlConnection conn = new SqlConnection("server=localhost;" +
                                       "Trusted_Connection=yes;" +
                                       "database=TelegramBot;");
                            conn.Open();
                            SqlDataReader getReader = null;
                            var myCommand = new SqlCommand("select * from Fridge where id = " + last.Message.From.Id,
                                                                 conn);
                            getReader = myCommand.ExecuteReader();
                            var products = new List<string>();
                            Console.WriteLine(getReader.HasRows);
                            getReader.Read();
                            var temp = getReader.GetString(1);
                            var productsArr = temp.Split(';');
                            Console.WriteLine(productsArr.Length);
                            if (productsArr != null && productsArr[0] != "")
                            {
                                foreach (var product in productsArr)
                                {
                                    var arr = product.Split(',');
                                    if (arr.Length == 3)
                                    {
                                        products.Add(arr[0] + ' ' + arr[1] + ' ' + arr[2]);
                                        Console.WriteLine(product[product.Length - 1]);
                                    }
                                }
                            }
                            getReader.Close();

                            var ingr = last.Message.Text.Substring(12);
                            var parse = ingr.Split(',');
                            string ins = "";
                            if (parse != null && parse.Length == 3)
                            {
                                products.Add(parse[0].Trim() + ' ' + parse[1].Trim() + ' ' + parse[2].Trim());
                                Console.WriteLine(products.Last());
                                foreach (var prod in products)
                                {
                                    var insparse = prod.Split(' ');
                                    ins += insparse[0] + "," + insparse[1] + "," + insparse[2] + ";";
                                }
                                var insCommand = new SqlCommand("update Fridge set products = '" + ins + "' where id = " + last.Message.From.Id, conn);
                                insCommand.ExecuteNonQuery();
                                message = "Продукт \"" + parse[0].Trim() + "\" успешно добавлен!";
                            }
                            else
                                message = "Ошибка";

                            getReader.Close();
                        }


                        await Bot.SendTextMessageAsync(last.Message.From.Id, message);
                    }
                }
                Thread.Sleep(100);
            }
        }
    }
}
