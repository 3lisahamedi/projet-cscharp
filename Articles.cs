using System;
using StockGenuis;

namespace StockGenius
{
    public class Article : IIdentifiable
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Content { get; set; }
    }

}
