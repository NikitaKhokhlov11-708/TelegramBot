using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TelegramBot
{
    public class Fridge
    {
        List<Product> listProducts;

        public Fridge()
        {
            listProducts = new List<Product>();
        }

        public void AddProduct(Product product)
        {
            foreach (var elem in listProducts)
            {
                if (elem.name == product.name)
                    product.currency++;
                else
                    listProducts.Add(product);
            }
        }
    }

    
}
