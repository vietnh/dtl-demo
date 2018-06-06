using System;

namespace Microsoft.Bot.Sample.SimpleEchoBot
{
    [Serializable]
    public class UserProfile
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string EMail { get; set; }
        public string ActiveReservation { get; set; }
        public int BookingId { get; set; }
    }
}