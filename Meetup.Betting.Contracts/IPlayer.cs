using System.Collections.Generic;
using System.Threading.Tasks;
using Meetup.Betting.Contracts.Messages;
using Orleans;

namespace Meetup.Betting.Contracts
{
    public interface IPlayer : IGrainWithIntegerKey
    {
        Task PlaceBet(Bet bet);

        Task<List<Bet>> GetBetHistory();
    }
}