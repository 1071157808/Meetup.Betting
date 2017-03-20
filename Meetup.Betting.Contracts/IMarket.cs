using System.Threading.Tasks;
using Meetup.Betting.Contracts.Messages;
using Orleans;

namespace Meetup.Betting.Contracts
{
    public interface IMarket : IGrainWithStringKey
    {
        Task<MarketSnapshot> GetSnapshot();

        Task ReceiveBet(Bet bet);
        
        Task Deactivate();

        Task ChangeOdds(OddData[] odds);
    }
}
