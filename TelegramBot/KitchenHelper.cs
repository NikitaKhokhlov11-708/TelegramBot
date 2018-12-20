﻿using System;
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
        private const string token = "714123426:AAGHsuJ7hA59HXhZhSiHRiaSSY2p1W8UQKI";
        public TelegramBotClient Bot;

        public KitchenHelper()
        {
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

        private async void ReceiveMessage()
        {
            Console.WriteLine(
              $"Hello, World!"
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
                        var rkm = new ReplyKeyboardMarkup();
                        rkm.ResizeKeyboard = true;
                        rkm.Keyboard = new KeyboardButton[][]
                        {
                            new KeyboardButton[]
                            {
                                new KeyboardButton("🥫"),
                                new KeyboardButton("📖"),
                                new KeyboardButton("🛒")
                            }
                        };

                        lastMessageId = last.Id;
                        Console.WriteLine(last.Message.Text);
                        string message = AnalyzeQuery(last);

                        await Bot.SendTextMessageAsync(last.Message.From.Id, message, replyMarkup: rkm);
                    }
                }
                Thread.Sleep(100);
            }
        }

        private string AnalyzeQuery(Update update)
        {
            var message = "Неизвестная команда.";
            var query = update.Message.Text;
            
            switch (query)
            {
                case "/start":
                    message = CommandStart();
                    break;

                case "🥫":
                    message = CommandFridge(update);
                    break;

                case "📖":
                    message = CommandRecipes(update);
                    break;

                case "🛒":
                    break;

                default:
                    if (query.Contains("/fridge_add "))
                        message = CommandAddProduct(update);
                    else if (query.Contains("/fridge_remove "))
                        message = CommandRemoveProduct(update);
                    else if (query.Contains("/recipe_add "))
                        message = CommandAddRecipe(update);
                    else if (query.Contains("/recipe_remove "))
                        message = CommandRemoveRecipe(update);
                    else if (query.Contains("/recipe_ingr_"))
                        message = CommandAddRecipeIngr(update);
                    else if (query.Contains("/recipe_text_"))
                        message = CommandAddRecipeText(update);
                    else if (query.Contains("/recipe_show_"))
                        message = CommandShowRecipe(update);
                    else
                        message = "Неизвестная команда";
                    break;
            }

            return message;
        }

        private string CommandStart()
        {
            return "Здравствуйте! Я — кухонный бот-помощник. Для регистрации нажмите на 🥫.";
        }

        private string CommandFridge(Update update)
        {
            string message = "";
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
                    var products = GetProducts(update);
                    if (products != null)
                    {
                        foreach (var elem in products)
                        {
                            message += (products.IndexOf(elem) + 1).ToString() + ". " + elem.Name + ", " + elem.Amount + " " + elem.Unit + "\n";
                        }
                    }
                    else
                        message += "Пусто\n";
                }
                message += "\nДобавить продукты можно при помощи команды /fridge_add Название, кол-во, ед. измерения \nПример: \n/fridge_add Томаты, 9, шт\n";
                message += "\nУдалить продукты можно при помощи команды /fridge_remove Название, кол-во, ед. измерения \nПример: \n/fridge_remove Томаты, 9, шт";
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
            conn.Close();

            return message;
        }

        private string CommandAddProduct(Update update)
        {
            string message = "";
            bool flag = false;
            SqlConnection conn = new SqlConnection("server=localhost;" + "Trusted_Connection=yes;" + "database=TelegramBot;");
            conn.Open();
            var products = GetProducts(update);
            var ingr = update.Message.Text.Substring(12);
            var parse = ingr.Split(',');
            string ins = "";

            for (int i = 0; i < parse.Length; i++)
            {
                parse[i] = parse[i].Trim();
            }
            
            if (parse != null && parse.Length == 3 && int.Parse(parse[1]) > 0)
            {
                for (int i = 0; i < products.Count(); i++)
                {
                    if (products[i].Name.ToLower() == parse[0].ToLower() && products[i].Unit.ToLower() == parse[2].ToLower())
                    {
                        var t = new Product(products[i].Name, products[i].Amount + int.Parse(parse[1]), products[i].Unit);
                        products[i] = t;
                        flag = true;
                    }
                    if (flag)
                        break;
                }
                if (!flag)
                    products.Add(new Product(parse[0], int.Parse(parse[1]), parse[2]));

                foreach (var prod in products)
                {
                    ins += prod.Name + "," + prod.Amount + "," + prod.Unit + ";";
                }
                var insCommand = new SqlCommand("update Fridge set products = '" + ins + "' where id = " + update.Message.From.Id, conn);
                insCommand.ExecuteNonQuery();
                message = "Продукт \"" + parse[0] + "\" успешно добавлен!";
            }
            else
                message = "Ошибка";
            conn.Close();

            return message;
        }

        private string CommandRemoveProduct(Update update)
        {
            string message = "";
            string ins = "";
            bool flag = false;
            SqlConnection conn = new SqlConnection("server=localhost;" + "Trusted_Connection=yes;" + "database=TelegramBot;");
            conn.Open();
            var ingr = update.Message.Text.Substring(15);
            var parse = ingr.Split(',');
            var products = GetProducts(update);

            for (int i = 0; i < parse.Length; i++)
            {
                parse[i] = parse[i].Trim();
            }
            
            if (parse != null && parse.Length == 3 && int.Parse(parse[1]) > 0)
            {
                for (int i = 0; i < products.Count(); i++)
                {
                    if (products[i].Name.ToLower() == parse[0].ToLower() && products[i].Unit.ToLower() == parse[2].ToLower() && (products[i].Amount - int.Parse(parse[1]) >= 0))
                    {
                        var t = new Product(products[i].Name, products[i].Amount - int.Parse(parse[1]), products[i].Unit);
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
                        if (prod.Amount > 0)
                            ins += prod.Name + "," + prod.Amount + "," + prod.Unit + ";";
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
            conn.Close();

            return message;
        }

        private string CommandRecipes(Update update)
        {
            string message = "";
            SqlConnection conn = new SqlConnection("server=localhost;" +
                                       "Trusted_Connection=yes;" +
                                       "database=TelegramBot;");
            conn.Open();

            try
            {
                SqlDataReader myReader = null;
                SqlCommand myCommand = new SqlCommand("select * from Recipe where Fridge_ID = " + update.Message.From.Id,
                                                         conn);
                myReader = myCommand.ExecuteReader();
                message = "Ваши рецепты: \n";
                if (!myReader.HasRows)
                {
                    myReader.Close();
                    message += "Пусто\n";
                }
                else
                {
                    myReader.Close();
                    var meals = GetMeals(update);

                    if (meals != null)
                    {
                        foreach (var m in meals)
                        {
                            message += (meals.IndexOf(m) + 1).ToString() + ". " + m.Name + "\n Показать рецепт: /recipe_show_" + (meals.IndexOf(m) + 1).ToString() + "\n";
                        }
                    }
                    else
                        message += "Пусто\n";
                }
                message += "\nДобавить рецепт можно при помощи команды /recipe_add Название \nПример: \n/recipe_add Пицца с ветчиной\n";
                message += "\nУдалить рецепт можно при помощи команды /recipe_remove Название \nПример: \n/recipe_remove Пицца с ветчиной\n";
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
            conn.Close();

            return message;
        }

        private string CommandAddRecipe(Update update)
        {
            string message = "";
            bool flag = false;
            SqlConnection conn = new SqlConnection("server=localhost;" + "Trusted_Connection=yes;" + "database=TelegramBot;");
            conn.Open();
            var meals = GetMeals(update);
            var name = update.Message.Text.Substring(12).Trim();

            if (name != null)
            {
                Meal add;
                for (int i = 0; i < meals.Count(); i++)
                {
                    if (meals[i].Name.ToLower() == name.ToLower())
                    {
                        flag = true;
                    }
                    if (flag)
                    {
                        message = "Ошибка";
                        break;
                    }
                        
                }
                if (!flag)
                {
                    add = new Meal(name, new List<Product>(), "");
                    meals.Add(add);
                    var insCommand = new SqlCommand("insert into Recipe values ( '" + name + "'," + update.Message.From.Id + ",'','')", conn);
                    insCommand.ExecuteNonQuery();
                    message = "Рецепт \"" + name + "\" успешно добавлен!\nДля добавления ингредиентов, необходимых для приготовления этого блюда, используйте команду\n/recipe_ingr_" + (meals.IndexOf(add) + 1).ToString() + " Название, кол-во, ед. измерения; Название, кол-во, ед. измерения;\nПример: \n/recipe_ingr_" + (meals.IndexOf(add) + 1).ToString() + " Томаты, 9, шт; Молоко, 5, л\n";
                }
            }
            else
                message = "Ошибка";

            conn.Close();
            return message;
        }

        private string CommandRemoveRecipe(Update update)
        {
            string message = "";
            bool flag = false;
            int ind = -1;
            SqlConnection conn = new SqlConnection("server=localhost;" + "Trusted_Connection=yes;" + "database=TelegramBot;");
            conn.Open();
            var meals = GetMeals(update);
            var name = update.Message.Text.Substring(15).Trim();

            if (name != null)
            {
                for (int i = 0; i < meals.Count(); i++)
                {
                    if (meals[i].Name.ToLower() == name.ToLower())
                    {
                        ind = i;
                        flag = true;
                    }
                }
                if (flag)
                {
                    meals.Remove(meals[ind]);
                    var insCommand = new SqlCommand("delete from Recipe where Name = '" + name + "' AND Fridge_ID = " + update.Message.From.Id, conn);
                    insCommand.ExecuteNonQuery();
                    message = "Рецепт \"" + name + "\" успешно удален!";
                }
                else
                    message = "Данный рецепт не найден";
            }
            else
                message = "Ошибка";

            conn.Close();
            return message;
        }

        private string CommandAddRecipeIngr(Update update)
        {
            string message = "";
            List<Meal> meals = GetMeals(update);
            List<Product> products = new List<Product>();
            SqlConnection conn = new SqlConnection("server=localhost;" + "Trusted_Connection=yes;" + "database=TelegramBot;");
            conn.Open();
            int space = update.Message.Text.IndexOf(" ");
            var ingr = update.Message.Text.Substring(space + 1).Trim();

            var productsArr = ingr.Split(';');
            if (productsArr != null && productsArr[0] != "")
            {
                foreach (var product in productsArr)
                {
                    var arr = product.Split(',');
                    if (arr.Length == 3)
                    {
                        products.Add(new Product(arr[0], int.Parse(arr[1]), arr[2]));
                    }
                }
            }
            else message = "Пусто";

            int id = int.Parse(update.Message.Text.Substring(13, space - 13)) - 1;
            if (id < meals.Count)
            {
                string ins = "";
                foreach (var prod in products)
                {
                    if (prod.Amount > 0)
                        ins += prod.Name + "," + prod.Amount + "," + prod.Unit + ";";
                }
                var insCommand = new SqlCommand("update Recipe set Ingridients = '" + ins + "'  where Name = '" + meals[id].Name + "' AND Fridge_ID = " + update.Message.From.Id, conn);
                insCommand.ExecuteNonQuery();
                message = "Ингредиенты у блюда \"" + meals[id].Name + "\"успешно обновлены!\nДля добавления текста этого рецепта используйте команду\n/recipe_text_" + (id + 1).ToString() + " Текст рецепта\nПример: \n/recipe_text_" + (id + 1).ToString() + " Сварить 5 пельменей.\n";
            }
            else
                message = "Ошибка";

            conn.Close();
            return message;
        }

        private string CommandAddRecipeText(Update update)
        {
            string message = "";
            List<Meal> meals = GetMeals(update);
            SqlConnection conn = new SqlConnection("server=localhost;" + "Trusted_Connection=yes;" + "database=TelegramBot;");
            conn.Open();
            int space = update.Message.Text.IndexOf(" ");
            var text = update.Message.Text.Substring(space + 1).Trim();

            int id = int.Parse(update.Message.Text.Substring(13, space - 13)) - 1;
            if (text != null && text != "")
            {
                var insCommand = new SqlCommand("select * from Recipe where Name = '" + meals[id].Name + "' AND Fridge_ID = " + update.Message.From.Id, conn);
                insCommand.ExecuteNonQuery();
                message = "Ингредиенты у блюда \"" + meals[id].Name + "\"успешно обновлены!\nДля добавления текста этого рецепта используйте команду\n/recipe_text_" + (id + 1).ToString() + " Текст рецепта\nПример: \n/recipe_text_" + (id + 1).ToString() + " Сварить 5 пельменей.\n";
            }
            else
                message = "Ошибка";

            conn.Close();
            return message;
        }

        private string CommandShowRecipe(Update update)
        {
            string message = "";
            List<Meal> meals = GetMeals(update);
            SqlConnection conn = new SqlConnection("server=localhost;" + "Trusted_Connection=yes;" + "database=TelegramBot;");
            conn.Open();
            int id = int.Parse(update.Message.Text.Substring(13, update.Message.Text.Length - 13)) - 1;
            if (id < meals.Count)
            {
                message += meals[id].Name + "\n\nНеобходимые ингредиенты:\n";
                foreach (var elem in meals[id].Ingredients)
                {
                    message += elem.Name + ", " + elem.Amount + " " + elem.Unit + "\n";
                }
                message += "\n" + meals[id].Receipe;
            }
            else
                message = "Ошибка";

            conn.Close();

            return message;
        }


        private List<Meal> GetMeals(Update update)
        {
            SqlConnection conn = new SqlConnection("server=localhost;" + "Trusted_Connection=yes;" + "database=TelegramBot;");
            conn.Open();
            var meals = new List<Meal>();
            SqlDataReader getReader = null;
            var myCommand = new SqlCommand("select * from Recipe where Fridge_ID = " + update.Message.From.Id, conn);
            getReader = myCommand.ExecuteReader();
            
            while (getReader.Read())
            {
                meals.Add(new Meal(getReader.GetString(1), GetMealIngr(update,getReader.GetString(1)), getReader.GetString(4)));
            }
            getReader.Close();
            conn.Close();
            return meals;
        }

        private List<Product> GetProducts(Update update)
        {
            SqlConnection conn = new SqlConnection("server=localhost;" + "Trusted_Connection=yes;" + "database=TelegramBot;");
            conn.Open();

            var products = new List<Product>();
            SqlDataReader getReader = null;
            var myCommand = new SqlCommand("select * from Fridge where id = " + update.Message.From.Id,
                                                 conn);
            getReader = myCommand.ExecuteReader();
            
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
                        products.Add(new Product(arr[0], int.Parse(arr[1]), arr[2]));
                    }
                }
            }
            getReader.Close();
            conn.Close();

            return products;
        }

        private List<Product> GetMealIngr(Update update, string name)
        {
            SqlConnection conn = new SqlConnection("server=localhost;" + "Trusted_Connection=yes;" + "database=TelegramBot;");
            conn.Open();
            var products = new List<Product>();
            SqlDataReader getReader = null;
            var myCommand = new SqlCommand("select * from Recipe where Fridge_ID = " + update.Message.From.Id + " AND Name = '" + name + "'",
                                                 conn);
            getReader = myCommand.ExecuteReader();

            getReader.Read();
            var temp = getReader.GetString(3);
            var productsArr = temp.Split(';');
            if (productsArr != null && productsArr[0] != "")
            {
                foreach (var product in productsArr)
                {
                    var arr = product.Split(',');
                    if (arr.Length == 3)
                    {
                        products.Add(new Product(arr[0], int.Parse(arr[1]), arr[2]));
                    }
                }
            }
            getReader.Close();
            conn.Close();

            return products;
        }
    }
}
