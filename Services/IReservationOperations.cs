using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.Bot.Sample.SimpleEchoBot
{
    public interface IReservationOperations
    {
        Task<IList<string>> GetExistingReservations(int userId);
    }
}
