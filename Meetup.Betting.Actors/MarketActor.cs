using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Meetup.Betting.Actors.Persistence;
using Meetup.Betting.Actors.Persistence.Events;
using Meetup.Betting.Contracts;
using Meetup.Betting.Contracts.Messages;
using Orleans;
using Orleans.EventSourcing;
using Orleans.EventSourcing.CustomStorage;
using Orleans.Providers;
using IMarketEvent = Meetup.Betting.Actors.Persistence.Events.IMarketEvent;
using MarketDeactivated = Meetup.Betting.Actors.Persistence.Events.MarketDeactivated;
using MarketOddsChanged = Meetup.Betting.Actors.Persistence.Events.MarketOddsChanged;

namespace Meetup.Betting.Actors
{
    [LogConsistencyProvider(ProviderName = "CustomStorage")]
    public class MarketActor : JournaledGrain<MarketState, IMarketEvent>, IMarket, ICustomStorageInterface<MarketState, IMarketEvent>
    {
        private readonly IEventStorage _eventStorage;
        private IDisposable _statisticsPublisher;

        public MarketActor(IEventStorage eventStorage)
        {
            _eventStorage = eventStorage;
        }

        public override async Task OnActivateAsync()
        {
            var parts = this.GetPrimaryKeyString().Split('|');
            var eventKey = parts[0];
            var marketKey = parts[1];
            var eventActor = GrainFactory.GetGrain<IEvent>(eventKey);
            await eventActor.RegisterMarket(marketKey);
            _statisticsPublisher = RegisterTimer(
                async state =>
                {
                    if (State.IsActive)
                    {
                        await GrainFactory.GetGrain<IEvent>(eventKey).ReceiveMarketSnapshot(new MarketSnapshot
                        {
                            MarketKey = this.GetPrimaryKeyString(),
                            Odds = State.Odds,
                            IsActive = State.IsActive,
                            TotalAmount = State.TotalAmount,
                            BetsCount = State.BetsCount
                        });
                    }
                },
                State,
                TimeSpan.FromSeconds(1),
                TimeSpan.FromSeconds(1));
            await base.OnActivateAsync();
        }

        public async Task ReceiveBet(Bet bet)
        {
            RaiseEvent(new BetPlaced
            {
                SelectionKey = bet.SelectionKey,
                Amount = bet.Amount
            });
            await ConfirmEvents();
        }

        public async Task Deactivate()
        {
            RaiseEvent(new MarketDeactivated());
            await ConfirmEvents();
        }

        public async Task ChangeOdds(OddData[] odds)
        {
            RaiseEvent(new MarketOddsChanged {Odds = odds});
            await ConfirmEvents();
        }

        public Task<MarketSnapshot> GetSnapshot()
        {
            return
                Task.FromResult(new MarketSnapshot
                {
                    MarketKey = this.GetPrimaryKeyString(),
                    Odds = State.Odds,
                    IsActive = State.IsActive,
                    TotalAmount = State.TotalAmount,
                    BetsCount = State.BetsCount
                });
        }

        public async Task<KeyValuePair<int, MarketState>> ReadStateFromStorage()
        {
            var streamId = this.GetPrimaryKeyString();
            var state = await _eventStorage.GetStreamState<MarketState>(streamId);
            return state;
        }

        public async Task<bool> ApplyUpdatesToStorage(IReadOnlyList<IMarketEvent> updates, int expectedversion)
        {
            var retult = await _eventStorage.AppendToStream(this.GetPrimaryKeyString(), updates, expectedversion);
            return retult;
        }
    }
}
