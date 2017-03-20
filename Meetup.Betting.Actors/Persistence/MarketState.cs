using System;
using Meetup.Betting.Actors.Persistence.Events;
using Meetup.Betting.Contracts.Messages;
using MarketDeactivated = Meetup.Betting.Actors.Persistence.Events.MarketDeactivated;
using MarketOddsChanged = Meetup.Betting.Actors.Persistence.Events.MarketOddsChanged;

namespace Meetup.Betting.Actors.Persistence
{
    [Serializable]
    public class MarketState
    {
        public OddData[] Odds { get; set; }
        public bool IsActive { get; set; }  
        public int BetsCount { get; set; }
        public decimal TotalAmount { get; set; }

        public void Apply(BetPlaced @event)
        {
            BetsCount++;
            TotalAmount += @event.Amount;
        }

        public void Apply(MarketDeactivated @event)
        {
            IsActive = false;
        }

        public void Apply(MarketOddsChanged @event)
        {
            IsActive = true;
            Odds = @event.Odds;
        }
    }
}