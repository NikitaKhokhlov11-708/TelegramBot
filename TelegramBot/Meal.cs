using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TelegramBot
{
    //класс блюда/рецепта
    public class Meal
    {
        public string Name;                     //название
        public List<Product> Ingredients;       //список ингредиентов из списка продуктов
        public string Recipe;                   //описание рецепта

        public Meal(string name, List<Product> ingredients, string receipe)
        {
            Name = name;
            Ingredients = ingredients;
            Recipe = receipe;
        }
    }
}
