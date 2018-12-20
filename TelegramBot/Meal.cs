using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TelegramBot
{
    public class Meal
    {
        public int ID;
        public string Name;
        public int OwnerID;
        public List<Product> Ingredients;
        public string Receipe;

        public Meal(int id, string name, int ownerid, List<Product> ingredients, string receipe)
        {
            ID = id;
            Name = name;
            OwnerID = ownerid;
            Ingredients = ingredients;
            Receipe = receipe;
        }
    }
}
