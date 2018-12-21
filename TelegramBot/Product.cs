using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TelegramBot
{
    //продукт           
    public class Product
    {
        public string Name;             //название продукта
        public int Amount;              //количество
        public string Unit;             //размерность

        public Product(string name, int amount, string unit)
        {
            Name = name;
            Amount = amount;
            Unit = unit;
        }
    }


}
