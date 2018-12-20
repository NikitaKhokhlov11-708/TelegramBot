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
        private const string token = "647547033:AAHy8ZNwQRDBq7B1VocSbWjFHo_jCeB9Bqs";
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
                            message += (meals.IndexOf(m) + 1).ToString() + ". " + m.Name + "\n";
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
                    message = "Рецепт \"" + name + "\" успешно добавлен!\nДля добавления ингредиентов, необходимых для приготовления этого блюда, используйте команду\n/recipe_ingr_" + (meals.IndexOf(add) + 1).ToString() + " Название, кол-во, ед. измерения; Название, кол-во, ед. измерения;\nПример: \n/recipe_ingr_1 Томаты, 9, шт; Молоко, 5, л\n";
                }
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
                meals.Add(new Meal(getReader.GetString(1), GetMealIngr(update,getReader.GetString(1)), getReader.GetString(3)));
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
