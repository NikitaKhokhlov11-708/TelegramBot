using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TelegramBot
{
    public class Product
    {
        public string Name;
        public int Amount;
        public string Unit;

        public Product(string name, int amount, string unit)
        {
            Name = name;
            Amount = amount;
            Unit = unit;
        }
    }


}
