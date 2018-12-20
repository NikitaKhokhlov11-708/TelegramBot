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
        private string token = "783289055:AAGzkG4PHu8fA0RaPvk8E7Qp2TorRCWdzt0";
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
                        string message = AnalyzeQuery(last);

                        await Bot.SendTextMessageAsync(last.Message.From.Id, message);
                    }
                }
                Thread.Sleep(100);
            }
        }

        public String AnalyzeQuery(Update update)
        {
            var message = "Неизвестная команда.";
            var query = update.Message.Text;
            if (query == "/start")
                message = "Здравствуйте! Я — кухонный бот-помощник.";

            if (query == "/fridge")
            {
                SqlConnection conn = new SqlConnection("server=localhost;" +
                                       "Trusted_Connection=yes;" +
                                       "database=TelegramBot;");
                conn.Open();

                try
                {
                    SqlDataReader myReader = null;
                    SqlCommand myCommand = new SqlCommand("select * from Fridge where id = " + update.Message.From.Id,
                                                             conn);
                    myReader = myCommand.ExecuteReader();

                    if (!myReader.HasRows)
                    {
                        myReader.Close();
                        myCommand = new SqlCommand("insert into Fridge values (" + update.Message.From.Id + ",'')", conn);
                        myCommand.ExecuteNonQuery();
                        message = "Вы успешно зарегистрировались. ";
                    }
                    else
                    {
                        message = "Ваш холодильник: \n";
                        myReader.Close();
                        SqlDataReader getReader = null;
                        myCommand = new SqlCommand("select * from Fridge where id = " + update.Message.From.Id,
                                                             conn);
                        var products = new List<Tuple<string, string, string>>();
                        getReader = myCommand.ExecuteReader();
                        getReader.Read();
                        var temp = getReader.GetString(1);
                        var productsArr = temp.Split(';');
                        if (productsArr != null && temp != "")
                        {
                            foreach (var product in productsArr)
                            {
                                var arr = product.Split(',');
                                if (arr.Length == 3)
                                    products.Add(new Tuple<string,string,string>(arr[0],arr[1], arr[2]));
                                else
                                    continue;
                            }
                            foreach (var elem in products)
                            {
                                message += (products.IndexOf(elem) + 1).ToString() + ". " + elem.Item1 + ", " + elem.Item2 + " " + elem.Item3 + "\n";
                            }
                        }
                        else
                            message += "Пусто\n";
                    }
                    message += "\nДобавить ингредиенты можно при помощи команды /fridge_add Название, кол-во, ед. измерения \nПример: \n/fridge_add Томаты, 9, шт";
                    message += "\nУдалить ингредиенты можно при помощи команды /fridge_remove Название, кол-во, ед. измерения \nПример: \n/fridge_remove Томаты, 9, шт";
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                }
                conn.Close();
            }

            if (query.Contains("/fridge_add "))
            {
                SqlConnection conn = new SqlConnection("server=localhost;" +
                           "Trusted_Connection=yes;" +
                           "database=TelegramBot;");
                conn.Open();
                SqlDataReader getReader = null;
                var myCommand = new SqlCommand("select * from Fridge where id = " + update.Message.From.Id,
                                                     conn);
                getReader = myCommand.ExecuteReader();
                var products = new List<Tuple<string, string, string>>();
                getReader.Read();
                var temp = getReader.GetString(1);
                var productsArr = temp.Split(';');
                if (productsArr != null && productsArr[0] != "")
                {
                    foreach (var product in productsArr)
                    {
                        var arr = product.Split(',');
                        if (arr.Length == 3)
                        {
                            products.Add(new Tuple<string, string, string>(arr[0], arr[1], arr[2]));
                        }
                    }
                }
                getReader.Close();

                var ingr = update.Message.Text.Substring(12);
                var parse = ingr.Split(',');
                for (int i = 0; i < parse.Length; i++)
                {
                    parse[i] = parse[i].Trim();
                }
                string ins = "";
                if (parse != null && parse.Length == 3 && int.Parse(parse[1]) > 0)
                {
                    bool flag = false;
                    for (int i = 0; i < products.Count(); i++) {
                        if (products[i].Item1.ToLower() == parse[0].ToLower() && products[i].Item3.ToLower() == parse[2].ToLower())
                        {
                            var t = new Tuple<string, string, string>(products[i].Item1, (int.Parse(products[i].Item2) + int.Parse(parse[1])).ToString(), products[i].Item3);
                            products[i] = t;
                            flag = true;
                        }
                        if (flag)
                            break;
                    }
                    if (!flag)
                        products.Add(new Tuple<string, string, string>(parse[0], parse[1], parse[2]));

                    foreach (var prod in products)
                    {
                        ins += prod.Item1 + "," + prod.Item2 + "," + prod.Item3 + ";";
                    }
                    var insCommand = new SqlCommand("update Fridge set products = '" + ins + "' where id = " + update.Message.From.Id, conn);
                    insCommand.ExecuteNonQuery();
                    message = "Продукт \"" + parse[0] + "\" успешно добавлен!";
                }
                else
                    message = "Ошибка";

                getReader.Close();
            }

            if (query.Contains("/fridge_remove "))
            {
                SqlConnection conn = new SqlConnection("server=localhost;" +
                           "Trusted_Connection=yes;" +
                           "database=TelegramBot;");
                conn.Open();
                SqlDataReader getReader = null;
                var myCommand = new SqlCommand("select * from Fridge where id = " + update.Message.From.Id,
                                                     conn);
                getReader = myCommand.ExecuteReader();
                var products = new List<Tuple<string, string, string>>();
                getReader.Read();
                var temp = getReader.GetString(1);
                var productsArr = temp.Split(';');
                if (productsArr != null && productsArr[0] != "")
                {
                    foreach (var product in productsArr)
                    {
                        var arr = product.Split(',');
                        if (arr.Length == 3)
                        {
                            products.Add(new Tuple<string, string, string>(arr[0], arr[1], arr[2]));
                        }
                    }
                }
                getReader.Close();

                var ingr = update.Message.Text.Substring(15);
                var parse = ingr.Split(',');
                for (int i = 0; i < parse.Length; i++)
                {
                    parse[i] = parse[i].Trim();
                }
                string ins = "";
                bool flag = false;
                if (parse != null && parse.Length == 3 && int.Parse(parse[1]) > 0)
                {
                    for (int i = 0; i < products.Count(); i++)
                    {
                        if (products[i].Item1.ToLower() == parse[0].ToLower() && products[i].Item3.ToLower() == parse[2].ToLower() && (int.Parse(products[i].Item2) - int.Parse(parse[1])) >= 0)
                        {
                            var t = new Tuple<string, string, string>(products[i].Item1, (int.Parse(products[i].Item2) - int.Parse(parse[1])).ToString(), products[i].Item3);
                            products[i] = t;
                            flag = true;
                        }
                        if (flag)
                            break;
                    }

                    if (flag)
                    {
                        foreach (var prod in products)
                        {
                            if (prod.Item2 != "0")
                                ins += prod.Item1 + "," + prod.Item2 + "," + prod.Item3 + ";";
                        }
                        var insCommand = new SqlCommand("update Fridge set products = '" + ins + "' where id = " + update.Message.From.Id, conn);
                        insCommand.ExecuteNonQuery();
                        message = "Продукт \"" + parse[0] + "\" в количестве " + parse[1] + " успешно удален!";
                    }
                    else
                        message = "Продукт отсутствует";
                }
                else
                    message = "Ошибка";

                getReader.Close();
            }
            return message;
        }
    }
}
