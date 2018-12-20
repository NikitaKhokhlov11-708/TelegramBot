using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TelegramBot
{
    public class Meal
    {
        public string Name;
        public List<Product> Ingredients;
        public string Receipe;

        public Meal(string name, List<Product> ingredients, string receipe)
        {
            Name = name;
            Ingredients = ingredients;
            Receipe = receipe;
        }
    }
}
