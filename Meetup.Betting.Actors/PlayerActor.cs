using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Meetup.Betting.Actors.Persistence.Events;
using Meetup.Betting.Contracts;
using Meetup.Betting.Contracts.Messages;
using Orleans;

namespace Meetup.Betting.Actors
{
    public class PlayerActor : Grain, IPlayer
    {
        private readonly List<Bet> _betHistory = new List<Bet>();

        public async Task PlaceBet(Bet bet)
        {
            var marketActor = GrainFactory.GetGrain<IMarket>($"{bet.EventKey}|{bet.MarketKey}");
            await marketActor.ReceiveBet(bet);
            _betHistory.Add(bet);
        }

        public Task<List<Bet>> GetBetHistory()
        {
            return Task.FromResult(_betHistory);
        }
    }
}