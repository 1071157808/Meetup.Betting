using System;
using Meetup.Betting.Contracts.Messages;

namespace Meetup.Betting.Actors.Persistence.Events
{
    public interface IMarketEvent
    {
    }

    [Serializable]
    public class BetPlaced : IMarketEvent
    {
        public string SelectionKey { get; set; }    
        public decimal Amount { get; set; }
    }

    [Serializable]
    public class MarketDeactivated : IMarketEvent
    {
    }

    [Serializable]
    public class MarketOddsChanged : IMarketEvent
    {
        public OddData[] Odds { get; set; } 
    }
}
