using System;
using StockGenuis;

namespace StockGenius
{
    public class User : IIdentifiable
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
    }
}
