using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Meetup.Betting.Actors.Persistence;
using Meetup.Betting.Contracts;
using Meetup.Betting.Contracts.Messages;
using Orleans;
using Orleans.EventSourcing;
using EventInformationChanged = Meetup.Betting.Actors.Persistence.Events.EventInformationChanged;
using IEventEvent = Meetup.Betting.Actors.Persistence.Events.IEventEvent;
using MarketRegistered = Meetup.Betting.Actors.Persistence.Events.MarketRegistered;
using ScoreboardChanged = Meetup.Betting.Actors.Persistence.Events.ScoreboardChanged;

namespace Meetup.Betting.Actors
{
    public class EventActor : JournaledGrain<EventState, IEventEvent>, IEvent
    {
        private HashSet<string> _markets = new HashSet<string>();
        private Dictionary<string, MarketSnapshot> _marketsMap = new Dictionary<string, MarketSnapshot>();
        private IDisposable _statisticsPublisher;

        public override async Task OnActivateAsync()
        {
            var tasks = _markets.Select(x => GrainFactory.GetGrain<IMarket>(x).GetSnapshot()).ToArray();
            await Task.WhenAll(tasks);
            _marketsMap = tasks.Select(x => x.Result).ToDictionary(x => x.MarketKey, x => x);

            _statisticsPublisher = RegisterTimer(
                async state =>
                {
                    var eventAggregratorActor = GrainFactory.GetGrain<IEventAggregator>(0);
                    var statistics = _marketsMap.Values.Aggregate(new Statistics(),
                        (ac, x) => ac.Add(x.BetsCount, (int) x.TotalAmount));
                    if (statistics.BetsCount > 0)
                    {
                        await eventAggregratorActor.ReceiveEventStatistics(new EventStatistics
                        {
                            EventKey = this.GetPrimaryKeyString(),
                            EventName = State.Name,
                            BetsCount = statistics.BetsCount,
                            TotalAmount = statistics.TotalAmount
                        });
                    }
                },
                State,
                TimeSpan.FromSeconds(1),
                TimeSpan.FromSeconds(1));
            await base.OnActivateAsync();
        }

        public Task<EventData> GetSnapshot()
        {
            return State.TournamentId != null
                ? Task.FromResult(new EventData
                {
                    Name = State.Name,
                    StartDate = State.StartDate,
                    TournamentKey = State.TournamentId,
                    TeamsKeys = State.TeamsIds,
                    EventKey = this.GetPrimaryKeyString(),
                    Scoreboard = new ScoreboardData
                    {
                        CurrentTime = State.Scoreboard.CurrentTime,
                        CurrentScore = State.Scoreboard.CurrentScore,
                        ScoresByPeriod = State.Scoreboard.ScoresByPeriod
                    },
                    Markets =
                        _marketsMap.ToDictionary(x => x.Key,
                            x =>
                                new MarketData
                                {
                                    Key = x.Value.MarketKey,
                                    Odds = x.Value.Odds.ToDictionary(y => y.SelectionKey, y => y)
                                })
                })
                : Task.FromResult((EventData) null);
        }

        public async Task SetEventInformation(EventInformation eventInformation)
        {
            RaiseEvent(new EventInformationChanged
            {
                Name = eventInformation.Name,
                StartDate = eventInformation.StartDate,
                TeamsIds = eventInformation.TeamsIds,
                TournamentId = eventInformation.TournamentId
            });
            await ConfirmEvents();
        }

        public async Task SetScoreboard(ScoreboardData scoreboardData)
        {
            RaiseEvent(new ScoreboardChanged
            {
                CurrentTime = scoreboardData.CurrentTime,
                CurrentScore =
                    scoreboardData.CurrentScore,
                ScoresByPeriod = scoreboardData.ScoresByPeriod
            });
            await ConfirmEvents();
        }

        public async Task<bool> RegisterMarket(string marketKey)
        {
            if (!State.Markets.Contains(marketKey))
            {
                RaiseEvent(new MarketRegistered {MarketKey = marketKey});
                await ConfirmEvents();
                return true;
            }
            return false;
        }

        public Task ReceiveMarketSnapshot(MarketSnapshot snapshot)
        {
            _marketsMap[snapshot.MarketKey] = snapshot;
            return TaskDone.Done;
        }

        private struct Statistics
        {
            public int BetsCount { get; }
            public int TotalAmount { get; }

            private Statistics(int betsCount, int totalAmount)
            {
                BetsCount = betsCount;
                TotalAmount = totalAmount;
            }

            public Statistics Add(int betsCount, int totalAmount)
            {
                return new Statistics(BetsCount + betsCount, TotalAmount + totalAmount);
            }
        }
    }
}
