namespace Microsoft.Bot.Sample.SimpleEchoBot
{
    public class ServiceLocator
    {
        public static IBookReservationOperations GetBookReservationOperations()
        {
            return new ServiceOperations();
        }

        public static IReservationOperations GetReservationOperations()
        {
            return new ServiceOperations();
        }

    }
}