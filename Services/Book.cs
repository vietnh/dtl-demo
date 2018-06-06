using System;

namespace Microsoft.Bot.Sample.SimpleEchoBot
{
    [Serializable]
    public class Book
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int Rating { get; set; }
        public int NumberOfReviews { get; set; }
        public int PriceStarting { get; set; }
        public string Image { get; set; }
        public string Location { get; set; }
    }
}