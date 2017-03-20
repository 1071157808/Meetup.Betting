using System;
using System.Threading.Tasks;
using Meetup.Betting.Contracts.Messages;
using Orleans;

namespace Meetup.Betting.Contracts
{
    public interface IEvent : IGrainWithStringKey
    {
        Task SetEventInformation(EventInformation eventInformation);
        Task SetScoreboard(ScoreboardData scoreboardData);
        Task<bool> RegisterMarket(string marketKey);
        Task ReceiveMarketSnapshot(MarketSnapshot snapshot);
        Task<EventData> GetSnapshot();
    }
}