using System.Threading.Tasks;

namespace Microsoft.Bot.Sample.SimpleEchoBot
{
    public interface ILuisAction
    {
        Task<object> FulfillAsync();
    }
}