using System.Threading.Tasks;
using Meetup.Betting.Contracts.Messages;
using Orleans;

namespace Meetup.Betting.Contracts
{
    public interface IEventFeedDataChangeTracker : IGrainWithStringKey
    {
        Task ReceiveInnerFeedEvent(EventData eventData);
    }
}