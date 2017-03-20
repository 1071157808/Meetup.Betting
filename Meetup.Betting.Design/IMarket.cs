using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Orleans;
using Orleans.Providers;

namespace Meetup.Betting.Design
{
    public interface IMarket : IGrainWithStringKey
    {
        Task ReceiveBet(Bet bet);

        Task Deactivate();

        Task ChangeOdds(OddData[] odds);
    }

    [StorageProvider(ProviderName = "RedisStorageProvider")]
    public class MarketActor : Grain<MarketState>, IMarket
    {
        public Task ReceiveBet(Bet bet)
        {
            State.BetsCount++;
            State.TotalAmount += bet.Amount;
            return WriteStateAsync();
        }

        public Task Deactivate()
        {
            State.IsActive = false;
            return WriteStateAsync();
        }

        public Task ChangeOdds(OddData[] odds)
        {
            State.IsActive = true;
            State.Odds = odds;
            return WriteStateAsync();
        }
    }

    public class Bet
    {
        public string SelectionKey { get; set; }    
        public decimal Amount { get; set; }
    }

    public class MarketState
    {
        public OddData[] Odds { get; set; }
        public bool IsActive { get; set; }
        public int BetsCount { get; set; }
        public decimal TotalAmount { get; set; }
    }

    public class OddData
    {
        
    }

    public interface IPlayer
    {
        void PlaceBet(Bet bet);
    }

    public interface IEvent
    {
        void SetEventInformation(EventInformation eventInformation);
        void SetScoreboard(ScoreboardData scoreboardData);
        InnerFeedEventData GetSnapshot();
    }

    public class ScoreboardData
    {
        
    }

    public class EventInformation
    {
        
    }

    public class InnerFeedEventData
    {
        
    }
}
