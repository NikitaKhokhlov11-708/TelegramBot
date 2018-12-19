using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot.Types;

namespace TelegramBot
{
    public class MyUser
    {
        public int Id;
        public String Name;
        public Fridge Fridge;

        public MyUser (User user)
        {
            Id = user.Id;
            Name = user.FirstName;
            Fridge = new Fridge();
        }
    }
}
