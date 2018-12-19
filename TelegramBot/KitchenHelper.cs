using System;
using System.Collections.Generic;
using System.Linq;
using System.Data.SqlClient;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Bot.Types;

namespace TelegramBot
{
    public class KitchenHelper
    {
        private string token = "708214216:AAH2JC9sa5gUvI4pqp5kbmGWV40N0DwdDNQ";
        static TelegramBotClient Bot;
        List<MyUser> UsersList;

        public KitchenHelper()
        {
            UsersList = new List<MyUser>();
        }

        public void TestApiAsync()
        {
            try
            {
                Bot = new TelegramBotClient(token);

                Thread newThread = new Thread(ReceiveMessage);
                newThread.Start();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        //проверка на наличие юзера в списке
        public bool CheckUser(int userId)
        {
            foreach (var user in UsersList)
                if (user.Id == userId)
                    return true;
            return false;
        }

        private async void ReceiveMessage()
        {
            MyUser tmpUser = new MyUser(Bot.GetMeAsync().Result);

            //проверка на наличие юзера в списке
            if (!CheckUser(tmpUser.Id))
                UsersList.Add(tmpUser);

            Console.WriteLine(
              $"Hello, World! I am user {tmpUser.Id} and my name is {tmpUser.Name}."
            );

            var lastMessageId = 0;
            SqlDataReader getReader;
            SqlCommand myCommand;
            List<string> products;
            String temp;
            String[] productsArr;

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

                            case "/fridge":
                                SqlConnection conn = new SqlConnection("server=localhost;" +
                                       "Trusted_Connection=yes;" +
                                       "database=TelegramBot;");
                                conn.Open();

                                try
                                {
                                    SqlDataReader myReader = null;
                                    myCommand = new SqlCommand("select * from Fridge where id = " + last.Message.From.Id,
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
                                        getReader = null;
                                        myCommand = new SqlCommand("select * from Fridge where id = " + last.Message.From.Id,
                                                                             conn);
                                        products = new List<string>();
                                        getReader = myCommand.ExecuteReader();
                                        Console.WriteLine(getReader.HasRows);
                                        getReader.Read();
                                        temp = getReader.GetString(1);
                                        Console.WriteLine(temp == null);
                                        productsArr = temp.Split(';');
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


                            case "/fridge_add":
                                    conn = new SqlConnection("server=localhost;" +
                                               "Trusted_Connection=yes;" +
                                               "database=TelegramBot;");
                                    conn.Open();
                                    myCommand = new SqlCommand("select * from Fridge where id = " + last.Message.From.Id,
                                                                         conn);
                                    getReader = myCommand.ExecuteReader();
                                    products = new List<string>();
                                    Console.WriteLine(getReader.HasRows);
                                    getReader.Read();
                                    temp = getReader.GetString(1);
                                    productsArr = temp.Split(';');
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
