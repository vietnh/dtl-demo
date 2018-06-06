using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.Bot.Sample.SimpleEchoBot
{
    public interface IBookReservationOperations
    {
        Task<UserProfile> GetUserInformation(string name, string email);
        Task<IList<Book>> GetBookAvailability(string reservationChoice);
        Task<BookReservation> ReserveBook(int hotelReservationId, int bookId, DateTime startDate, int days);
    }
}
