using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.Bot.Sample.SimpleEchoBot
{
    public class ServiceOperations : IBookReservationOperations, IReservationOperations
    {
        private static readonly IList<string> BookGenres = new[] { "Action", "Drama", "Fantasy", "Horror" };

        public async Task<IList<Book>> GetBookAvailability(string reservationChoice)
        {
            await Task.Delay(1500);
            var books = new List<Book>();
            var random = new Random(1);

            // Filling the book results manually for demo purposes
            for (var i = 1; i <= 3; i++)
            {
                var book = new Book
                {
                    Id = i,
                    Name = $"{reservationChoice} Book {i}",
                    Location = reservationChoice,
                    Rating = random.Next(1, 5),
                    NumberOfReviews = random.Next(0, 5000),
                    PriceStarting = random.Next(95, 495),
                    Image = $"https://placeholdit.imgix.net/~text?txtsize=35&txt=Book+{i}&w=500&h=260"
                };

                books.Add(book);
            }

            books.Sort((h1, h2) => h1.PriceStarting.CompareTo(h2.PriceStarting));

            return books;
        }

        public Task<IList<string>> GetExistingReservations(int userId)
        {
            return Task.FromResult(BookGenres);
        }

        public async Task<UserProfile> GetUserInformation(string name, string email)
        {
            await Task.Delay(1567);
            return new UserProfile { Id = 42, Name = name, EMail = email };
        }

        public async Task<BookReservation> ReserveBook(int hotelReservationId, int bookId, DateTime startDate, int days)
        {
            BookReservation cr = new BookReservation()
            {
                Id = bookId,
                ReservationId = hotelReservationId,
                StartDate = startDate,
                Days = days
            };

            // Talk to back end
            await Task.Delay(1234);

            cr.BookingId = new Random(1).Next(10000, 99999);
            return cr;
        }
    }
}