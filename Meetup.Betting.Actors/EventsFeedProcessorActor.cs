using System.Linq;
using System.Threading.Tasks;
using Meetup.Betting.Contracts;
using Meetup.Betting.Contracts.Messages;
using Orleans;

namespace Meetup.Betting.Actors
{
    public class EventsFeedProcessorActor : Grain, IEventsFeedProcessor
    {
        public async Task Process(EventsFeedChangeSet changeSet)
        {
            await Task.WhenAll(
                changeSet.Updated.Select(
                    x => GrainFactory.GetGrain<IEventFeedDataChangeTracker>(x.EventKey).ReceiveInnerFeedEvent(x)));
        }
    }
}