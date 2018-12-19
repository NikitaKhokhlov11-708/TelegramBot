using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TelegramBot
{
    public class Product
    {
        public String name;
        public String description;
        public int cost;
        public bool availability;
        public int currency;
        public String typeCurrency;

        public Product(string name, string description, int cost, bool availability, int currency, string typeCurrency)
        {
            this.name = name;
            this.description = description;
            this.cost = cost;
            this.availability = availability;
            this.currency = currency;
            this.typeCurrency = typeCurrency;
        }
    }


}
