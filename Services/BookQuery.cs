using Microsoft.Bot.Builder.FormFlow;
using System;

namespace Microsoft.Bot.Sample.SimpleEchoBot
{
    [Serializable]
    public class BookQuery
    {
        public DateTime Start { get; set; }

        [Numeric(1, 14)]
        public int Days { get; set; }
    }
}