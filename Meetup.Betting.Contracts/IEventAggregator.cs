using System.Threading.Tasks;
using Meetup.Betting.Contracts.Messages;
using Orleans;

namespace Meetup.Betting.Contracts
{
    public interface IEventAggregator : IGrainWithIntegerKey
    {
        Task ReceiveEventStatistics(EventStatistics eventStatistics);

        Task<EventStatistics[]> GetEventsStatistics();
    }
}