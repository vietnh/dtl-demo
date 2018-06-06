using System;

namespace Microsoft.Bot.Sample.SimpleEchoBot
{
    [Serializable]
    public class BookReservation
    {
        public int BookingId { get; set; }
        public int Id { get; set; }
        public int ReservationId { get; set; }
        public DateTime StartDate { get; set; }
        public int Days { get; set; }
    }
}