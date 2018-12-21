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
        //Api бота
        private const string token = "708214216:AAH2JC9sa5gUvI4pqp5kbmGWV40N0DwdDNQ";
        public TelegramBotClient Bot;

        public KitchenHelper()
        {
        }

        //запуск бота
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

        //обработка сообщений пользователя
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
                        //выпадают на выбор 3 смайла, отвечающие за некоторые запросы
                        rkm.Keyboard = new KeyboardButton[][]
                        {
                            new KeyboardButton[]
                            {
                                new KeyboardButton("🥫"),     //холодильник
                                new KeyboardButton("📖"),    //рецепты
                                new KeyboardButton("🛒")     //список покупок
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

        //обработка запросов
        private string AnalyzeQuery(Update update)
        {
            var message = "Неизвестная команда.";
            var query = update.Message.Text;

            switch (query)
            {
                case "/start":
                    //стартовая команда при запуске бота
                    message = CommandStart(update);
                    break;

                case "🥫":
                    //основная команда, создаёт холодильник
                    //когда холодильник создан, выводит его содержимое
                    message = CommandFridge(update);
                    break;

                case "📖":
                    //вывод сохранённых рецептов
                    message = CommandRecipes(update);
                    break;

                case "🛒":
                    //список покупок
                    message = CommandShopping(update);
                    break;

                //команды, вводимые пользователем самостоятельно
                default:
                    //добавление продуктов в холодильник
                    if (query.Contains("/fridge_add "))
                        message = CommandAddProduct(update);
                    //удаление продуктов из холодильника
                    else if (query.Contains("/fridge_remove "))
                        message = CommandRemoveProduct(update);
                    //добавление рецепта
                    else if (query.Contains("/recipe_add "))
                        message = CommandAddRecipe(update);
                    //удаление рецепта
                    else if (query.Contains("/recipe_remove "))
                        message = CommandRemoveRecipe(update);
                    //добавление ингредиента в рецепт
                    else if (query.Contains("/recipe_ingr_"))
                        message = CommandAddRecipeIngr(update);
                    //добавление описания в рецепт
                    else if (query.Contains("/recipe_text_"))
                        message = CommandAddRecipeText(update);
                    //вывод рецепта
                    else if (query.Contains("/recipe_show_"))
                        message = CommandShowRecipe(update);
                    //список покупок
                    else if (query.Contains("/shopping_add "))
                        message = CommandAddShopping(update);
                    //удаление продукта из списка
                    else if (query.Contains("/shopping_remove "))
                        message = CommandRemoveShopping(update);
                    //добавление необходимых продуктов в список покупок
                    else if (query.Contains("/shopping_meal_"))
                        message = BuyForMeal(update);
                    //приготовить рецепт = минус соответствующие продукты из холодильника
                    else if (query.Contains("/cook_"))
                        message = CommandCook(update);
                    else
                        message = "Неизвестная команда";
                    break;
            }

            return message;
        }

        //стартовая команда
        private string CommandStart(Update update)
        {
            string message = "";

            Registration(update);

            return "Здравствуйте! Я — кухонный бот-помощник. Вы успешно зарегистровались! \nИспользуйте команды для продолжения:\n \n" +
                "🥫 выводит содержимое холодильника\n" +
                "📖 вывод сохранённых рецептов\n" +
                "🛒 список покупок\n";
        }

        //регистрация
        private void Registration(Update update)
        {
            //подключение к бд
            SqlConnection conn = new SqlConnection("server=localhost;" +
                                       "Trusted_Connection=yes;" +
                                       "database=TelegramBot;");
            conn.Open();

            var myCommand = new SqlCommand("SELECT * FROM Fridge WHERE id = " + update.Message.From.Id,
                                                     conn);
            var myReader = myCommand.ExecuteReader();

            //добавление поля в таблицу Fridge (холодильник)
            if (!myReader.HasRows)
            {
                myReader.Close();
                //добавление в таблицу Fridge id пользователя
                myCommand = new SqlCommand("INSERT INTO Fridge VALUES (" + update.Message.From.Id + ",'')", conn);
                myCommand.ExecuteNonQuery();
            }

            //добавление поля в таблицу Shopping (списка покупок)
            myCommand = new SqlCommand("SELECT * FROM Shopping WHERE id = " + update.Message.From.Id,
                                                 conn);
            
            myReader = myCommand.ExecuteReader();
            //если список ещё не создан, создаём
            if (!myReader.HasRows)
            {
                myReader.Close();
                myCommand = new SqlCommand("INSERT INTO Shopping VALUES (" + update.Message.From.Id + ",'')", conn);
                myCommand.ExecuteNonQuery();
            }

        }

        //холодильник (первый смайл)
        private string CommandFridge(Update update)
        {
            string message = "";
            //подключение к бд
            SqlConnection conn = new SqlConnection("server=localhost;" +
                                       "Trusted_Connection=yes;" +
                                       "database=TelegramBot;");
            conn.Open();

            try
            {
                SqlDataReader myReader = null;
                SqlCommand myCommand = new SqlCommand("SELECT * FROM Fridge WHERE id = " + update.Message.From.Id,
                                                         conn);
                myReader = myCommand.ExecuteReader();

                //холодильник уже создан, выводим его содержимое

                //строка, которая выведется пользователю
                message = "🥫🥫🥫Ваш холодильник🥫🥫🥫\n";
                //получение продуктов из бд
                var products = GetProducts(update);
                if (products != null)
                {
                    //проход по продуктам
                    foreach (var elem in products)
                    {
                        //добавление в строку продукта
                        //id + название продукта + количество + размерность
                        message += (products.IndexOf(elem) + 1).ToString() + ". " + elem.Name + ", " + elem.Amount + " " + elem.Unit + "\n";
                    }
                }
                else
                    message += "Пусто\n";

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

        //добавление продукта в холодильник /fridge_add
        private string CommandAddProduct(Update update)
        {
            string message = "";
            bool flag = false;
            SqlConnection conn = new SqlConnection("server=localhost;" + "Trusted_Connection=yes;" + "database=TelegramBot;");
            conn.Open();
            //получение продуктов из бд
            var products = GetProducts(update);
            //первые 12 символов "/fridge_add"
            var ingr = update.Message.Text.Substring(12);
            //запрос выглядит так 
            // /fridge_add Томаты, 9, шт
            // мы убрали /fridge_add
            //осталось  "Томаты, 9, шт"
            //разбиваем эту строку на массив из 3 строк по запятым
            var parse = ingr.Split(',');
            string ins = "";

            for (int i = 0; i < parse.Length; i++)
            {
                //убираем лишние пробелы в начале и конце строк
                parse[i] = parse[i].Trim();
            }

            //если введённые данные пользователем корректны, продолжаем
            if (parse != null && parse.Length == 3 && int.Parse(parse[1]) > 0)
            {
                for (int i = 0; i < products.Count(); i++)
                {
                    //если в списке продуктов уже имеется данный продукт, ставим флаг
                    if (products[i].Name.ToLower() == parse[0].ToLower() && products[i].Unit.ToLower() == parse[2].ToLower())
                    {
                        //приплюсовываем parse[1] к количеству объекта
                        var t = new Product(products[i].Name, products[i].Amount + int.Parse(parse[1]), products[i].Unit);
                        products[i] = t;
                        flag = true;
                    }
                    //если нашли, выходим из цикла
                    if (flag)
                        break;
                }
                //если продукта ещё нет в списке, добавляем
                if (!flag)
                    products.Add(new Product(parse[0], int.Parse(parse[1]), parse[2]));

                //обновляем данные о всех продуктах
                foreach (var prod in products)
                {
                    ins += prod.Name + "," + prod.Amount + "," + prod.Unit + ";";
                }
                //sql комманда для обновления содержимого таблицы
                var insCommand = new SqlCommand("UPDATE  Fridge SET products = '" + ins + "' WHERE id = " + update.Message.From.Id, conn);
                insCommand.ExecuteNonQuery();
                message = "Продукт \"" + parse[0] + "\" успешно добавлен!";
            }
            else
                message = "Ошибка";
            conn.Close();

            return message;
        }

        //удаление продукта из холодильника /fridge_remove
        private string CommandRemoveProduct(Update update)
        {
            string message = "";
            string ins = "";
            bool flag = false;
            SqlConnection conn = new SqlConnection("server=localhost;" + "Trusted_Connection=yes;" + "database=TelegramBot;");
            conn.Open();
            //первые 15 символов "/fridge_add"
            var ingr = update.Message.Text.Substring(15);
            var parse = ingr.Split(',');
            //получение продуктов из бд
            var products = GetProducts(update);

            for (int i = 0; i < parse.Length; i++)
            {
                parse[i] = parse[i].Trim();
            }

            if (parse != null && parse.Length == 3 && int.Parse(parse[1]) > 0)
            {
                //ищем продукт в холодильнике
                for (int i = 0; i < products.Count(); i++)
                {
                    if (products[i].Name.ToLower() == parse[0].ToLower() && products[i].Unit.ToLower() == parse[2].ToLower() && (products[i].Amount - int.Parse(parse[1]) >= 0))
                    {
                        //вычитаем parse[1] из количества
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
                        //если количество в продукте корректное, обновляем информацию
                        if (prod.Amount > 0)
                            ins += prod.Name + "," + prod.Amount + "," + prod.Unit + ";";
                    }
                    var insCommand = new SqlCommand("UPDATE  Fridge SET products = '" + ins + "' WHERE id = " + update.Message.From.Id, conn);
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

        //команда, которая выводит сохранённые рецепты (второй смайл)
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
                SqlCommand myCommand = new SqlCommand("SELECT * FROM Recipe WHERE Fridge_ID = " + update.Message.From.Id,
                                                         conn);
                myReader = myCommand.ExecuteReader();
                message = "📖📖📖Ваши рецепты📖📖📖\n";
                //если рецептов нет
                if (!myReader.HasRows)
                {
                    myReader.Close();
                    message += "Пусто\n";
                }
                else
                {
                    myReader.Close();
                    // Meals = рецепты
                    //получение рецептов из бд
                    var meals = GetMeals(update);

                    if (meals != null)
                    {
                        foreach (var m in meals)
                        {
                            //выводит название рецепта
                            //если нужно полное описание, пользователь вводит запрос с /recipe_show_
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

        //добавление рецепта /recipe_remove
        private string CommandAddRecipe(Update update)
        {
            string message = "";
            bool flag = false;
            SqlConnection conn = new SqlConnection("server=localhost;" + "Trusted_Connection=yes;" + "database=TelegramBot;");
            conn.Open();
            //получение рецептов из бд
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
                    //если уже существует рецепт с таким названием, ошибка
                    if (flag)
                    {
                        message = "Ошибка";
                        break;
                    }

                }
                //иначе добавляем
                //в данном методе только название рецепта
                //ингредиенты добавляем в другом методе, в случае если пользователю нужно хранить только название рецепта
                if (!flag)
                {
                    add = new Meal(name, new List<Product>(), "");
                    meals.Add(add);
                    var insCommand = new SqlCommand("INSERT INTO Recipe VALUES ( '" + name + "'," + update.Message.From.Id + ",'','')", conn);
                    insCommand.ExecuteNonQuery();
                    message = "Рецепт \"" + name + "\" успешно добавлен!\nДля добавления ингредиентов, необходимых для приготовления этого блюда, используйте команду\n/recipe_ingr_" + (meals.IndexOf(add) + 1).ToString() + " Название, кол-во, ед. измерения; Название, кол-во, ед. измерения;\nПример: \n/recipe_ingr_" + (meals.IndexOf(add) + 1).ToString() + " Томаты, 9, шт; Молоко, 5, л\n";
                }
            }
            else
                message = "Ошибка";

            conn.Close();
            return message;
        }

        //удаление рецепта  /recipe_remove
        private string CommandRemoveRecipe(Update update)
        {
            string message = "";
            bool flag = false;
            int ind = -1;
            SqlConnection conn = new SqlConnection("server=localhost;" + "Trusted_Connection=yes;" + "database=TelegramBot;");
            conn.Open();
            //получение рецептов из бд
            var meals = GetMeals(update);
            var name = update.Message.Text.Substring(15).Trim();

            if (name != null)
            {
                //поиск
                for (int i = 0; i < meals.Count(); i++)
                {
                    if (meals[i].Name.ToLower() == name.ToLower())
                    {
                        ind = i;
                        flag = true;
                    }
                }
                //удаление
                if (flag)
                {
                    meals.Remove(meals[ind]);
                    var insCommand = new SqlCommand("DELETE FROM Recipe WHERE Name = '" + name + "' AND Fridge_ID = " + update.Message.From.Id, conn);
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

        //добавление ингредиентов в указанный рецепт (по id рецепта) /recipe_ingr_
        private string CommandAddRecipeIngr(Update update)
        {
            string message = "";
            //получение рецептов из бд
            List<Meal> meals = GetMeals(update);
            List<Product> products = new List<Product>();
            SqlConnection conn = new SqlConnection("server=localhost;" + "Trusted_Connection=yes;" + "database=TelegramBot;");
            conn.Open();
            //до пробела id, после ингредиенты через запятую
            int space = update.Message.Text.IndexOf(" ");
            var ingr = update.Message.Text.Substring(space + 1).Trim();

            //список ингредентов
            var productsArr = ingr.Split(';');
            if (productsArr != null && productsArr[0] != "")
            {
                foreach (var product in productsArr)
                {
                    var arr = product.Split(',');
                    if (arr.Length == 3)
                    {
                        products.Add(new Product(arr[0].Trim(), int.Parse(arr[1].Trim()), arr[2].Trim()));
                    }
                }
            }
            else message = "Пусто";

            int id = int.Parse(update.Message.Text.Substring(13, space - 13)) - 1;

            //если id корректный, добавляем ингредиенты к рецепту
            if (id < meals.Count && id >= 0)
            {
                string ins = "";
                foreach (var prod in products)
                {
                    if (prod.Amount > 0)
                        ins += prod.Name + "," + prod.Amount + "," + prod.Unit + ";";
                }
                var insCommand = new SqlCommand("UPDATE  Recipe SET Ingridients = '" + ins + "'  WHERE Name = '" + meals[id].Name + "' AND Fridge_ID = " + update.Message.From.Id, conn);
                insCommand.ExecuteNonQuery();
                message = "Ингредиенты у блюда \"" + meals[id].Name + "\" успешно обновлены!\nДля добавления текста этого рецепта используйте команду\n/recipe_text_" + (id + 1).ToString() + " Текст рецепта\nПример: \n/recipe_text_" + (id + 1).ToString() + " Сварить 5 пельменей.\n";
            }
            else
                message = "Ошибка";

            conn.Close();
            return message;
        }

        //добавление описания рецепта /recipe_text_
        private string CommandAddRecipeText(Update update)
        {
            string message = "";
            //получение рецептов из бд
            List<Meal> meals = GetMeals(update);
            SqlConnection conn = new SqlConnection("server=localhost;" + "Trusted_Connection=yes;" + "database=TelegramBot;");
            conn.Open();
            //до пробела id, после ингредиенты через запятую
            int space = update.Message.Text.IndexOf(" ");
            var text = update.Message.Text.Substring(space + 1).Trim();

            int id = int.Parse(update.Message.Text.Substring(13, space - 13)) - 1;
            if (text != null && text != "")
            {
                var insCommand = new SqlCommand("UPDATE  Recipe SET Recipe = '" + text + "'  WHERE Name = '" + meals[id].Name + "' AND Fridge_ID = " + update.Message.From.Id, conn);
                insCommand.ExecuteNonQuery();
                message = "Рецепт блюда \"" + meals[id].Name + "\" успешно обновлён!\nДля просмотра этого рецепта используйте команду\n/recipe_show_" + (id + 1).ToString();
            }
            else
                message = "Ошибка";

            conn.Close();
            return message;
        }

        //вывод рецепта пользователю /recipe_show_ 
        private string CommandShowRecipe(Update update)
        {
            string message = "";
            //получение рецептов из бд
            List<Meal> meals = GetMeals(update);
            SqlConnection conn = new SqlConnection("server=localhost;" + "Trusted_Connection=yes;" + "database=TelegramBot;");
            conn.Open();
            int id = int.Parse(update.Message.Text.Substring(13, update.Message.Text.Length - 13)) - 1;
            //если id корректный
            if (id < meals.Count && id >= 0)
            {
                //вывод необходимых ингредиентов в сообщение пользователю
                message += meals[id].Name + "\n\nНеобходимые ингредиенты:\n";
                foreach (var elem in meals[id].Ingredients)
                {
                    message += elem.Name + ", " + elem.Amount + " " + elem.Unit + "\n";
                }
                message += "\n" + meals[id].Recipe + "\n\n";
                //пишем пользователю, достаточно ли у него продуктов в холодильнике
                if (AddNotEnough(update, meals[id].Name).Count == 0)
                    message += "У Вас хватает ингредиентов для приготовления этого блюда. Приготовить: \n/cook_" + (id + 1).ToString();
                else
                    message += "У Вас не хватает ингредиентов для приготовления этого блюда. Добаавить недостающие ингредиенты в список покупок: \n/shopping_meal_" + (id + 1).ToString();
            }
            else
                message = "Ошибка";

            conn.Close();

            return message;
        }

        //список покупок /shopping_meal_
        private string CommandShopping(Update update)
        {
            string message = "";
            SqlConnection conn = new SqlConnection("server=localhost;" + "Trusted_Connection=yes;" + "database=TelegramBot;");
            conn.Open();

            message = "🛒🛒🛒Ваш список покупок🛒🛒🛒\n";
            //получение списка продуктов из бд
            var products = GetShoppingList(update);
            if (products != null && products.Count != 0)
            {
                foreach (var elem in products)
                {
                    message += (products.IndexOf(elem) + 1).ToString() + ". " + elem.Name + ", " + elem.Amount + " " + elem.Unit + "\n";
                }
            }
            else
                message += "Пусто\n";

            message += "\nДобавить продукты можно при помощи команды /shopping_add Название, кол-во, ед. измерения \nПример: \n/shopping_add Томаты, 9, шт\n";
            message += "\nУдалить продукты можно при помощи команды /shopping_remove Название, кол-во, ед. измерения \nПример: \n/shopping_remove Томаты, 9, шт";

            conn.Close();

            return message;
        }

        //добавление продукта в список покупок /shopping_add
        private string CommandAddShopping(Update update)
        {
            string message = "";
            bool flag = false;
            SqlConnection conn = new SqlConnection("server=localhost;" + "Trusted_Connection=yes;" + "database=TelegramBot;");
            conn.Open();
            //получение списка продуктов из бд
            var products = GetShoppingList(update);
            //первые 14 символов "/shopping_add"
            var ingr = update.Message.Text.Substring(14);
            var parse = ingr.Split(',');
            string ins = "";

            for (int i = 0; i < parse.Length; i++)
            {
                parse[i] = parse[i].Trim();
            }

            if (parse != null && parse.Length == 3 && int.Parse(parse[1]) > 0)
            {
                //поиск продукта в списке покупок
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
                //если его нет, добавляем
                if (!flag)
                    products.Add(new Product(parse[0], int.Parse(parse[1]), parse[2]));

                foreach (var prod in products)
                {
                    ins += prod.Name + "," + prod.Amount + "," + prod.Unit + ";";
                }
                //обновляем бд
                var insCommand = new SqlCommand("UPDATE Shopping SET products = '" + ins + "' WHERE id = " + update.Message.From.Id, conn);
                insCommand.ExecuteNonQuery();
                message = "Продукт \"" + parse[0] + "\" успешно добавлен!";
            }
            else
                message = "Ошибка";
            conn.Close();

            return message;
        }

        //удаление продукта из списка покупок /shopping_remove
        private string CommandRemoveShopping(Update update)
        {
            string message = "";
            string ins = "";
            bool flag = false;
            SqlConnection conn = new SqlConnection("server=localhost;" + "Trusted_Connection=yes;" + "database=TelegramBot;");
            conn.Open();
            //первые 14 символов "/shopping_remove"
            var ingr = update.Message.Text.Substring(17);
            var parse = ingr.Split(',');
            var products = GetShoppingList(update);

            for (int i = 0; i < parse.Length; i++)
            {
                parse[i] = parse[i].Trim();
            }

            if (parse != null && parse.Length == 3 && int.Parse(parse[1]) > 0)
            {
                //поиск продукта в списке покупок
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
                    //обновляем бд
                    var insCommand = new SqlCommand("UPDATE Shopping SET products = '" + ins + "' WHERE id = " + update.Message.From.Id, conn);
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

        //получение списка рецептов из бд
        private List<Meal> GetMeals(Update update)
        {
            SqlConnection conn = new SqlConnection("server=localhost;" + "Trusted_Connection=yes;" + "database=TelegramBot;");
            conn.Open();
            var meals = new List<Meal>();
            SqlDataReader getReader = null;
            var myCommand = new SqlCommand("SELECT * FROM Recipe WHERE Fridge_ID = " + update.Message.From.Id, conn);
            getReader = myCommand.ExecuteReader();

            while (getReader.Read())
            {
                //добавление в список рецепта
                meals.Add(new Meal(getReader.GetString(1), GetMealIngr(update, getReader.GetString(1)), getReader.GetString(4)));
            }
            getReader.Close();
            conn.Close();
            return meals;
        }

        //получение продуктов из бд в список из Product
        private List<Product> GetProducts(Update update)
        {
            SqlConnection conn = new SqlConnection("server=localhost;" + "Trusted_Connection=yes;" + "database=TelegramBot;");
            conn.Open();

            var products = new List<Product>();
            SqlDataReader getReader = null;
            var myCommand = new SqlCommand("SELECT * FROM Fridge WHERE id = " + update.Message.From.Id,
                                                 conn);
            getReader = myCommand.ExecuteReader();

            getReader.Read();
            //парсинг данных из бд
            var temp = getReader.GetString(1);
            var productsArr = temp.Split(';');
            if (productsArr != null && productsArr[0] != "")
            {
                foreach (var product in productsArr)
                {
                    var arr = product.Split(',');
                    if (arr.Length == 3)
                    {
                        //добавление продукта в список
                        products.Add(new Product(arr[0], int.Parse(arr[1]), arr[2]));
                    }
                }
            }
            getReader.Close();
            conn.Close();

            return products;
        }

        //получение списка покупок из бд
        private List<Product> GetShoppingList(Update update)
        {
            SqlConnection conn = new SqlConnection("server=localhost;" + "Trusted_Connection=yes;" + "database=TelegramBot;");
            conn.Open();
            //список покупок
            var shoppingList = new List<Product>();
            SqlDataReader getReader = null;
            var myCommand = new SqlCommand("SELECT * FROM Shopping WHERE id = " + update.Message.From.Id,
                                                 conn);
            getReader = myCommand.ExecuteReader();


            getReader.Read();
            //парсинг данных из бд
            var temp = getReader.GetString(1);
            var productsArr = temp.Split(';');
            if (productsArr != null && productsArr[0] != "")
            {
                foreach (var product in productsArr)
                {
                    var arr = product.Split(',');
                    if (arr.Length == 3)
                    {
                        //добавление продукта в список
                        shoppingList.Add(new Product(arr[0], int.Parse(arr[1]), arr[2]));
                    }
                }
            }
            getReader.Close();
            conn.Close();


            return shoppingList;
        }

        //получение ингредиентов указанного рецепта (по названию)
        private List<Product> GetMealIngr(Update update, string name)
        {
            SqlConnection conn = new SqlConnection("server=localhost;" + "Trusted_Connection=yes;" + "database=TelegramBot;");
            conn.Open();
            //список игридиентов
            var ingredients = new List<Product>();
            SqlDataReader getReader = null;
            var myCommand = new SqlCommand("SELECT * FROM Recipe WHERE Fridge_ID = " + update.Message.From.Id + " AND Name = '" + name + "'",
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
                        //добавление ингредиентов в список
                        ingredients.Add(new Product(arr[0], int.Parse(arr[1]), arr[2]));
                    }
                }
            }
            getReader.Close();
            conn.Close();

            return ingredients;
        }

        //добавление всех продуктов для рецепта в список покупок
        private string BuyForMeal(Update update)
        {
            string message = "";
            //получение рецептов из бд
            List<Meal> meals = GetMeals(update);
            SqlConnection conn = new SqlConnection("server=localhost;" + "Trusted_Connection=yes;" + "database=TelegramBot;");
            conn.Open();
            int id = int.Parse(update.Message.Text.Substring(15, update.Message.Text.Length - 15)) - 1;
            if (id < meals.Count && id >= 0)
            {
                //получение списка недостающих проектов
                var toBuy = AddNotEnough(update, meals[id].Name);
                string ins = "";
                foreach (var prod in toBuy)
                {
                    ins += prod.Name + "," + prod.Amount + "," + prod.Unit + ";";
                }
                var insCommand = new SqlCommand("UPDATE  Shopping SET products = '" + ins + "' WHERE id = " + update.Message.From.Id, conn);
                insCommand.ExecuteNonQuery();
                message = "Недостающие ингредиенты добавлены в список покупок";
            }
            else
                message = "Ошибка";

            conn.Close();

            return message;
        }

        //получение списка недостающих для рецепта продуктов
        private List<Product> AddNotEnough(Update update, string name)
        {
            //список продуктов в холодильнике
            var yourList = GetProducts(update);
            //ингредиенты нужные для рецепта
            var mealIngr = GetMealIngr(update, name);
            var shop = new List<Product>();

            foreach (var m in mealIngr)
            {
                bool toAdd = true;
                foreach (var y in yourList)
                {
                    Console.WriteLine(y.Name == m.Name);
                    if (y.Name == m.Name && y.Unit == m.Unit)
                    {
                        toAdd = false;
                        //если ингредиент есть в холодильнике, но в недостаточном количестве
                        if (y.Amount < m.Amount)
                            shop.Add(new Product(m.Name, m.Amount - y.Amount, m.Unit));
                    }
                }

                //если в списке вообще нет данного ингредиента
                if (toAdd)
                    shop.Add(m);
            }

            return shop;
        }

        //приготовить = использовать необходимые продукты из холодильника
        private string CommandCook(Update update)
        {
            List<Meal> meals = GetMeals(update);
            SqlConnection conn = new SqlConnection("server=localhost;" + "Trusted_Connection=yes;" + "database=TelegramBot;");
            conn.Open();
            int id = int.Parse(update.Message.Text.Substring(6, update.Message.Text.Length - 6)) - 1;
            if (id < meals.Count && id >= 0)
            {
                return Cook(update, meals[id].Name);
            }
            return "Ошибка";
        }

        //удаление использованных (в необходимом количестве) продуктов из холодильника
        private string Cook(Update update, string name)
        {
            SqlConnection conn = new SqlConnection("server=localhost;" + "Trusted_Connection=yes;" + "database=TelegramBot;");
            conn.Open();
            var yourList = GetProducts(update);
            var mealIngr = GetMealIngr(update, name);
            string ins = "";

            foreach (var m in mealIngr)
            {
                bool flag = false;
                for (int i = 0; i < yourList.Count; i++)
                {
                    //нахождение соответственных ингредиентов в холодильнике
                    if (yourList[i].Name.ToLower() == m.Name.ToLower() && yourList[i].Unit.ToLower() == m.Unit.ToLower() && (yourList[i].Amount - m.Amount) >= 0)
                    {
                        //удаление использованного количества
                        var t = new Product(yourList[i].Name, yourList[i].Amount - m.Amount, yourList[i].Unit);
                        yourList[i] = t;
                        flag = true;
                    }
                    if (flag)
                        break;
                }
                if (!flag)
                    return "Ошибка";
            }

            //обновление бд
            foreach (var prod in yourList)
            {
                if (prod.Amount > 0)
                    ins += prod.Name + "," + prod.Amount + "," + prod.Unit + ";";
            }
            var insCommand = new SqlCommand("UPDATE  Fridge SET products = '" + ins + "' WHERE id = " + update.Message.From.Id, conn);
            insCommand.ExecuteNonQuery();

            return "Блюдо \"" + name + "\" приготовлено, данные о продуктах изменены";
        }
    }
}
