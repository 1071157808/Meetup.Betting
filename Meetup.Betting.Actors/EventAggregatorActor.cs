using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Meetup.Betting.Contracts;
using Meetup.Betting.Contracts.Messages;
using Orleans;

namespace Meetup.Betting.Actors
{
    public class EventAggregatorActor : Grain, IEventAggregator
    {
        private readonly Dictionary<string, EventStatistics> _eventsMap = new Dictionary<string, EventStatistics>();

        public Task ReceiveEventStatistics(EventStatistics eventStatistics)
        {
            _eventsMap[eventStatistics.EventKey] = eventStatistics;
            return TaskDone.Done;
        }

        public Task<EventStatistics[]> GetEventsStatistics()
        {
            return Task.FromResult(_eventsMap.Values.OrderByDescending(x => x.BetsCount).Take(20).ToArray());
        }
    }
}